using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models;
using SmartRoomFinder.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace SmartRoomFinder.Services.Implementations
{
    public class KycService : IKycService
    {
        private readonly AppDbContext _context;
        private readonly IEkycAiService _ekycAi;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SmartRoomFinder.Hubs.NotificationHub> _hubContext;
        private readonly SmartRoomFinder.Services.IUserConnectionManager _connectionManager;

        public KycService(
            AppDbContext context, 
            IEkycAiService ekycAi,
            Microsoft.AspNetCore.SignalR.IHubContext<SmartRoomFinder.Hubs.NotificationHub> hubContext,
            SmartRoomFinder.Services.IUserConnectionManager connectionManager)
        {
            _context = context;
            _ekycAi = ekycAi;
            _hubContext = hubContext;
            _connectionManager = connectionManager;
        }

        public async Task<KycProfileModel?> GetKycProfileAsync(string userId)
        {
            return await _context.KycProfiles
                .FirstOrDefaultAsync(k => k.UserId == userId);
        }

        public async Task<KycProfileModel> SubmitKycAsync(
            string userId,
            string identityCardNumber,
            string frontImageUrl,
            string backImageUrl,
            string selfieImageUrl)
        {
            // Gọi AI Service song song: OCR và Face Matching
            var ocrTask = _ekycAi.ExtractCccdInfoAsync(frontImageUrl);
            var faceTask = _ekycAi.CompareFacesAsync(frontImageUrl, selfieImageUrl);
            await Task.WhenAll(ocrTask, faceTask);

            var ocrResult = await ocrTask;
            var faceResult = await faceTask;

            var existing = await _context.KycProfiles.FirstOrDefaultAsync(k => k.UserId == userId);

            if (existing != null)
            {
                // Cho phép nộp lại nếu bị từ chối hoặc chưa được duyệt
                existing.IdentityCardNumber = identityCardNumber;
                existing.FrontImageUrl = frontImageUrl;
                existing.BackImageUrl = backImageUrl;
                existing.SelfieImageUrl = selfieImageUrl;
                existing.Status = KycStatus.Pending;
                existing.RejectReason = null;
                existing.SubmittedAt = DateTime.UtcNow;
                existing.ReviewedAt = null;
                existing.ReviewedByAdminId = null;

                if (ocrResult.Success)
                {
                    existing.FullNameOnCard = ocrResult.FullName;
                    existing.DateOfBirthOnCard = ocrResult.DateOfBirth;
                    existing.AiOcrResultJson = ocrResult.RawJson;
                }
                if (faceResult.Success)
                {
                    existing.AiFaceMatchScore = faceResult.MatchScore;
                    existing.AiIsMatch = faceResult.IsMatch;
                }

                _context.KycProfiles.Update(existing);

                // Cập nhật cache trạng thái trên UserModel
                var userExisting = await _context.Users.FindAsync(userId);
                if (userExisting != null)
                {
                    userExisting.KycStatus = KycStatus.Pending;
                    userExisting.IsKycVerified = false;
                }

                // Lưu thông báo vào CSDL
                var notification = new NotificationModel
                {
                    UserId = userId,
                    Title = "Nộp hồ sơ KYC thành công",
                    Content = "Hồ sơ xác minh danh tính (KYC) của bạn đã được ghi nhận và đang chờ Admin xét duyệt.",
                    Type = "KYC",
                    LinkUrl = "/Profile/KycStatus",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();
                return existing;
            }

            // Tạo mới
            var profile = new KycProfileModel
            {
                UserId = userId,
                IdentityCardNumber = identityCardNumber,
                FrontImageUrl = frontImageUrl,
                BackImageUrl = backImageUrl,
                SelfieImageUrl = selfieImageUrl,
                Status = KycStatus.Pending,
                SubmittedAt = DateTime.UtcNow
            };

            if (ocrResult.Success)
            {
                profile.FullNameOnCard = ocrResult.FullName;
                profile.DateOfBirthOnCard = ocrResult.DateOfBirth;
                profile.AiOcrResultJson = ocrResult.RawJson;
            }
            if (faceResult.Success)
            {
                profile.AiFaceMatchScore = faceResult.MatchScore;
                profile.AiIsMatch = faceResult.IsMatch;
            }

            _context.KycProfiles.Add(profile);

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.KycStatus = KycStatus.Pending;
                user.IsKycVerified = false;
            }

            // Lưu thông báo vào CSDL
            var newNotification = new NotificationModel
            {
                UserId = userId,
                Title = "Nộp hồ sơ KYC thành công",
                Content = "Hồ sơ xác minh danh tính (KYC) của bạn đã được ghi nhận và đang chờ Admin xét duyệt.",
                Type = "KYC",
                LinkUrl = "/Profile/KycStatus",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(newNotification);

            await _context.SaveChangesAsync();
            return profile;
        }

        public async Task<bool> ApproveKycAsync(string kycId, string adminId)
        {
            var kyc = await _context.KycProfiles.FindAsync(kycId);
            if (kyc == null) return false;

            kyc.Status = KycStatus.Approved;
            kyc.ReviewedAt = DateTime.UtcNow;
            kyc.ReviewedByAdminId = adminId;
            kyc.RejectReason = null;

            // Mở khóa tính năng đăng bài cho Chủ trọ
            var user = await _context.Users.FindAsync(kyc.UserId);
            if (user != null)
            {
                user.IsKycVerified = true;
                user.KycStatus = KycStatus.Approved;
            }

            // Lưu thông báo vào CSDL
            var notification = new NotificationModel
            {
                UserId = kyc.UserId,
                Title = "Hồ sơ xác minh (KYC) đã được phê duyệt",
                Content = "Chúc mừng! Hồ sơ KYC của bạn đã được Admin phê duyệt. Bạn đã có thể đăng tin mới và cài đặt thanh toán.",
                Type = "KYC",
                LinkUrl = "/Profile/KycStatus",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // Gửi thông báo thời gian thực qua SignalR
            try
            {
                var connections = _connectionManager.GetUserConnections(kyc.UserId);
                if (connections != null && connections.Count > 0)
                {
                    await _hubContext.Clients.Clients(connections).SendAsync("ReceiveNotification", "Hồ sơ xác minh (KYC) của bạn đã được phê duyệt.");
                }
            }
            catch { /* Bỏ qua nếu có lỗi SignalR */ }

            return true;
        }

        public async Task<bool> RejectKycAsync(string kycId, string adminId, string reason)
        {
            var kyc = await _context.KycProfiles.FindAsync(kycId);
            if (kyc == null) return false;

            kyc.Status = KycStatus.Rejected;
            kyc.RejectReason = reason;
            kyc.ReviewedAt = DateTime.UtcNow;
            kyc.ReviewedByAdminId = adminId;

            var user = await _context.Users.FindAsync(kyc.UserId);
            if (user != null)
            {
                user.IsKycVerified = false;
                user.KycStatus = KycStatus.Rejected;
            }

            // Lưu thông báo vào CSDL
            var notification = new NotificationModel
            {
                UserId = kyc.UserId,
                Title = "Hồ sơ xác minh (KYC) bị từ chối",
                Content = $"Hồ sơ KYC của bạn bị từ chối. Lý do: {reason}",
                Type = "KYC",
                LinkUrl = "/Profile/KycStatus",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();

            // Gửi thông báo thời gian thực qua SignalR
            try
            {
                var connections = _connectionManager.GetUserConnections(kyc.UserId);
                if (connections != null && connections.Count > 0)
                {
                    await _hubContext.Clients.Clients(connections).SendAsync("ReceiveNotification", $"Hồ sơ KYC của bạn bị từ chối. Lý do: {reason}");
                }
            }
            catch { /* Bỏ qua nếu có lỗi SignalR */ }

            return true;
        }

        public async Task<List<KycProfileModel>> GetPendingKycListAsync()
        {
            return await _context.KycProfiles
                .Include(k => k.User)
                .Where(k => k.Status == KycStatus.Pending)
                .OrderBy(k => k.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<KycProfileModel>> GetAllKycListAsync()
        {
            return await _context.KycProfiles
                .Include(k => k.User)
                .OrderByDescending(k => k.SubmittedAt)
                .ToListAsync();
        }
    }
}

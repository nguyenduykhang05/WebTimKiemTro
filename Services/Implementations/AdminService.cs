using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models.ViewModels;
using SmartRoomFinder.Services.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardViewModel> GetDashboardStatsAsync(string? filterStatus = null)
        {
            var query = _context.Rooms.Include(r => r.Images).AsQueryable();

            if (!string.IsNullOrEmpty(filterStatus) && Enum.TryParse<RoomStatus>(filterStatus, true, out var status))
            {
                query = query.Where(r => r.ApprovalStatus == status);
            }

            var rooms = await query.OrderByDescending(r => r.PostedAt).ToListAsync();
            var reviews = await _context.Reviews.OrderByDescending(r => r.CreatedAt).Take(20).ToListAsync();
            var reports = await _context.Reports.Where(r => r.Status != "resolved").OrderByDescending(r => r.CreatedAt).ToListAsync();

            var pendingRoomsCount = await _context.Rooms.CountAsync(r => r.ApprovalStatus == RoomStatus.Pending);
            var verifiedRoomsCount = await _context.Rooms.CountAsync(r => r.ApprovalStatus == RoomStatus.Verified);
            var rejectedRoomsCount = await _context.Rooms.CountAsync(r => r.ApprovalStatus == RoomStatus.Rejected);
            var needInfoRoomsCount = await _context.Rooms.CountAsync(r => r.ApprovalStatus == RoomStatus.NeedsInfo);
            var totalUsers = await _context.Users.CountAsync();
            var totalReports = await _context.Reports.CountAsync();

            var last6Months = Enumerable.Range(0, 6).Select(i => DateTime.UtcNow.AddMonths(-i).ToString("MM/yyyy")).Reverse().ToList();
            var monthlyNewRooms = new Dictionary<string, int>();
            var monthlyTotalViews = new Dictionary<string, int>();

            foreach (var month in last6Months)
            {
                var monthDate = DateTime.ParseExact(month, "MM/yyyy", null);
                var roomsInMonth = await _context.Rooms
                    .Where(r => r.PostedAt.Year == monthDate.Year && r.PostedAt.Month == monthDate.Month)
                    .ToListAsync();

                monthlyNewRooms[month] = roomsInMonth.Count;
                monthlyTotalViews[month] = roomsInMonth.Sum(r => r.ViewCount);
            }

            return new AdminDashboardViewModel
            {
                TotalUsers = totalUsers,
                TotalRooms = await _context.Rooms.CountAsync(),
                TotalReports = totalReports,
                PendingRoomsCount = pendingRoomsCount,
                VerifiedRoomsCount = verifiedRoomsCount,
                RejectedRoomsCount = rejectedRoomsCount,
                NeedInfoRoomsCount = needInfoRoomsCount,
                SupportRequestsCount = 0,
                
                MonthlyNewRooms = monthlyNewRooms,
                MonthlyTotalViews = monthlyTotalViews,
                
                PendingRooms = rooms.Where(r => r.ApprovalStatus == RoomStatus.Pending),
                RecentVerifiedRooms = rooms.Where(r => r.ApprovalStatus == RoomStatus.Verified).Take(10),
                RecentReviews = reviews,
                RecentReports = reports
            };
        }

        public async Task<bool> VerifyRoomAsync(string roomId, string note, string adminId, string adminName)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return false;

            room.ApprovalStatus = RoomStatus.Verified;
            room.IsVerified = true;
            room.IsActive = true;
            room.UpdatedAt = DateTime.UtcNow;

            var history = room.ReviewHistory;
            history.Add(new RoomReviewHistory
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow,
                Title = "Bài đăng đã xác minh",
                Subtitle = $"Admin đã phê duyệt bài đăng. Ghi chú: \"{note}\"",
                ActorId = adminId,
                ActorName = adminName
            });
            room.ReviewHistory = history;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectRoomAsync(string roomId, string note, string adminId, string adminName)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return false;

            room.ApprovalStatus = RoomStatus.Rejected;
            room.IsVerified = false;
            room.IsActive = false;
            room.UpdatedAt = DateTime.UtcNow;

            var history = room.ReviewHistory;
            history.Add(new RoomReviewHistory
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow,
                Title = "Từ chối bài đăng",
                Subtitle = $"Admin từ chối bài đăng. Lý do: \"{note}\"",
                ActorId = adminId,
                ActorName = adminName
            });
            room.ReviewHistory = history;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RequestRoomInfoAsync(string roomId, string note, string adminId, string adminName)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return false;

            room.ApprovalStatus = RoomStatus.NeedsInfo;
            room.IsVerified = false;
            room.UpdatedAt = DateTime.UtcNow;

            var history = room.ReviewHistory;
            history.Add(new RoomReviewHistory
            {
                Id = Guid.NewGuid().ToString(),
                Time = DateTime.UtcNow,
                Title = "Yêu cầu bổ sung thông tin",
                Subtitle = $"Admin yêu cầu bổ sung thông tin: \"{note}\"",
                ActorId = adminId,
                ActorName = adminName
            });
            room.ReviewHistory = history;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ToggleHideReviewAsync(string reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review == null) return false;

            review.IsHidden = !review.IsHidden;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ResolveReportAsync(string reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return false;

            report.Status = "resolved";
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> HideReportedRoomAsync(string reportId)
        {
            var report = await _context.Reports.FindAsync(reportId);
            if (report == null) return false;

            var room = await _context.Rooms.FindAsync(report.RoomId);
            if (room != null)
            {
                room.IsActive = false; // Hide the room
                room.ApprovalStatus = RoomStatus.Rejected; // Reject it
                _context.Rooms.Update(room);
            }

            report.Status = "resolved"; // Close the report
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

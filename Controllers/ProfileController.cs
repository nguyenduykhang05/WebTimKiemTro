using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Services.Interfaces;

namespace SmartRoomFinder.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IKycService _kycService;
        private readonly IFileService _fileService;

        public ProfileController(AppDbContext context, IKycService kycService, IFileService fileService)
        {
            _context = context;
            _kycService = kycService;
            _fileService = fileService;
        }

        // -------------------------------------------------------
        // Trang hồ sơ cá nhân
        // -------------------------------------------------------
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (user.Role == UserRole.Landlord)
            {
                ViewBag.KycProfile = await _kycService.GetKycProfileAsync(userId);
            }

            return View(user);
        }

        // -------------------------------------------------------
        // Chỉnh sửa hồ sơ
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(UserModel model, IFormFile? avatarFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || model.Id != userId) return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var uploadedUrl = await _fileService.UploadImageAsync(avatarFile, "avatars");
                if (!string.IsNullOrEmpty(uploadedUrl))
                {
                    user.ProfileImageUrl = uploadedUrl;
                }
            }
            else if (!string.IsNullOrEmpty(model.ProfileImageUrl))
            {
                user.ProfileImageUrl = model.ProfileImageUrl;
            }

            user.Name = model.Name;
            user.PhoneNumber = model.PhoneNumber;
            user.Location = model.Location;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        // -------------------------------------------------------
        // Nộp hồ sơ KYC (eKYC xác thực CCCD)
        // -------------------------------------------------------
        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public async Task<IActionResult> SubmitKyc(string? reason = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var existing = await _kycService.GetKycProfileAsync(userId);

            // Đã được duyệt → không cần nộp lại
            if (existing?.Status == SmartRoomFinder.Models.KycStatus.Approved)
            {
                return RedirectToAction("KycStatus");
            }

            ViewBag.Reason = reason;
            ViewBag.ExistingKyc = existing;
            return View(existing);
        }

        [Authorize(Roles = "Landlord")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitKyc(
            string identityCardNumber,
            IFormFile frontImage,
            IFormFile backImage,
            IFormFile selfieImage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (frontImage == null || backImage == null || selfieImage == null)
            {
                ModelState.AddModelError("", "Vui lòng tải lên đủ 3 ảnh (mặt trước CCCD, mặt sau CCCD, ảnh selfie cầm CCCD).");
                return View();
            }

            // Upload 3 ảnh lên server
            var frontUrl = await _fileService.UploadImageAsync(frontImage, "kyc");
            var backUrl = await _fileService.UploadImageAsync(backImage, "kyc");
            var selfieUrl = await _fileService.UploadImageAsync(selfieImage, "kyc");

            await _kycService.SubmitKycAsync(userId, identityCardNumber, frontUrl, backUrl, selfieUrl);

            TempData["Success"] = "Hồ sơ xác thực danh tính đã được gửi thành công! Admin sẽ xét duyệt trong vòng 24 giờ.";
            return RedirectToAction("KycStatus");
        }

        // -------------------------------------------------------
        // Xem trạng thái KYC
        // -------------------------------------------------------
        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public async Task<IActionResult> KycStatus()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var kyc = await _kycService.GetKycProfileAsync(userId);
            return View(kyc);
        }

        // -------------------------------------------------------
        // Cài đặt PayOS (Chủ trọ nhập API Key để nhận tiền cọc)
        // -------------------------------------------------------
        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public async Task<IActionResult> PaymentSettings()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            // Bắt buộc phải KYC trước khi cấu hình thanh toán
            if (!user.IsKycVerified)
            {
                TempData["Warning"] = "Bạn cần xác thực danh tính (KYC) trước khi cấu hình cổng thanh toán.";
                return RedirectToAction("SubmitKyc");
            }

            return View(user);
        }

        [Authorize(Roles = "Landlord")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentSettings(
            string method,
            string? payOsClientId, 
            string? payOsApiKey, 
            string? payOsChecksumKey,
            string? bankName,
            string? bankAccountNumber,
            string? bankAccountHolder)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            if (method == "PayOS")
            {
                if (string.IsNullOrWhiteSpace(payOsClientId) ||
                    string.IsNullOrWhiteSpace(payOsApiKey) ||
                    string.IsNullOrWhiteSpace(payOsChecksumKey))
                {
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ cả 3 thông tin cấu hình PayOS.");
                    return View(user);
                }

                user.PayOsClientId = payOsClientId.Trim();
                user.PayOsApiKey = payOsApiKey.Trim();
                user.PayOsChecksumKey = payOsChecksumKey.Trim();

                // Xóa cấu hình VietQR nếu chọn PayOS
                user.BankName = string.Empty;
                user.BankAccountNumber = string.Empty;
                user.BankAccountHolder = string.Empty;
            }
            else if (method == "VietQR")
            {
                if (string.IsNullOrWhiteSpace(bankName) ||
                    string.IsNullOrWhiteSpace(bankAccountNumber) ||
                    string.IsNullOrWhiteSpace(bankAccountHolder))
                {
                    ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin tài khoản ngân hàng.");
                    return View(user);
                }

                user.BankName = bankName.Trim();
                user.BankAccountNumber = bankAccountNumber.Trim();
                user.BankAccountHolder = bankAccountHolder.Trim().ToUpper();

                // Xóa cấu hình PayOS nếu chọn VietQR
                user.PayOsClientId = string.Empty;
                user.PayOsApiKey = string.Empty;
                user.PayOsChecksumKey = string.Empty;
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã lưu cấu hình liên kết thanh toán {(method == "PayOS" ? "PayOS" : "VietQR")} thành công! Phòng của bạn đã sẵn sàng nhận đặt cọc.";
            return RedirectToAction("Index");
        }
    }
}

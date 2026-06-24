using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SmartRoomFinder.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly AppDbContext _context;
        private readonly IKycService _kycService;
        private readonly IChototScraperService _chototScraperService;

        public AdminController(IAdminService adminService, AppDbContext context, IKycService kycService, IChototScraperService chototScraperService)
        {
            _adminService = adminService;
            _context = context;
            _kycService = kycService;
            _chototScraperService = chototScraperService;
        }

        public async Task<IActionResult> Dashboard(string? status = null)
        {
            var stats = await _adminService.GetDashboardStatsAsync(status);
            ViewBag.CurrentFilter = status;
            return View(stats);
        }

        [HttpPost]
        public async Task<IActionResult> Verify(string id, string note = "Tin đăng hợp lệ.")
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminName = User.Identity?.Name ?? "Admin";
            var success = await _adminService.VerifyRoomAsync(id, note, adminId ?? "", adminName);
            if (!success) return NotFound();
            TempData["Success"] = "Đã phê duyệt phòng trọ thành công!";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> Reject(string id, string note = "Tin đăng vi phạm tiêu chuẩn cộng đồng.")
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminName = User.Identity?.Name ?? "Admin";
            var success = await _adminService.RejectRoomAsync(id, note, adminId ?? "", adminName);
            if (!success) return NotFound();
            TempData["Success"] = "Đã từ chối bài đăng!";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> RequestInfo(string id, string note = "Vui lòng cập nhật hình ảnh rõ nét hơn.")
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var adminName = User.Identity?.Name ?? "Admin";
            var success = await _adminService.RequestRoomInfoAsync(id, note, adminId ?? "", adminName);
            if (!success) return NotFound();
            TempData["Success"] = "Đã gửi yêu cầu bổ sung thông tin!";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleHideReview(string id)
        {
            var success = await _adminService.ToggleHideReviewAsync(id);
            if (!success) return NotFound();
            TempData["Success"] = "Đã cập nhật trạng thái đánh giá.";
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        public async Task<IActionResult> ResolveReport(string id)
        {
            var success = await _adminService.ResolveReportAsync(id);
            if (!success) return NotFound();
            TempData["Success"] = "Đã đóng báo cáo vi phạm.";
            return RedirectToAction(nameof(Reports));
        }

        [HttpPost]
        public async Task<IActionResult> HideReportedRoom(string id)
        {
            var success = await _adminService.HideReportedRoomAsync(id);
            if (!success) return NotFound();
            TempData["Success"] = "Đã ẩn bài đăng vi phạm.";
            return RedirectToAction(nameof(Reports));
        }

        public async Task<IActionResult> Support()
        {
            ViewBag.CurrentMenu = "System";
            ViewBag.CurrentSubMenu = "Support";
            var tickets = await _context.SupportTickets.OrderByDescending(t => t.CreatedAt).ToListAsync();
            return View(tickets);
        }

        [HttpPost]
        public async Task<IActionResult> ReplySupport(string id, string replyMessage)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket != null)
            {
                ticket.AdminReply = replyMessage;
                ticket.Status = "Resolved";
                ticket.RepliedAt = System.DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã trả lời yêu cầu hỗ trợ.";
            }
            return RedirectToAction(nameof(Support));
        }

        public async Task<IActionResult> Settings()
        {
            ViewBag.CurrentMenu = "System";
            ViewBag.CurrentSubMenu = "Settings";
            var settings = await _context.SystemSettings.ToDictionaryAsync(s => s.Key, s => s.Value);
            ViewBag.AdminEmail = settings.GetValueOrDefault("AdminEmail", "admin@smartroomfinder.com");
            ViewBag.SiteName = settings.GetValueOrDefault("SiteName", "Smart Room Finder");
            ViewBag.MaintenanceMode = settings.GetValueOrDefault("MaintenanceMode", "false") == "true";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSettings(string adminEmail, string siteName, bool maintenanceMode)
        {
            async Task Upsert(string key, string val, string desc)
            {
                var s = await _context.SystemSettings.FirstOrDefaultAsync(x => x.Key == key);
                if (s == null) _context.SystemSettings.Add(new SystemSettingModel { Key = key, Value = val, Description = desc });
                else s.Value = val;
            }

            await Upsert("AdminEmail", adminEmail, "Email liên hệ hệ thống");
            await Upsert("SiteName", siteName, "Tên website");
            await Upsert("MaintenanceMode", maintenanceMode.ToString().ToLower(), "Chế độ bảo trì");
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật cài đặt hệ thống thành công.";
            return RedirectToAction(nameof(Settings));
        }

        // -------------------------------------------------------
        // QUẢN LÝ NGƯỜI DÙNG
        // -------------------------------------------------------
        public async Task<IActionResult> ManageUsers()
        {
            var users = await _context.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsLocked = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã khoá tài khoản {user.Name}.";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsLocked = false;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã mở khoá tài khoản {user.Name}.";
            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserPackage(string userId, RoomPackage package, int days)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.CurrentPackage = package;
            if (package == RoomPackage.Default)
            {
                user.PackageExpiresAt = null;
            }
            else
            {
                user.PackageExpiresAt = DateTime.UtcNow.AddDays(days);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật gói dịch vụ cho {user.Name}.";
            return RedirectToAction(nameof(ManageUsers));
        }

        // -------------------------------------------------------
        // QUẢN LÝ BÁO CÁO VI PHẠM
        // -------------------------------------------------------
        public async Task<IActionResult> Reports()
        {
            var reports = await _context.Reports.ToListAsync();
            return View(reports);
        }

        // -------------------------------------------------------
        // GIAO DỊCH DỊCH VỤ
        // -------------------------------------------------------
        public async Task<IActionResult> ServiceTransactions()
        {
            var transactions = await _context.ServiceTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
            return View(transactions);
        }

        // -------------------------------------------------------
        // QUẢN LÝ KYC - XÁC THỰC DANH TÍNH CHỦ TRỌ
        // -------------------------------------------------------

        /// <summary>Danh sách tất cả hồ sơ KYC (lọc theo trạng thái).</summary>
        public async Task<IActionResult> VerifyKyc(string? filter = null)
        {
            List<KycProfileModel> list;
            if (filter == "pending")
                list = await _kycService.GetPendingKycListAsync();
            else
                list = await _kycService.GetAllKycListAsync();

            ViewBag.Filter = filter ?? "all";
            ViewBag.PendingCount = (await _kycService.GetPendingKycListAsync()).Count;
            return View(list);
        }

        /// <summary>Admin phê duyệt hồ sơ KYC → Chủ trọ được phép đăng tin.</summary>
        [HttpPost]
        public async Task<IActionResult> ApproveKyc(string id)
        {
            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var success = await _kycService.ApproveKycAsync(id, adminId);
            if (!success) return NotFound();
            TempData["Success"] = "✅ Đã phê duyệt hồ sơ KYC! Chủ trọ có thể đăng bài ngay.";
            return RedirectToAction(nameof(VerifyKyc), new { filter = "pending" });
        }

        /// <summary>Admin từ chối hồ sơ KYC với lý do cụ thể.</summary>
        [HttpPost]
        public async Task<IActionResult> RejectKyc(string id, string rejectReason)
        {
            if (string.IsNullOrWhiteSpace(rejectReason))
            {
                TempData["Error"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(VerifyKyc));
            }

            var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var success = await _kycService.RejectKycAsync(id, adminId, rejectReason);
            if (!success) return NotFound();
            TempData["Success"] = "❌ Đã từ chối hồ sơ KYC. Lý do đã được gửi cho chủ trọ.";
            return RedirectToAction(nameof(VerifyKyc));
        }

        /// <summary>Scrape dữ liệu phòng trọ từ API Chợ Tốt.</summary>
        [HttpPost]
        public async Task<IActionResult> ScrapeChotot([FromBody] ScrapeRequest model)
        {
            try
            {
                int limit = model.Limit > 0 ? model.Limit : 10;
                var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
                var adminName = User.Identity?.Name ?? "Admin";
                
                int count = await _chototScraperService.ScrapeAndImportRoomsAsync(adminId, adminName, limit);
                
                if (count > 0)
                {
                    return Json(new { success = true, message = $"Đã lấy thành công {count} phòng trọ từ Chợ Tốt!" });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy phòng trọ nào hoặc quá trình cào dữ liệu gặp lỗi." });
                }
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        public class ScrapeRequest
        {
            public int Limit { get; set; }
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SmartRoomFinder.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly AppDbContext _context;

        public AdminController(IAdminService adminService, AppDbContext context)
        {
            _adminService = adminService;
            _context = context;
        }

        public async Task<IActionResult> Dashboard(string status = null)
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
            var emailSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "AdminEmail");
            if (emailSetting == null) _context.SystemSettings.Add(new SystemSettingModel { Key = "AdminEmail", Value = adminEmail, Description = "Email liên hệ hệ thống" });
            else emailSetting.Value = adminEmail;

            var siteNameSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "SiteName");
            if (siteNameSetting == null) _context.SystemSettings.Add(new SystemSettingModel { Key = "SiteName", Value = siteName, Description = "Tên website" });
            else siteNameSetting.Value = siteName;

            var maintenanceSetting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == "MaintenanceMode");
            if (maintenanceSetting == null) _context.SystemSettings.Add(new SystemSettingModel { Key = "MaintenanceMode", Value = maintenanceMode.ToString().ToLower(), Description = "Chế độ bảo trì" });
            else maintenanceSetting.Value = maintenanceMode.ToString().ToLower();

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật cài đặt hệ thống thành công.";
            return RedirectToAction(nameof(Settings));
        }

        // --- USER MANAGEMENT ---
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
            _context.Users.Update(user);
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
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Đã mở khoá tài khoản {user.Name}.";
            
            return RedirectToAction(nameof(ManageUsers));
        }

        // --- REPORTS MANAGEMENT ---
        public async Task<IActionResult> Reports()
        {
            // Fetch all reports
            var reports = await _context.Reports.ToListAsync();
            return View(reports);
        }
        // --- DEPOSITS MANAGEMENT ---
        public async Task<IActionResult> ManageDeposits()
        {
            var deposits = await _context.Deposits
                .Include(d => d.Room)
                .Include(d => d.Renter)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return View(deposits);
        }

        [HttpPost]
        public async Task<IActionResult> RefundDeposit(string id)
        {
            var deposit = await _context.Deposits.FindAsync(id);
            if (deposit == null) return NotFound();

            if (deposit.Status == DepositStatus.Paid)
            {
                deposit.Status = DepositStatus.Refunded;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã hoàn cọc cho người thuê thành công.";
            }

            return RedirectToAction(nameof(ManageDeposits));
        }

        [HttpPost]
        public async Task<IActionResult> TransferDeposit(string id)
        {
            var deposit = await _context.Deposits.FindAsync(id);
            if (deposit == null) return NotFound();

            if (deposit.Status == DepositStatus.Paid)
            {
                deposit.Status = DepositStatus.Completed;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã chuyển cọc cho chủ trọ thành công.";
            }

            return RedirectToAction(nameof(ManageDeposits));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models;
using SmartRoomFinder.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.SignalR;

namespace SmartRoomFinder.Controllers
{
    [Authorize]
    public class DepositController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly Microsoft.AspNetCore.SignalR.IHubContext<SmartRoomFinder.Hubs.NotificationHub> _hubContext;
        private readonly SmartRoomFinder.Services.IUserConnectionManager _connectionManager;

        public DepositController(
            AppDbContext context, 
            IPaymentService paymentService,
            Microsoft.AspNetCore.SignalR.IHubContext<SmartRoomFinder.Hubs.NotificationHub> hubContext,
            SmartRoomFinder.Services.IUserConnectionManager connectionManager)
        {
            _context = context;
            _paymentService = paymentService;
            _hubContext = hubContext;
            _connectionManager = connectionManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isLandlord = User.IsInRole("Landlord");

            IQueryable<DepositModel> query = _context.Deposits
                .Include(d => d.Room)
                .Include(d => d.Renter);

            query = isLandlord
                ? query.Where(d => d.Room.OwnerId == userId)
                : query.Where(d => d.RenterId == userId);

            var deposits = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return View("MyDeposits", deposits);
        }

        [HttpPost]
        [Authorize(Roles = "Renter")]
        public async Task<IActionResult> Create(string roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null || !room.IsActive || room.IsReserved)
                return BadRequest("Phòng không tồn tại hoặc đã được đặt.");

            // Kiểm tra chủ trọ đã cấu hình PayOS hoặc VietQR chưa
            var landlord = await _context.Users.FindAsync(room.OwnerId);
            if (landlord == null || (!landlord.HasPayOsConfigured && !landlord.HasVietQrConfigured))
            {
                TempData["ErrorMessage"] = "Chủ trọ chưa cấu hình phương thức nhận tiền đặt cọc. Vui lòng nhắn tin trực tiếp để trao đổi.";
                return RedirectToAction("Detail", "Home", new { id = roomId });
            }

            // Kiểm tra chủ trọ đã xác thực KYC chưa
            if (!landlord.IsKycVerified)
            {
                TempData["ErrorMessage"] = "Chủ trọ chưa xác thực danh tính. Hệ thống không hỗ trợ đặt cọc với chủ trọ chưa được xác thực để bảo vệ bạn.";
                return RedirectToAction("Detail", "Home", new { id = roomId });
            }

            double amount = room.DepositAmount;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Tái sử dụng đơn cọc đang Pending nếu có
            var existing = await _context.Deposits
                .FirstOrDefaultAsync(d => d.RoomId == roomId && d.RenterId == userId && d.Status == DepositStatus.Pending);

            DepositModel deposit;
            if (existing != null)
            {
                deposit = existing;
            }
            else
            {
                deposit = new DepositModel
                {
                    RoomId = roomId,
                    RenterId = userId!,
                    Amount = amount,
                    ExpiresAt = DateTime.UtcNow.AddDays(3)
                };
                _context.Deposits.Add(deposit);
                await _context.SaveChangesAsync();
            }

            if (amount <= 0)
            {
                // Nếu đặt cọc 0đ -> Duyệt tự động thành công ngay
                deposit.Status = DepositStatus.Paid;
                deposit.PaidAt = DateTime.UtcNow;
                deposit.TransactionId = "FREE-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                room.IsReserved = true;

                // Tạo thông báo
                var freeNotif = new NotificationModel
                {
                    UserId = userId!,
                    Title = "Giữ chỗ phòng thành công",
                    Content = $"Bạn đã giữ chỗ thành công phòng '{room.Title}' (Đặt chỗ 0đ).",
                    Type = "System",
                    LinkUrl = "/Deposit",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(freeNotif);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "🎉 Giữ chỗ phòng thành công! Phòng đã được đặt cho bạn.";
                return RedirectToAction("Index");
            }

            if (landlord.HasVietQrConfigured && !landlord.HasPayOsConfigured)
            {
                // Chuyển hướng đến trang hiển thị mã VietQR chuyển khoản trực tiếp
                return RedirectToAction("VietQrPayment", new { depositId = deposit.Id });
            }

            var checkoutUrl = await _paymentService.CreateDepositPaymentLinkAsync(deposit.Id, roomId, amount);
            return Redirect(checkoutUrl);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback(
            string depositId,
            bool cancel = false,
            string? code = null,
            string? id = null,
            string? orderCode = null,
            string? status = null)
        {
            var deposit = await _context.Deposits
                .Include(d => d.Room)
                .FirstOrDefaultAsync(d => d.Id == depositId);

            if (deposit == null) return NotFound("Không tìm thấy đơn cọc.");

            if (cancel || status == "CANCELLED")
            {
                deposit.Status = DepositStatus.Refunded;
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "Bạn đã hủy thanh toán cọc.";
                return RedirectToAction("Detail", "Home", new { id = deposit.RoomId });
            }

            if (status == "PAID")
            {
                try
                {
                    // Xác minh với PayOS của Chủ trọ
                    var landlord = await _context.Users.FindAsync(deposit.Room.OwnerId);
                    bool verified = landlord != null && await _paymentService.VerifyPaymentAsync(deposit.Id, landlord.Id);

                    if (verified && deposit.Status != DepositStatus.Paid)
                    {
                        deposit.Status = DepositStatus.Paid;
                        deposit.PaidAt = DateTime.UtcNow;
                        deposit.ExpiresAt = DateTime.UtcNow.AddDays(3);
                        deposit.TransactionId = id ?? string.Empty;

                        if (deposit.Room != null)
                            deposit.Room.IsReserved = true;

                        // 1. Tạo thông báo cho Người thuê (Renter)
                        var renterNotif = new NotificationModel
                        {
                            UserId = deposit.RenterId,
                            Title = "Đặt cọc phòng thành công",
                            Content = $"Bạn đã đặt cọc giữ chỗ thành công phòng trọ '{deposit.Room?.Title}'. Số tiền cọc: {deposit.Amount:N0} VNĐ. Phòng được giữ trong 3 ngày.",
                            Type = "Payment",
                            LinkUrl = "/Deposit",
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        };
                        _context.Notifications.Add(renterNotif);

                        // 2. Tạo thông báo cho Chủ trọ (Landlord)
                        if (landlord != null)
                        {
                            var landlordNotif = new NotificationModel
                            {
                                UserId = landlord.Id,
                                Title = "Có giao dịch đặt cọc mới",
                                Content = $"Khách thuê đã đặt cọc thành công phòng '{deposit.Room?.Title}'. Số tiền cọc: {deposit.Amount:N0} VNĐ đã được chuyển trực tiếp vào tài khoản PayOS của bạn.",
                                Type = "Payment",
                                LinkUrl = "/Deposit",
                                IsRead = false,
                                CreatedAt = DateTime.UtcNow
                            };
                            _context.Notifications.Add(landlordNotif);
                        }

                        await _context.SaveChangesAsync();

                        // Gửi SignalR thông báo thời gian thực
                        try
                        {
                            var renterConns = _connectionManager.GetUserConnections(deposit.RenterId);
                            if (renterConns != null && renterConns.Count > 0)
                            {
                                await _hubContext.Clients.Clients(renterConns).SendAsync("ReceiveNotification", $"Đặt cọc phòng '{deposit.Room?.Title}' thành công.");
                            }

                            if (landlord != null)
                            {
                                var landlordConns = _connectionManager.GetUserConnections(landlord.Id);
                                if (landlordConns != null && landlordConns.Count > 0)
                                {
                                    await _hubContext.Clients.Clients(landlordConns).SendAsync("ReceiveNotification", $"Có giao dịch đặt cọc mới cho phòng '{deposit.Room?.Title}'.");
                                }
                            }
                        }
                        catch { /* Bỏ qua lỗi SignalR nếu có */ }

                        TempData["SuccessMessage"] = "🎉 Thanh toán cọc thành công! Phòng đã được giữ cho bạn trong 3 ngày.";
                    }
                }
                catch (Exception)
                {
                    TempData["ErrorMessage"] = "Lỗi khi xác minh thanh toán. Vui lòng liên hệ hỗ trợ.";
                }
            }

            return RedirectToAction("Detail", "Home", new { id = deposit.RoomId });
        }

        // -------------------------------------------------------
        // Trang hiển thị mã VietQR chuyển khoản trực tiếp
        // -------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> VietQrPayment(string depositId)
        {
            var deposit = await _context.Deposits
                .Include(d => d.Room)
                .Include(d => d.Renter)
                .FirstOrDefaultAsync(d => d.Id == depositId);

            if (deposit == null) return NotFound("Không tìm thấy đơn cọc.");

            var landlord = await _context.Users.FindAsync(deposit.Room.OwnerId);
            if (landlord == null || !landlord.HasVietQrConfigured)
            {
                TempData["ErrorMessage"] = "Chủ trọ chưa cấu hình thông tin chuyển khoản VietQR.";
                return RedirectToAction("Detail", "Home", new { id = deposit.RoomId });
            }

            ViewBag.Landlord = landlord;
            return View(deposit);
        }

        // -------------------------------------------------------
        // Xác nhận chuyển khoản VietQR (Simulation / Manual confirmation)
        // -------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> ConfirmVietQrPayment(string depositId)
        {
            var deposit = await _context.Deposits
                .Include(d => d.Room)
                .FirstOrDefaultAsync(d => d.Id == depositId);

            if (deposit == null) return NotFound("Không tìm thấy đơn cọc.");

            if (deposit.Status != DepositStatus.Paid)
            {
                deposit.Status = DepositStatus.Paid;
                deposit.PaidAt = DateTime.UtcNow;
                deposit.ExpiresAt = DateTime.UtcNow.AddDays(3);
                deposit.TransactionId = "VIETQR-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                if (deposit.Room != null)
                    deposit.Room.IsReserved = true;

                // 1. Tạo thông báo cho Renter
                var renterNotif = new NotificationModel
                {
                    UserId = deposit.RenterId,
                    Title = "Đặt cọc phòng (VietQR) đang chờ xác nhận",
                    Content = $"Bạn đã báo cáo chuyển khoản thành công phòng '{deposit.Room?.Title}'. Số tiền: {deposit.Amount:N0} VNĐ. Chủ nhà đang đối soát.",
                    Type = "Payment",
                    LinkUrl = "/Deposit",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(renterNotif);

                // 2. Tạo thông báo cho Landlord
                var landlord = await _context.Users.FindAsync(deposit.Room.OwnerId);
                if (landlord != null)
                {
                    var landlordNotif = new NotificationModel
                    {
                        UserId = landlord.Id,
                        Title = "Khách thuê báo đã chuyển khoản cọc",
                        Content = $"Khách thuê đã báo hoàn thành chuyển cọc phòng '{deposit.Room?.Title}'. Số tiền: {deposit.Amount:N0} VNĐ. Vui lòng check tài khoản ngân hàng của bạn.",
                        Type = "Payment",
                        LinkUrl = "/Deposit",
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Notifications.Add(landlordNotif);
                }

                await _context.SaveChangesAsync();

                // Gửi Realtime SignalR
                try
                {
                    var renterConns = _connectionManager.GetUserConnections(deposit.RenterId);
                    if (renterConns != null && renterConns.Count > 0)
                    {
                        await _hubContext.Clients.Clients(renterConns).SendAsync("ReceiveNotification", $"Đã gửi thông báo đặt cọc phòng '{deposit.Room?.Title}' đến chủ nhà.");
                    }

                    if (landlord != null)
                    {
                        var landlordConns = _connectionManager.GetUserConnections(landlord.Id);
                        if (landlordConns != null && landlordConns.Count > 0)
                        {
                            await _hubContext.Clients.Clients(landlordConns).SendAsync("ReceiveNotification", $"Khách đã chuyển cọc VietQR cho phòng '{deposit.Room?.Title}'.");
                        }
                    }
                }
                catch { }

                TempData["SuccessMessage"] = "🎉 Bạn đã gửi thông báo chuyển khoản thành công! Vui lòng chờ chủ nhà kiểm tra số dư.";
            }

            return RedirectToAction("Index");
        }
    }
}

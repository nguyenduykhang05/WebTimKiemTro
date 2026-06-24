using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models;
using SmartRoomFinder.Services;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace SmartRoomFinder.Controllers
{
    [Authorize(Roles = "Landlord")]
    public class ServicePricingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;

        public ServicePricingController(AppDbContext context, IPaymentService paymentService)
        {
            _context = context;
            _paymentService = paymentService;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    ViewBag.CurrentPackage = user.CurrentPackage;
                    ViewBag.PackageExpiresAt = user.PackageExpiresAt;
                    
                    // Check if first time buyer
                    bool hasBought = await _context.ServiceTransactions.AnyAsync(p => p.UserId == userId && p.Status == "Paid");
                    ViewBag.IsFirstTimeBuyer = !hasBought;
                }
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(string package, int days)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return BadRequest();

            if (!Enum.TryParse<RoomPackage>(package, out var selectedPackage))
            {
                return BadRequest("Gói dịch vụ không hợp lệ.");
            }

            if (selectedPackage == RoomPackage.Default)
            {
                TempData["ErrorMessage"] = "Tin thường là mặc định và hoàn toàn miễn phí, không cần mua.";
                return RedirectToAction("Index");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return BadRequest();

            // Allow upgrade only to HIGHER package
            if (user.PackageExpiresAt.HasValue && user.PackageExpiresAt.Value > DateTime.UtcNow)
            {
                if (selectedPackage <= user.CurrentPackage)
                {
                    TempData["ErrorMessage"] = "Bạn chỉ có thể nâng cấp lên gói dịch vụ cao hơn gói hiện tại của bạn.";
                    return RedirectToAction("Index");
                }
            }

            double pricePer5Days = selectedPackage switch
            {
                RoomPackage.VipNoiBat => 351000,
                RoomPackage.Vip1 => 210600,
                RoomPackage.Vip2 => 140400,
                RoomPackage.Vip3 => 86400,
                _ => 0
            };

            int chunks = days / 5;
            double amount = pricePer5Days * chunks;

            bool isFirstTimeBuyer = !await _context.ServiceTransactions.AnyAsync(p => p.UserId == userId && p.Status == "Paid");

            if (selectedPackage == RoomPackage.Vip1 && days == 5 && isFirstTimeBuyer)
            {
                amount = 1000;
            }
            else if (days == 30)
            {
                amount = amount * 0.8; // Giảm 20%
            }

            var transaction = new ServiceTransactionModel
            {
                UserId = userId,
                RoomId = null,
                Package = selectedPackage,
                Days = days,
                Amount = amount,
                Status = "Pending"
            };

            _context.ServiceTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Pass empty string for roomId to payment service, or maybe modify the signature later
            var checkoutUrl = await _paymentService.CreateServicePaymentLinkAsync(transaction.Id, "", amount);
            return Redirect(checkoutUrl);
        }

        [AllowAnonymous]
        public async Task<IActionResult> PaymentCallback(string transactionId, string? status, bool cancel = false)
        {
            var transaction = await _context.ServiceTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) return NotFound();

            if (cancel || status == "CANCELLED")
            {
                transaction.Status = "Cancelled";
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "Bạn đã hủy thanh toán dịch vụ đăng tin.";
                return RedirectToAction("Manage", "Room");
            }

            if (status == "PAID")
            {
                bool isVerified = await _paymentService.VerifyServicePaymentAsync(transactionId);
                if (isVerified)
                {
                    transaction.Status = "Paid";
                    
                    if (transaction.User != null)
                    {
                        transaction.User.CurrentPackage = transaction.Package;
                        
                        // Extend expiration date or set new one
                        if (transaction.User.PackageExpiresAt.HasValue && transaction.User.PackageExpiresAt.Value > DateTime.UtcNow)
                        {
                            transaction.User.PackageExpiresAt = transaction.User.PackageExpiresAt.Value.AddDays(transaction.Days);
                        }
                        else
                        {
                            transaction.User.PackageExpiresAt = DateTime.UtcNow.AddDays(transaction.Days);
                        }

                        // Bonus: Update all existing active rooms of this user to have the new package
                        var userRooms = await _context.Rooms.Where(r => r.OwnerId == transaction.UserId && r.IsActive).ToListAsync();
                        foreach(var r in userRooms)
                        {
                            r.Package = transaction.Package;
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Thanh toán thành công! Tài khoản của bạn đã được nâng cấp lên gói {transaction.Package} với thời hạn {transaction.Days} ngày.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Xác minh thanh toán thất bại. Vui lòng liên hệ hỗ trợ.";
                }
            }

            return RedirectToAction("Manage", "Room");
        }
    }
}

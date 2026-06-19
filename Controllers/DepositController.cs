using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models;
using SmartRoomFinder.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PayOS;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace SmartRoomFinder.Controllers
{
    [Authorize]
    public class DepositController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly PayOSClient _payOS;

        public DepositController(AppDbContext context, IPaymentService paymentService, PayOSClient payOS)
        {
            _context = context;
            _paymentService = paymentService;
            _payOS = payOS;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isLandlord = User.IsInRole("Landlord");

            IQueryable<DepositModel> query = _context.Deposits
                .Include(d => d.Room)
                .Include(d => d.Renter);

            if (isLandlord)
            {
                query = query.Where(d => d.Room.OwnerId == userId);
            }
            else
            {
                query = query.Where(d => d.RenterId == userId);
            }

            var deposits = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return View("MyDeposits", deposits);
        }

        [HttpPost]
        [Authorize(Roles = "Renter")]
        public async Task<IActionResult> Create(string roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null || !room.IsActive || room.IsReserved)
            {
                return BadRequest("Phòng không tồn tại hoặc đã được đặt.");
            }
            double amount = room.DepositAmount > 0 ? room.DepositAmount : 500000;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Check if user already has a pending deposit for this room
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
                    RenterId = userId,
                    Amount = amount,
                    ExpiresAt = DateTime.UtcNow.AddDays(3) // Will be reset when paid
                };
                _context.Deposits.Add(deposit);
                await _context.SaveChangesAsync();
            }

            var checkoutUrl = await _paymentService.CreateDepositPaymentLinkAsync(deposit.Id, roomId, amount);
            return Redirect(checkoutUrl);
        }

        [HttpGet]
        public async Task<IActionResult> PaymentCallback(string depositId, bool cancel = false, string? code = null, string? id = null, string? orderCode = null, string? status = null)
        {
            var deposit = await _context.Deposits.Include(d => d.Room).FirstOrDefaultAsync(d => d.Id == depositId);
            if (deposit == null) return NotFound("Không tìm thấy đơn cọc");

            if (cancel || status == "CANCELLED")
            {
                deposit.Status = DepositStatus.Refunded; // Mark as cancelled
                await _context.SaveChangesAsync();
                TempData["ErrorMessage"] = "Bạn đã hủy thanh toán cọc.";
                return RedirectToAction("Detail", "Home", new { id = deposit.RoomId });
            }

            if (status == "PAID")
            {
                // Verify with PayOS
                try 
                {
                    var paymentLinkInformation = await _payOS.PaymentRequests.GetAsync(long.Parse(deposit.OrderCode.ToString()));
                    if (paymentLinkInformation.Status == PayOS.Models.V2.PaymentRequests.PaymentLinkStatus.Paid)
                    {
                        if (deposit.Status != DepositStatus.Paid)
                        {
                            deposit.Status = DepositStatus.Paid;
                            deposit.PaidAt = DateTime.UtcNow;
                            deposit.ExpiresAt = DateTime.UtcNow.AddDays(3); // 3 days to sign contract
                            deposit.TransactionId = id ?? paymentLinkInformation.Id;
                            
                            if (deposit.Room != null)
                            {
                                deposit.Room.IsReserved = true;
                            }
                            await _context.SaveChangesAsync();
                        }
                        TempData["SuccessMessage"] = "Thanh toán cọc thành công! Phòng đã được giữ cho bạn trong 3 ngày.";
                        return RedirectToAction("Detail", "Home", new { id = deposit.RoomId });
                    }
                }
                catch(Exception ex)
                {
                    // Log error
                    TempData["ErrorMessage"] = "Lỗi khi xác minh thanh toán.";
                    return RedirectToAction("Detail", "Home", new { id = deposit.RoomId });
                }
            }

            return RedirectToAction("Detail", "Home", new { id = deposit.RoomId });
        }
    }
}

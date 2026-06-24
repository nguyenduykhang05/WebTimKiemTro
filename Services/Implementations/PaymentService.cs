using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PayOS;
using PayOS.Models.V2.PaymentRequests;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models;

namespace SmartRoomFinder.Services.Implementations
{
    /// <summary>
    /// Dịch vụ thanh toán: tạo link PayOS dùng cấu hình riêng của từng Chủ trọ.
    /// Tiền cọc chạy thẳng vào tài khoản ngân hàng của Chủ trọ, không qua Admin.
    /// </summary>
    public class PaymentService : IPaymentService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly PayOSClient _adminPayOs;

        public PaymentService(AppDbContext context, IHttpContextAccessor httpContextAccessor, PayOSClient adminPayOs)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _adminPayOs = adminPayOs;
        }

        public async Task<string> CreateDepositPaymentLinkAsync(string depositId, string roomId, double amount)
        {
            var deposit = await _context.Deposits
                .Include(d => d.Room)
                .FirstOrDefaultAsync(d => d.Id == depositId);

            if (deposit == null) throw new InvalidOperationException("Không tìm thấy giao dịch cọc.");

            // Lấy thông tin chủ trọ để dùng PayOS riêng
            var landlord = await _context.Users.FindAsync(deposit.Room.OwnerId);
            if (landlord == null || !landlord.HasPayOsConfigured)
                throw new InvalidOperationException("Chủ trọ chưa cấu hình cổng thanh toán. Vui lòng liên hệ chủ trọ.");

            // Khởi tạo PayOS Client với credentials riêng của chủ trọ
            var landlordPayOs = new PayOSClient(
                landlord.PayOsClientId,
                landlord.PayOsApiKey,
                landlord.PayOsChecksumKey
            );

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null
                ? $"{request.Scheme}://{request.Host}"
                : "https://localhost:58489";

            long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            deposit.OrderCode = orderCode;
            await _context.SaveChangesAsync();

            // Giới hạn description ≤ 25 ký tự theo yêu cầu PayOS
            var title = deposit.Room.Title;
            var desc = "Dat coc: " + (title.Length > 16 ? title[..16] : title);

            var createRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (long)Math.Round(amount),
                Description = desc,
                CancelUrl = $"{baseUrl}/Deposit/PaymentCallback?depositId={depositId}&cancel=true",
                ReturnUrl = $"{baseUrl}/Deposit/PaymentCallback?depositId={depositId}&status=PAID",
                Items = new List<PaymentLinkItem>
                {
                    new PaymentLinkItem
                    {
                        Name = title.Length > 50 ? title[..50] : title,
                        Quantity = 1,
                        Price = (long)Math.Round(amount)
                    }
                }
            };

            var response = await landlordPayOs.PaymentRequests.CreateAsync(createRequest);
            return response.CheckoutUrl;
        }

        public async Task<bool> VerifyPaymentAsync(string depositId, string landlordUserId)
        {
            var deposit = await _context.Deposits
                .Include(d => d.Room)
                .FirstOrDefaultAsync(d => d.Id == depositId);

            if (deposit == null) return false;

            var landlord = await _context.Users.FindAsync(landlordUserId);
            if (landlord == null || !landlord.HasPayOsConfigured) return false;

            var landlordPayOs = new PayOSClient(
                landlord.PayOsClientId,
                landlord.PayOsApiKey,
                landlord.PayOsChecksumKey
            );

            try
            {
                var info = await landlordPayOs.PaymentRequests.GetAsync(deposit.OrderCode);
                return info?.Status == PaymentLinkStatus.Paid;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> CreateServicePaymentLinkAsync(string transactionId, string roomId, double amount)
        {
            var transaction = await _context.ServiceTransactions
                .Include(t => t.Room)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (transaction == null) throw new InvalidOperationException("Không tìm thấy giao dịch.");

            var request = _httpContextAccessor.HttpContext?.Request;
            var baseUrl = request != null
                ? $"{request.Scheme}://{request.Host}"
                : "https://localhost:58489";

            long orderCode = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            transaction.OrderCode = orderCode;
            await _context.SaveChangesAsync();

            var title = transaction.Room?.Title ?? "Phong tro";
            var desc = "Dich vu: " + (title.Length > 16 ? title[..16] : title);

            var createRequest = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (long)Math.Round(amount),
                Description = desc,
                CancelUrl = $"{baseUrl}/ServicePricing/PaymentCallback?transactionId={transactionId}&cancel=true",
                ReturnUrl = $"{baseUrl}/ServicePricing/PaymentCallback?transactionId={transactionId}&status=PAID",
                Items = new List<PaymentLinkItem>
                {
                    new PaymentLinkItem
                    {
                        Name = "Dịch vụ đăng tin - " + transaction.Package.ToString(),
                        Quantity = 1,
                        Price = (long)Math.Round(amount)
                    }
                }
            };

            var response = await _adminPayOs.PaymentRequests.CreateAsync(createRequest);
            return response.CheckoutUrl;
        }

        public async Task<bool> VerifyServicePaymentAsync(string transactionId)
        {
            var transaction = await _context.ServiceTransactions.FirstOrDefaultAsync(t => t.Id == transactionId);
            if (transaction == null) return false;

            try
            {
                var info = await _adminPayOs.PaymentRequests.GetAsync(transaction.OrderCode);
                return info?.Status == PaymentLinkStatus.Paid;
            }
            catch
            {
                return false;
            }
        }
    }
}

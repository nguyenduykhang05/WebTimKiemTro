using PayOS;
using PayOS.Models.V2.PaymentRequests;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;

namespace SmartRoomFinder.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly PayOSClient _payOS;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(PayOSClient payOS, AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _payOS = payOS;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> CreateDepositPaymentLinkAsync(string depositId, string roomId, double amount)
        {
            var deposit = await _context.Deposits.FindAsync(depositId);
            if (deposit == null) throw new Exception("Deposit not found");

            var request = _httpContextAccessor.HttpContext.Request;
            var domain = $"{request.Scheme}://{request.Host}";

            
            // Generate numeric orderCode (max 53 bits)
            var orderCode = int.Parse(DateTimeOffset.Now.ToString("ffffff")) + new Random().Next(1000, 9999);
            deposit.OrderCode = orderCode;
            await _context.SaveChangesAsync();
            var paymentData = new CreatePaymentLinkRequest
            {
                OrderCode = orderCode,
                Amount = (int)amount,
                Description = "Coc phong",
                CancelUrl = $"{domain}/Deposit/PaymentCallback?depositId={depositId}",
                ReturnUrl = $"{domain}/Deposit/PaymentCallback?depositId={depositId}",
                Items = new List<PaymentLinkItem> { new PaymentLinkItem { Name = "Coc phong", Quantity = 1, Price = (int)amount } }
            };

            var createPayment = await _payOS.PaymentRequests.CreateAsync(paymentData);

            return createPayment.CheckoutUrl;
        }
    }
}

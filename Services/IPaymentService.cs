using System.Threading.Tasks;

namespace SmartRoomFinder.Services
{
    public interface IPaymentService
    {
        Task<string> CreateDepositPaymentLinkAsync(string depositId, string roomId, double amount);
        Task<bool> VerifyPaymentAsync(string depositId, string landlordUserId);

        Task<string> CreateServicePaymentLinkAsync(string transactionId, string roomId, double amount);
        Task<bool> VerifyServicePaymentAsync(string transactionId);
    }
}

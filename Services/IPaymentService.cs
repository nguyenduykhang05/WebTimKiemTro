using System.Threading.Tasks;

namespace SmartRoomFinder.Services
{
    public interface IPaymentService
    {
        Task<string> CreateDepositPaymentLinkAsync(string depositId, string roomId, double amount);
    }
}

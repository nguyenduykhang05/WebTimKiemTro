using SmartRoomFinder.Models.ViewModels;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardViewModel> GetDashboardStatsAsync(string? filterStatus = null);
        Task<bool> VerifyRoomAsync(string roomId, string note, string adminId, string adminName);
        Task<bool> RejectRoomAsync(string roomId, string note, string adminId, string adminName);
        Task<bool> RequestRoomInfoAsync(string roomId, string note, string adminId, string adminName);
        Task<bool> ToggleHideReviewAsync(string reviewId);
        Task<bool> ResolveReportAsync(string reportId);
        Task<bool> HideReportedRoomAsync(string reportId);
    }
}

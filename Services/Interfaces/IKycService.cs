using System.Threading.Tasks;
using SmartRoomFinder.Models;

namespace SmartRoomFinder.Services.Interfaces
{
    public interface IKycService
    {
        /// <summary>Lấy hồ sơ KYC của user (null nếu chưa nộp).</summary>
        Task<KycProfileModel?> GetKycProfileAsync(string userId);

        /// <summary>Chủ trọ nộp hồ sơ KYC lần đầu hoặc nộp lại sau khi bị từ chối.</summary>
        Task<KycProfileModel> SubmitKycAsync(
            string userId,
            string identityCardNumber,
            string frontImageUrl,
            string backImageUrl,
            string selfieImageUrl);

        /// <summary>Admin phê duyệt KYC → IsKycVerified = true.</summary>
        Task<bool> ApproveKycAsync(string kycId, string adminId);

        /// <summary>Admin từ chối KYC → IsKycVerified = false, lưu lý do.</summary>
        Task<bool> RejectKycAsync(string kycId, string adminId, string reason);

        /// <summary>Lấy danh sách tất cả hồ sơ KYC đang Pending để Admin duyệt.</summary>
        Task<List<KycProfileModel>> GetPendingKycListAsync();

        /// <summary>Lấy toàn bộ hồ sơ KYC để Admin tổng quan.</summary>
        Task<List<KycProfileModel>> GetAllKycListAsync();
    }
}

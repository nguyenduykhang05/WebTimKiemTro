using SmartRoomFinder.Models;
using SmartRoomFinder.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Interfaces
{
    public interface IRoomService
    {
        Task<List<RoomModel>> GetLandlordRoomsAsync(string landlordId);
        Task<RoomModel?> GetRoomByIdAsync(string id);
        Task<string> CreateRoomAsync(RoomCreateViewModel model, string ownerId, string ownerName);
        Task<bool> UpdateRoomAsync(RoomEditViewModel model, string ownerId);
        Task<bool> DeleteRoomAsync(string id, string ownerId);
        Task<LandlordStatsViewModel> GetLandlordStatsAsync(string landlordId);
    }
}

using SmartRoomFinder.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Interfaces
{
    public interface IRecommendationService
    {
        Task<List<RecommendedRoomDto>> GetSimilarRoomsAsync(string roomId, int limit = 5);
        Task<List<RecommendedRoomDto>> GetRecommendationsForUserAsync(string userId, int limit = 10);
    }
}

using Microsoft.AspNetCore.Mvc;
using SmartRoomFinder.Services.Interfaces;
using System.Threading.Tasks;

namespace SmartRoomFinder.Controllers.Api
{
    [Route("api/recommendations")]
    [ApiController]
    public class RecommendationApiController : ControllerBase
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationApiController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("similar/{roomId}")]
        public async Task<IActionResult> GetSimilarRooms(string roomId, [FromQuery] int limit = 5)
        {
            var recommendations = await _recommendationService.GetSimilarRoomsAsync(roomId, limit);
            return Ok(new
            {
                success = true,
                data = recommendations
            });
        }
    }
}

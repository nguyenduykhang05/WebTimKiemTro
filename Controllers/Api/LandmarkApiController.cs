using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models.DTOs;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRoomFinder.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class LandmarkApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public LandmarkApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLandmarks()
        {
            var landmarks = await _context.Set<LandmarkModel>()
                .Where(l => l.IsActive)
                .ToListAsync();

            return Ok(ApiResponse<System.Collections.Generic.IEnumerable<LandmarkModel>>.Ok(landmarks, landmarks.Count, "Landmarks retrieved successfully"));
        }
    }
}

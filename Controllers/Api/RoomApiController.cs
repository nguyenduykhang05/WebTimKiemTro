using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Helpers;
using SmartRoomFinder.Models;
using SmartRoomFinder.Models.DTOs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SmartRoomFinder.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RoomApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("radius")]
        public async Task<IActionResult> GetRoomsInRadius([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radiusKm)
        {
            if (radiusKm <= 0 || radiusKm > 50)
            {
                return BadRequest(ApiResponse<List<RoomMapDto>>.Fail("Invalid radius. Must be between 0 and 50 km."));
            }

            // 1. Calculate Bounding Box
            var bbox = HaversineHelper.GetBoundingBox(lat, lng, radiusKm);

            // 2. Query Database using Bounding Box (Fast Index Scan)
            var candidates = await _context.Rooms
                .Where(r => r.IsActive && !r.IsDraft &&
                            r.Latitude >= bbox.MinLat && r.Latitude <= bbox.MaxLat &&
                            r.Longitude >= bbox.MinLng && r.Longitude <= bbox.MaxLng)
                .ToListAsync();

            // 3. Apply exact Haversine filter in memory
            var results = new List<RoomMapDto>();
            foreach (var room in candidates)
            {
                var distance = HaversineHelper.CalculateDistance(lat, lng, room.Latitude, room.Longitude);
                if (distance <= radiusKm)
                {
                    results.Add(new RoomMapDto
                    {
                        Id = room.Id,
                        Title = room.Title,
                        Price = room.Price,
                        Area = room.Area,
                        Address = room.Address,
                        Latitude = room.Latitude,
                        Longitude = room.Longitude,
                        ImageUrl = room.MainImageUrl,
                        DistanceKm = Math.Round(distance, 2)
                    });
                }
            }

            // Sort by nearest
            results = results.OrderBy(r => r.DistanceKm).ToList();

            return Ok(ApiResponse<List<RoomMapDto>>.Ok(results, results.Count, "Rooms found within radius"));
        }
    }
}

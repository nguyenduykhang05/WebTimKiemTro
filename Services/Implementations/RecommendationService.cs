using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Models.DTOs;
using SmartRoomFinder.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Implementations
{
    public class RecommendationService : IRecommendationService
    {
        private readonly AppDbContext _context;

        public RecommendationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<RecommendedRoomDto>> GetSimilarRoomsAsync(string roomId, int limit = 5)
        {
            var currentRoom = await _context.Rooms.FindAsync(roomId);
            if (currentRoom == null) return new List<RecommendedRoomDto>();

            // For optimization, use bounding box to filter rooms within ~5km
            // 1 degree latitude ~ 111km -> 5km ~ 0.045 degrees
            double maxDistKm = 5.0;
            double degreeDelta = maxDistKm / 111.0;

            var candidates = await _context.Rooms
                .Where(r => r.IsActive && !r.IsDraft && r.Id != roomId)
                .Where(r => r.Latitude >= currentRoom.Latitude - degreeDelta && 
                            r.Latitude <= currentRoom.Latitude + degreeDelta &&
                            r.Longitude >= currentRoom.Longitude - degreeDelta && 
                            r.Longitude <= currentRoom.Longitude + degreeDelta)
                .ToListAsync();

            var scoredRooms = new List<Tuple<RoomModel, double, string>>();

            double maxPriceDiff = 2000000.0; // 2 million VND
            double maxAreaDiff = 20.0; // 20 m2

            foreach (var room in candidates)
            {
                // 1. Location Similarity (Haversine)
                double distance = CalculateHaversineDistance(currentRoom.Latitude, currentRoom.Longitude, room.Latitude, room.Longitude);
                if (distance > maxDistKm) continue; // skip if really outside 5km after haversine check

                double sLocation = Math.Max(0, 1.0 - (distance / maxDistKm));

                // 2. Price Similarity
                double sPrice = 1.0 - Math.Min(Math.Abs(currentRoom.Price - room.Price) / maxPriceDiff, 1.0);

                // 3. Area Similarity
                double sArea = 1.0 - Math.Min(Math.Abs(currentRoom.Area - room.Area) / maxAreaDiff, 1.0);

                // 4. Amenities Similarity (Jaccard)
                var currentAm = new HashSet<string>(currentRoom.Amenities.Select(a => a.ToLower()));
                var roomAm = new HashSet<string>(room.Amenities.Select(a => a.ToLower()));
                double sAmenities = CalculateJaccardSimilarity(currentAm, roomAm);

                // 5. Type Similarity
                double sType = (currentRoom.Type == room.Type) ? 1.0 : 0.0;

                // Combine (Weights: Loc 35%, Price 30%, Type 15%, Area 10%, Amenities 10%)
                double totalScore = (0.35 * sLocation) + (0.30 * sPrice) + (0.15 * sType) + (0.10 * sArea) + (0.10 * sAmenities);

                // Determine Match Reason
                string reason = GetMatchReason(sLocation, sPrice, sType, distance, room.Type);

                scoredRooms.Add(new Tuple<RoomModel, double, string>(room, totalScore * 100, reason));
            }

            var recommendations = scoredRooms
                .OrderByDescending(x => x.Item2)
                .Take(limit)
                .Select(x => new RecommendedRoomDto
                {
                    Id = x.Item1.Id,
                    Title = x.Item1.Title,
                    Price = x.Item1.Price,
                    Area = x.Item1.Area,
                    Address = x.Item1.Address,
                    MainImageUrl = x.Item1.MainImageUrl,
                    SimilarityScore = Math.Round(x.Item2, 1),
                    MatchReason = x.Item3,
                    PostedAt = x.Item1.PostedAt,
                    IsVerified = x.Item1.IsVerified
                })
                .ToList();

            return recommendations;
        }

        public Task<List<RecommendedRoomDto>> GetRecommendationsForUserAsync(string userId, int limit = 10)
        {
            // Future implementation for Collaborative Filtering
            throw new NotImplementedException();
        }

        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth radius in km
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        private double CalculateJaccardSimilarity(HashSet<string> setA, HashSet<string> setB)
        {
            if (!setA.Any() && !setB.Any()) return 1.0;
            var intersection = setA.Intersect(setB).Count();
            var union = setA.Union(setB).Count();
            return union == 0 ? 0 : (double)intersection / union;
        }

        private string GetMatchReason(double sLocation, double sPrice, double sType, double distance, RoomType type)
        {
            if (sLocation > 0.8 && sPrice > 0.8 && sType == 1.0)
            {
                return $"Cùng loại {type}, cách {distance:F1}km và cùng tầm giá";
            }
            if (sLocation > 0.8 && sPrice > 0.8)
            {
                return $"Cách {distance:F1}km và cùng tầm giá";
            }
            if (sLocation > 0.8)
            {
                return $"Gần bạn (cách {distance:F1}km)";
            }
            if (sPrice > 0.8)
            {
                return $"Cùng tầm giá";
            }
            return "Phù hợp với nhu cầu của bạn";
        }
    }
}

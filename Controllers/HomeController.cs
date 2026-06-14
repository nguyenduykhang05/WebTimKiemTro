using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;

using SmartRoomFinder.Services.Interfaces;

namespace SmartRoomFinder.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRecommendationService _recommendationService;

        public HomeController(AppDbContext context, IRecommendationService recommendationService)
        {
            _context = context;
            _recommendationService = recommendationService;
        }

        public async Task<IActionResult> Index(string? search, string? location, RoomType? type, double? maxPrice, double? minArea, int? rating)
        {
            var query = _context.Rooms
                .Where(r => r.IsActive && !r.IsDraft);

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Title.Contains(search) || r.Description.Contains(search));
            }

            // Apply location filter
            if (!string.IsNullOrEmpty(location))
            {
                query = query.Where(r => r.Location.Contains(location));
            }

            // Apply type filter
            if (type.HasValue)
            {
                query = query.Where(r => r.Type == type.Value);
            }

            // Apply price filter
            if (maxPrice.HasValue)
            {
                query = query.Where(r => r.Price <= maxPrice.Value);
            }

            // Apply area filter
            if (minArea.HasValue)
            {
                query = query.Where(r => r.Area >= minArea.Value);
            }

            // Apply rating filter
            if (rating.HasValue)
            {
                if (rating.Value == 5)
                {
                    query = query.Where(r => r.Rating >= 4.8); // 5 sao có thể là gần 5
                }
                else if (rating.Value == 4)
                {
                    query = query.Where(r => r.Rating >= 4.0);
                }
            }

            var rooms = await query.OrderByDescending(r => r.PostedAt).ToListAsync();

            ViewData["Search"] = search;
            ViewData["Location"] = location;
            ViewData["MaxPrice"] = maxPrice;
            ViewData["Rating"] = rating;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var favoriteRoomIds = new List<string>();
            if (userId != null)
            {
                favoriteRoomIds = await _context.UserFavorites
                    .Where(f => f.UserId == userId)
                    .Select(f => f.RoomId)
                    .ToListAsync();
            }
            ViewData["FavoriteRoomIds"] = favoriteRoomIds;

            // Store current filter values in ViewData to restore in View
            ViewData["Search"] = search;
            ViewData["Location"] = location;
            ViewData["Type"] = type;
            ViewData["MaxPrice"] = maxPrice;
            ViewData["MinArea"] = minArea;

            return View(rooms);
        }

        public async Task<IActionResult> Detail(string id)
        {
            var room = await _context.Rooms
                .Include(r => r.Images)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (room == null)
            {
                return NotFound();
            }

            // Increment view count
            room.ViewCount++;
            await _context.SaveChangesAsync();

            // Fetch reviews
            var reviews = await _context.Reviews
                .Where(r => r.RoomId == id && !r.IsHidden)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            ViewData["Reviews"] = reviews;

            // Check if user is the owner
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewData["IsOwner"] = currentUserId == room.OwnerId;
            ViewData["IsRenter"] = User.IsInRole("Renter");

            // Allow any authenticated user who is not the owner to leave a comment/review
            bool canReview = false;
            var favoriteRoomIds = new List<string>();
            if (currentUserId != null)
            {
                canReview = currentUserId != room.OwnerId; // Changed from application status
                
                favoriteRoomIds = await _context.UserFavorites
                    .Where(f => f.UserId == currentUserId)
                    .Select(f => f.RoomId)
                    .ToListAsync();
            }
            ViewData["CanReview"] = canReview;
            ViewData["FavoriteRoomIds"] = favoriteRoomIds;

            return View(room);
        }

        public async Task<IActionResult> SimilarRooms(string id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
            {
                return NotFound();
            }

            ViewData["OriginalRoomTitle"] = room.Title;
            ViewData["OriginalRoomId"] = room.Id;

            // Lấy 20 phòng tương tự
            var similarRooms = await _recommendationService.GetSimilarRoomsAsync(id, 20);

            return View(similarRooms);
        }

        [Authorize(Roles = "Renter")]
        [HttpPost]
        public async Task<IActionResult> SubmitReview(string roomId, double rating, string comment, string university, string duration)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            // Check if user is allowed to review
            var hasRented = await _context.Applications.AnyAsync(a =>
                a.RenterId == userId && a.RoomId == roomId && a.Status == "approved");

            if (!hasRented)
            {
                return Forbid();
            }

            var user = await _context.Users.FindAsync(userId);
            var userName = user?.Name ?? "Người thuê";

            var review = new ReviewModel
            {
                RoomId = roomId,
                UserId = userId,
                UserName = userName,
                UserAvatar = user?.ProfileImageUrl ?? "/images/default_avatar.png",
                Rating = rating,
                Comment = comment,
                RenterUniversity = university,
                RentalDuration = duration,
                CreatedAt = DateTime.UtcNow
            };

            _context.Reviews.Add(review);

            // Update room rating
            var room = await _context.Rooms.FindAsync(roomId);
            if (room != null)
            {
                var roomReviews = await _context.Reviews.Where(r => r.RoomId == roomId && !r.IsHidden).ToListAsync();
                roomReviews.Add(review);
                room.Rating = roomReviews.Average(r => r.Rating);
                room.TotalReviews = roomReviews.Count;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", new { id = roomId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> SubmitReport(string roomId, string reason, string description)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var report = new ReportModel
            {
                RoomId = roomId,
                ReporterId = userId,
                Reason = reason,
                Description = description ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return RedirectToAction("Detail", new { id = roomId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) return NotFound();

            var favorite = await _context.UserFavorites
                .FirstOrDefaultAsync(f => f.UserId == userId && f.RoomId == id);

            bool isFavorite;
            if (favorite != null)
            {
                // Remove from favorites
                _context.UserFavorites.Remove(favorite);
                isFavorite = false;
            }
            else
            {
                // Add to favorites
                _context.UserFavorites.Add(new UserFavoriteRoomModel
                {
                    UserId = userId,
                    RoomId = id
                });
                isFavorite = true;
            }

            await _context.SaveChangesAsync();

            return Ok(new { isFavorite });
        }

        [Authorize]
        public async Task<IActionResult> Wishlist()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var favoriteRoomIds = await _context.UserFavorites
                .Where(f => f.UserId == userId)
                .Select(f => f.RoomId)
                .ToListAsync();

            var rooms = await _context.Rooms
                .Where(r => favoriteRoomIds.Contains(r.Id))
                .ToListAsync();
                
            return View(rooms);
        }

        public async Task<IActionResult> Map()
        {
            var rooms = await _context.Rooms.Where(r => r.IsActive && !r.IsDraft).ToListAsync();
            return View(rooms);
        }

        public IActionResult Blog()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}

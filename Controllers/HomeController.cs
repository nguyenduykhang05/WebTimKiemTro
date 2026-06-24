using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;

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
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
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

            var rooms = await query
                .OrderByDescending(r => r.Package)
                .ThenByDescending(r => r.PostedAt)
                .ToListAsync();

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
            ViewData["Rating"] = rating;

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

            // Bỏ check owner để test bình luận
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null)
            {
                return NotFound();
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

        public async Task<IActionResult> Blog()
        {
            var articles = new List<dynamic>();
            try
            {
                using var client = new System.Net.Http.HttpClient();
                var response = await client.GetStringAsync("https://api.rss2json.com/v1/api.json?rss_url=https://vnexpress.net/rss/bat-dong-san.rss");
                var result = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(response);
                
                if (result.GetProperty("status").GetString() == "ok")
                {
                    var items = result.GetProperty("items").EnumerateArray();
                    foreach (var item in items)
                    {
                        var title = item.GetProperty("title").GetString();
                        var link = item.GetProperty("link").GetString();
                        var pubDate = item.GetProperty("pubDate").GetString();
                        var thumbnailUrl = "";
                        
                        if (item.TryGetProperty("enclosure", out var enclosure) && enclosure.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            if (enclosure.TryGetProperty("link", out var linkProp))
                            {
                                thumbnailUrl = linkProp.GetString()?.Replace("&amp;", "&");
                            }
                        }
                        
                        var desc = item.GetProperty("description").GetString();
                        
                        // If enclosure is empty, try to extract from description
                        if (string.IsNullOrEmpty(thumbnailUrl) && !string.IsNullOrEmpty(desc) && desc.Contains("<img "))
                        {
                            var match = System.Text.RegularExpressions.Regex.Match(desc, "<img.+?src=[\"'](.+?)[\"'].*?>", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                            if (match.Success)
                            {
                                thumbnailUrl = match.Groups[1].Value.Replace("&amp;", "&");
                            }
                        }

                        // Basic cleanup of description if it contains HTML
                        if (!string.IsNullOrEmpty(desc))
                        {
                            var indexOfCloseTag = desc.LastIndexOf("</a>");
                            if (indexOfCloseTag >= 0)
                            {
                                desc = desc.Substring(indexOfCloseTag + 4).Trim();
                            }
                            desc = System.Text.RegularExpressions.Regex.Replace(desc, "<.*?>", String.Empty);
                        }

                        if (string.IsNullOrEmpty(thumbnailUrl))
                        {
                            thumbnailUrl = "https://images.unsplash.com/photo-1560518883-ce09059eeffa?auto=format&fit=crop&w=600&q=80";
                        }

                        articles.Add(new {
                            Title = title,
                            Link = link,
                            PubDate = pubDate,
                            ThumbnailUrl = thumbnailUrl,
                            Description = desc
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching RSS: {ex.Message}");
            }

            ViewBag.Articles = articles.Take(6).ToList();
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

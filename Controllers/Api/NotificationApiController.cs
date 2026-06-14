using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models.DTOs;

namespace SmartRoomFinder.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NotificationApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationApiController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notifs = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();

            var dtos = notifs.Select(n => new NotificationDto
            {
                Id = n.Id,
                Title = n.Title,
                Content = n.Content,
                Type = n.Type,
                LinkUrl = n.LinkUrl,
                IsRead = n.IsRead,
                CreatedAtStr = GetTimeAgo(n.CreatedAt)
            }).ToList();

            var unreadCount = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

            return Ok(new { success = true, data = dtos, unreadCount = unreadCount });
        }

        [HttpPost("read/{id}")]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notif = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);
            
            if (notif == null) return NotFound();

            notif.IsRead = true;
            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var unreadNotifs = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach(var notif in unreadNotifs)
            {
                notif.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { success = true });
        }

        private string GetTimeAgo(System.DateTime dt)
        {
            var ts = new System.TimeSpan(System.DateTime.UtcNow.Ticks - dt.Ticks);
            double delta = System.Math.Abs(ts.TotalSeconds);

            if (delta < 60) return ts.Seconds == 1 ? "1 giây trước" : ts.Seconds + " giây trước";
            if (delta < 3600) return ts.Minutes == 1 ? "1 phút trước" : ts.Minutes + " phút trước";
            if (delta < 86400) return ts.Hours == 1 ? "1 giờ trước" : ts.Hours + " giờ trước";
            if (delta < 2592000) return ts.Days == 1 ? "1 ngày trước" : ts.Days + " ngày trước";
            
            return dt.ToString("dd/MM/yyyy");
        }
    }
}

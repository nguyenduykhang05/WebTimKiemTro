using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Models.ViewModels;
using SmartRoomFinder.Services.Interfaces;

namespace SmartRoomFinder.Controllers
{
    [Authorize]
    public class RoomController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IRoomService _roomService;

        public RoomController(AppDbContext context, IRoomService roomService)
        {
            _context = context;
            _roomService = roomService;
        }

        [Authorize(Roles = "Landlord")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var stats = await _roomService.GetLandlordStatsAsync(userId ?? "");

            // Fetch top 5 recent applications for dashboard
            ViewBag.RecentApplications = await _context.Applications
                .Where(a => a.OwnerId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(5)
                .ToListAsync();

            return View(stats);
        }

        // List landlord's own rooms
        [Authorize(Roles = "Landlord")]
        public async Task<IActionResult> Manage()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var rooms = await _roomService.GetLandlordRoomsAsync(userId ?? "");
            return View(rooms);
        }

        // Post a new room (GET)
        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RoomCreateViewModel());
        }

        // Post a new room (POST)
        [Authorize(Roles = "Landlord")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Chủ trọ";

            await _roomService.CreateRoomAsync(model, userId ?? "", userName);

            return RedirectToAction(nameof(Manage));
        }

        // Edit room (GET)
        [Authorize(Roles = "Landlord")]
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var room = await _roomService.GetRoomByIdAsync(id);
            if (room == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (room.OwnerId != userId) return Unauthorized();

            var model = new RoomEditViewModel
            {
                Id = room.Id,
                Title = room.Title,
                Description = room.Description,
                Price = room.Price,
                Address = room.Address,
                Location = room.Location,
                Type = room.Type,
                Area = room.Area,
                Bedrooms = room.Bedrooms,
                Direction = room.Direction,
                AmenitiesInput = string.Join(", ", room.Amenities),
                CurrentMainImageUrl = room.MainImageUrl,
                CurrentSubImages = _context.RoomImages.Where(ri => ri.RoomId == room.Id).OrderBy(ri => ri.SortOrder).ToList()
            };

            return View(model);
        }

        // Edit room (POST)
        [Authorize(Roles = "Landlord")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RoomEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var success = await _roomService.UpdateRoomAsync(model, userId);

            if (!success) return NotFound();

            return RedirectToAction(nameof(Manage));
        }

        // Delete room (POST)
        [Authorize(Roles = "Landlord")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
            var success = await _roomService.DeleteRoomAsync(id, userId);

            if (!success) return NotFound();

            return RedirectToAction(nameof(Manage));
        }

        // --- RENTER APPLICATIONS & CHATS LOGIC (Using DbContext for now) ---

        // Submit rental application (POST)
        [Authorize(Roles = "Renter")]
        [HttpPost]
        public async Task<IActionResult> Apply(string roomId, string message, string renterPhone, string expectedMoveInDate)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();

            var renterId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var renterName = User.Identity?.Name ?? "Người thuê";

            var landlord = await _context.Users.FindAsync(room.OwnerId);
            var landlordName = landlord?.Name ?? "Chủ trọ";

            DateTime? moveInDate = null;
            if (DateTime.TryParse(expectedMoveInDate, out var date))
            {
                moveInDate = date;
            }

            var app = new ApplicationModel
            {
                RoomId = room.Id,
                RoomTitle = room.Title,
                RoomImageUrl = room.MainImageUrl,
                OwnerId = room.OwnerId,
                OwnerName = landlordName,
                RenterId = renterId ?? "",
                RenterName = renterName,
                RenterPhone = renterPhone,
                Message = message,
                ExpectedMoveInDate = moveInDate,
                Status = "pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Applications.Add(app);

            // Automatically open a chat between them
            var chat = await _context.Chats.FirstOrDefaultAsync(c =>
                c.RoomId == room.Id && c.RenterId == renterId && c.OwnerId == room.OwnerId);

            if (chat == null)
            {
                chat = new ChatModel
                {
                    RoomId = room.Id,
                    RoomTitle = room.Title,
                    RoomImageUrl = room.MainImageUrl,
                    OwnerId = room.OwnerId,
                    OwnerName = landlordName,
                    RenterId = renterId ?? "",
                    RenterName = renterName,
                    LastMessage = $"Hồ sơ đăng ký thuê phòng: \"{message}\"",
                    LastMessageTime = DateTime.UtcNow,
                    LastSenderId = renterId ?? "",
                    Participants = new List<string> { renterId ?? "", room.OwnerId },
                    ApplicationId = app.Id,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Chats.Add(chat);
            }

            await _context.SaveChangesAsync();

            // Save first message in chat
            var firstMsg = new MessageModel
            {
                ChatId = chat.Id,
                SenderId = renterId ?? "",
                SenderName = renterName,
                Text = $"Chào bạn, mình vừa gửi đơn ứng tuyển thuê phòng \"{room.Title}\" với tin nhắn: \"{message}\". Điện thoại liên hệ: {renterPhone}.",
                CreatedAt = DateTime.UtcNow
            };
            _context.Messages.Add(firstMsg);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Chat", new { chatId = chat.Id });
        }

        // View landlord or renter applications
        [HttpGet]
        public async Task<IActionResult> Applications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            List<ApplicationModel> apps;

            if (User.IsInRole("Landlord"))
            {
                apps = await _context.Applications
                    .Where(a => a.OwnerId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
            }
            else
            {
                apps = await _context.Applications
                    .Where(a => a.RenterId == userId)
                    .OrderByDescending(a => a.CreatedAt)
                    .ToListAsync();
            }

            return View(apps);
        }

        // Update application status (Landlord action)
        [Authorize(Roles = "Landlord")]
        [HttpPost]
        public async Task<IActionResult> UpdateApplicationStatus(string appId, string status, string note)
        {
            var app = await _context.Applications.FindAsync(appId);
            if (app == null) return NotFound();

            app.Status = status; // approved / rejected
            app.Note = note;
            app.UpdatedAt = DateTime.UtcNow;

            // Send notification message in chat
            var chat = await _context.Chats.FirstOrDefaultAsync(c => c.ApplicationId == app.Id);
            if (chat != null)
            {
                chat.LastMessage = $"Trạng thái đơn: {status.ToUpper()}. Ghi chú: {note}";
                chat.LastMessageTime = DateTime.UtcNow;
                chat.LastSenderId = app.OwnerId;

                var notifyMsg = new MessageModel
                {
                    ChatId = chat.Id,
                    SenderId = app.OwnerId,
                    SenderName = app.OwnerName,
                    Text = $"[HỆ THỐNG] Đơn đăng ký thuê phòng đã được cập nhật thành: **{status.ToUpper()}**.\nPhản hồi từ chủ trọ: \"{note}\"",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(notifyMsg);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Applications");
        }
    }
}

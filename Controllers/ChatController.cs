using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;

namespace SmartRoomFinder.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly AppDbContext _context;

        public ChatController(AppDbContext context)
        {
            _context = context;
        }

        // List all chats for current user
        public async Task<IActionResult> Index(string? chatId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Auth");

            // Fetch all chats where user is participant
            var chats = await _context.Chats
                .Where(c => c.RenterId == userId || c.OwnerId == userId)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            ViewData["Chats"] = chats;
            ViewData["CurrentUserId"] = userId;

            ChatModel? selectedChat = null;
            if (!string.IsNullOrEmpty(chatId))
            {
                selectedChat = chats.FirstOrDefault(c => c.Id == chatId);
                if (selectedChat != null)
                {
                    // Fetch messages for selected chat
                    var messages = await _context.Messages
                        .Where(m => m.ChatId == chatId)
                        .OrderBy(m => m.CreatedAt)
                        .ToListAsync();

                    ViewData["Messages"] = messages;
                    ViewData["SelectedChat"] = selectedChat;
                }
            }

            return View();
        }

        // Send a message
        [HttpPost]
        public async Task<IActionResult> SendMessage(string chatId, string text)
        {
            if (string.IsNullOrEmpty(text)) return BadRequest();

            var chat = await _context.Chats.FindAsync(chatId);
            if (chat == null) return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "User";

            if (userId != chat.RenterId && userId != chat.OwnerId)
            {
                return Forbid();
            }

            var msg = new MessageModel
            {
                ChatId = chatId,
                SenderId = userId,
                SenderName = userName,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };

            _context.Messages.Add(msg);

            // Update last message in chat
            chat.LastMessage = text;
            chat.LastMessageTime = DateTime.UtcNow;
            chat.LastSenderId = userId;
            chat.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction("Index", new { chatId = chatId });
        }

        [HttpPost]
        public async Task<IActionResult> StartChat(string roomId)
        {
            var room = await _context.Rooms.FindAsync(roomId);
            if (room == null) return NotFound();

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId == null) return Challenge();

            var landlord = await _context.Users.FindAsync(room.OwnerId);
            var landlordName = landlord?.Name ?? "Chủ trọ";
            var currentUserName = User.Identity?.Name ?? "Người thuê";

            var chat = await _context.Chats.FirstOrDefaultAsync(c =>
                c.RoomId == room.Id && c.RenterId == currentUserId && c.OwnerId == room.OwnerId);

            if (chat == null)
            {
                chat = new ChatModel
                {
                    RoomId = room.Id,
                    RoomTitle = room.Title,
                    RoomImageUrl = room.MainImageUrl,
                    OwnerId = room.OwnerId,
                    OwnerName = landlordName,
                    RenterId = currentUserId,
                    RenterName = currentUserName,
                    LastMessage = "Bắt đầu cuộc trò chuyện mới.",
                    LastMessageTime = DateTime.UtcNow,
                    LastSenderId = currentUserId,
                    Participants = new List<string> { currentUserId, room.OwnerId },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Chats.Add(chat);
                await _context.SaveChangesAsync();

                var firstMsg = new MessageModel
                {
                    ChatId = chat.Id,
                    SenderId = currentUserId,
                    SenderName = currentUserName,
                    Text = $"Xin chào, tôi muốn hỏi thăm về phòng trọ \"{room.Title}\" của bạn.",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(firstMsg);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", new { chatId = chat.Id });
        }
    }
}

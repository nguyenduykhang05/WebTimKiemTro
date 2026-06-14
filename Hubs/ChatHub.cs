using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models.DTOs;
using SmartRoomFinder.Services;

namespace SmartRoomFinder.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly AppDbContext _context;
        private readonly IUserConnectionManager _connectionManager;

        public ChatHub(AppDbContext context, IUserConnectionManager connectionManager)
        {
            _context = context;
            _connectionManager = connectionManager;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.KeepUserConnection(userId, Context.ConnectionId);
                // Broadcast online status to all
                await Clients.All.SendAsync("UserOnlineStatus", userId, true);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                _connectionManager.RemoveUserConnection(Context.ConnectionId);
                if (!_connectionManager.IsUserOnline(userId))
                {
                    // Broadcast offline status to all
                    await Clients.All.SendAsync("UserOnlineStatus", userId, false);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinChat(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        }

        public async Task LeaveChat(string chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
        }

        public async Task Typing(string chatId, bool isTyping)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            // Send to others in group
            await Clients.GroupExcept(chatId, Context.ConnectionId).SendAsync("ReceiveTyping", userId, isTyping);
        }

        public async Task SendMessage(string chatId, string text)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = Context.User?.Identity?.Name ?? "User";

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(text)) return;

            var chat = await _context.Chats.FindAsync(chatId);
            if (chat == null) return;

            // Only allow participants
            if (chat.OwnerId != userId && chat.RenterId != userId) return;

            var msg = new MessageModel
            {
                ChatId = chatId,
                SenderId = userId,
                SenderName = userName,
                Text = text,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Messages.Add(msg);

            chat.LastMessage = text;
            chat.LastMessageTime = DateTime.UtcNow;
            chat.LastSenderId = userId;
            chat.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var dto = new ChatMessageDto
            {
                Id = msg.Id,
                ChatId = msg.ChatId,
                SenderId = msg.SenderId,
                SenderName = msg.SenderName,
                Text = msg.Text,
                Type = msg.Type,
                CreatedAtStr = msg.CreatedAt.ToString("HH:mm"),
                IsRead = msg.IsRead
            };

            // Broadcast to group
            await Clients.Group(chatId).SendAsync("ReceiveMessage", dto);

            // Send notification event to receiver if they are not in the chat group
            var receiverId = chat.OwnerId == userId ? chat.RenterId : chat.OwnerId;
            var receiverConnections = _connectionManager.GetUserConnections(receiverId);
            if (receiverConnections != null && receiverConnections.Count > 0)
            {
                await Clients.Clients(receiverConnections).SendAsync("ReceiveNotification", "Có tin nhắn mới từ " + userName);
            }
        }

        public async Task MarkAsRead(string chatId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return;

            var unreadMessages = await _context.Messages
                .Where(m => m.ChatId == chatId && m.SenderId != userId && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
                
                // Notify sender that messages were read
                await Clients.GroupExcept(chatId, Context.ConnectionId).SendAsync("MessagesRead", chatId);
            }
        }
    }
}

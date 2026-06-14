using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public class AIChatLogModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SessionId { get; set; } = string.Empty; // Session or UserId
        public string UserMessage { get; set; } = string.Empty;
        public string BotReply { get; set; } = string.Empty;
        public string ExtractedJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

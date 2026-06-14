using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public class MessageModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string ChatId { get; set; } = string.Empty;

        [Required]
        public string SenderId { get; set; } = string.Empty;

        [Required]
        public string SenderName { get; set; } = string.Empty;

        [Required]
        public string Text { get; set; } = string.Empty;

        public string Type { get; set; } = "text"; // "text", "image", etc.

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

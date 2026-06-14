using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public class ReportModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string RoomId { get; set; } = string.Empty;

        [Required]
        public string ReporterId { get; set; } = string.Empty;

        [Required]
        public string Reason { get; set; } = "Spam"; // "Spam", "Lừa đảo", "Tin giả"

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "pending"; // "pending", "resolved"
    }
}

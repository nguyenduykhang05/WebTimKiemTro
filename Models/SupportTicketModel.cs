using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRoomFinder.Models
{
    public class SupportTicketModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string UserEmail { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // "Pending", "Resolved"
        public string Status { get; set; } = "Pending";

        public string? AdminReply { get; set; }

        public DateTime? RepliedAt { get; set; }
    }
}

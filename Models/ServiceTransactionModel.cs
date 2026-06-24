using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRoomFinder.Models
{
    public class ServiceTransactionModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string? RoomId { get; set; }

        public RoomPackage Package { get; set; }

        public int Days { get; set; }

        public double Amount { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, Paid, Cancelled

        public long OrderCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public virtual UserModel? User { get; set; }

        [ForeignKey("RoomId")]
        public virtual RoomModel? Room { get; set; }
    }
}

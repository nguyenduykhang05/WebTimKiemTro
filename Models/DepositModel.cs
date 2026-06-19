using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRoomFinder.Models
{
    public enum DepositStatus
    {
        Pending,
        Paid,
        Completed,
        Refunded,
        Forfeited
    }

    public class DepositModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string RoomId { get; set; } = string.Empty;

        [Required]
        public string RenterId { get; set; } = string.Empty;

        [Required]
        public double Amount { get; set; }

        public DepositStatus Status { get; set; } = DepositStatus.Pending;

        public string TransactionId { get; set; } = string.Empty;

        public long OrderCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public DateTime? PaidAt { get; set; }

        // Navigation properties
        [ForeignKey("RoomId")]
        public virtual RoomModel Room { get; set; } = null!;

        [ForeignKey("RenterId")]
        public virtual UserModel Renter { get; set; } = null!;
    }
}

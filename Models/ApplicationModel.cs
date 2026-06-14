using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public class ApplicationModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string RoomId { get; set; } = string.Empty;

        [Required]
        public string RoomTitle { get; set; } = string.Empty;

        public string RoomImageUrl { get; set; } = string.Empty;

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        public string OwnerName { get; set; } = string.Empty;

        [Required]
        public string RenterId { get; set; } = string.Empty;

        [Required]
        public string RenterName { get; set; } = string.Empty;

        [Required]
        public string RenterPhone { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string Status { get; set; } = "pending"; // pending, approved, rejected

        public DateTime? ExpectedMoveInDate { get; set; }

        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

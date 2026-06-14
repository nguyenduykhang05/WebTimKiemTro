using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public class ReviewModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string RoomId { get; set; } = string.Empty;

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        public string UserAvatar { get; set; } = "/images/default_avatar.png";

        [Required]
        [Range(1, 5)]
        public double Rating { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        public string RenterUniversity { get; set; } = "ĐH HUTECH";

        public string RentalDuration { get; set; } = "6 tháng";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsHidden { get; set; } = false;
    }
}

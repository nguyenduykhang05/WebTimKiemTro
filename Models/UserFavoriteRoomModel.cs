using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public class UserFavoriteRoomModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string RoomId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual UserModel? User { get; set; }
        public virtual RoomModel? Room { get; set; }
    }
}

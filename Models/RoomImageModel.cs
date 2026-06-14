using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRoomFinder.Models
{
    public class RoomImageModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string RoomId { get; set; } = string.Empty;

        [Required]
        public string Url { get; set; } = string.Empty;

        public string Caption { get; set; } = string.Empty;

        public int SortOrder { get; set; } = 0;

        public bool IsMain { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("RoomId")]
        public virtual RoomModel Room { get; set; } = null!;
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRoomFinder.Models
{
    public enum AppointmentStatus
    {
        Pending,
        Accepted,
        Declined,
        Completed,
        Cancelled
    }

    public class AppointmentModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string TenantId { get; set; } = string.Empty;

        public string TenantName { get; set; } = string.Empty;

        public string TenantPhone { get; set; } = string.Empty;

        [Required]
        public string RoomId { get; set; } = string.Empty;

        public string RoomTitle { get; set; } = string.Empty;

        [Required]
        public string LandlordId { get; set; } = string.Empty;

        public string LandlordName { get; set; } = string.Empty;

        /// <summary>Thời gian hẹn xem phòng.</summary>
        [Required]
        public DateTime MeetTime { get; set; }

        /// <summary>Ghi chú thêm của người thuê.</summary>
        public string Note { get; set; } = string.Empty;

        public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

        /// <summary>Phản hồi từ chủ trọ khi duyệt/từ chối.</summary>
        public string LandlordResponse { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("TenantId")]
        public virtual UserModel Tenant { get; set; } = null!;

        [ForeignKey("RoomId")]
        public virtual RoomModel Room { get; set; } = null!;
    }
}

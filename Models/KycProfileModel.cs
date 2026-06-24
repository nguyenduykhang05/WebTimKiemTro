using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRoomFinder.Models
{
    public enum KycStatus
    {
        NotSubmitted,
        Pending,
        Approved,
        Rejected
    }

    public class KycProfileModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Khóa ngoại 1-1 với bảng Users. Mỗi Landlord chỉ có 1 hồ sơ KYC.</summary>
        [Required]
        public string UserId { get; set; } = string.Empty;

        /// <summary>Số CCCD 12 chữ số.</summary>
        [Required]
        [StringLength(12, MinimumLength = 9)]
        public string IdentityCardNumber { get; set; } = string.Empty;

        /// <summary>Họ và tên trên CCCD (OCR bóc tách được).</summary>
        [StringLength(100)]
        public string FullNameOnCard { get; set; } = string.Empty;

        /// <summary>Ngày sinh trên CCCD (OCR bóc tách được).</summary>
        public string DateOfBirthOnCard { get; set; } = string.Empty;

        /// <summary>URL ảnh mặt trước CCCD (upload lên server).</summary>
        [Required]
        public string FrontImageUrl { get; set; } = string.Empty;

        /// <summary>URL ảnh mặt sau CCCD.</summary>
        [Required]
        public string BackImageUrl { get; set; } = string.Empty;

        /// <summary>URL ảnh chân dung Selfie cầm CCCD sát mặt.</summary>
        [Required]
        public string SelfieImageUrl { get; set; } = string.Empty;

        /// <summary>Trạng thái duyệt KYC.</summary>
        public KycStatus Status { get; set; } = KycStatus.NotSubmitted;

        /// <summary>Lý do Admin từ chối (Nullable).</summary>
        public string? RejectReason { get; set; }

        /// <summary>Điểm trùng khớp khuôn mặt do AI trả về (0.0 - 1.0).</summary>
        public double? AiFaceMatchScore { get; set; }

        /// <summary>Kết quả AI: khuôn mặt có trùng khớp không.</summary>
        public bool? AiIsMatch { get; set; }

        /// <summary>Toàn bộ JSON kết quả OCR từ AI service.</summary>
        public string AiOcrResultJson { get; set; } = "{}";

        /// <summary>Thời điểm nộp hồ sơ KYC.</summary>
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Thời điểm Admin duyệt/từ chối.</summary>
        public DateTime? ReviewedAt { get; set; }

        /// <summary>ID Admin đã duyệt.</summary>
        public string? ReviewedByAdminId { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public virtual UserModel User { get; set; } = null!;
    }
}

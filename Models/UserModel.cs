using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models
{
    public enum UserRole
    {
        Renter,
        Landlord,
        Admin
    }

    public class UserModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string ProfileImageUrl { get; set; } = "/images/default_avatar.png";

        public string Location { get; set; } = "TP. Ho Chi Minh";

        public string PhoneNumber { get; set; } = string.Empty;

        public UserRole Role { get; set; } = UserRole.Renter;

        public bool HasSelectedRole { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsLocked { get; set; } = false;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // -------------------------------------------------------
        // KYC (eKYC) - Chỉ dùng cho Landlord
        // -------------------------------------------------------

        /// <summary>
        /// Cờ đánh dấu tài khoản Chủ trọ đã được Admin xác thực CCCD.
        /// Nếu false, Landlord không được phép tạo/đăng bài phòng trọ.
        /// </summary>
        public bool IsKycVerified { get; set; } = false;

        /// <summary>Trạng thái hồ sơ KYC nhanh (cache lại từ KycProfileModel).</summary>
        public KycStatus KycStatus { get; set; } = KycStatus.NotSubmitted;

        // -------------------------------------------------------
        // Cấu hình cổng thanh toán PayOS riêng của từng Chủ trọ
        // Tiền cọc sẽ chạy thẳng vào tài khoản ngân hàng của Chủ trọ.
        // -------------------------------------------------------

        public string PayOsClientId { get; set; } = string.Empty;

        public string PayOsApiKey { get; set; } = string.Empty;

        public string PayOsChecksumKey { get; set; } = string.Empty;

        /// <summary>Chủ trọ đã hoàn tất cấu hình cổng PayOS chưa.</summary>
        public bool HasPayOsConfigured =>
            !string.IsNullOrWhiteSpace(PayOsClientId) &&
            !string.IsNullOrWhiteSpace(PayOsApiKey) &&
            !string.IsNullOrWhiteSpace(PayOsChecksumKey);

        // -------------------------------------------------------
        // Cấu hình chuyển khoản ngân hàng qua QR code (VietQR)
        // -------------------------------------------------------
        public string? BankName { get; set; } = string.Empty;
        public string? BankAccountNumber { get; set; } = string.Empty;
        public string? BankAccountHolder { get; set; } = string.Empty;

        public bool HasVietQrConfigured =>
            !string.IsNullOrWhiteSpace(BankName) &&
            !string.IsNullOrWhiteSpace(BankAccountNumber) &&
            !string.IsNullOrWhiteSpace(BankAccountHolder);

        // -------------------------------------------------------
        // Gói Dịch vụ VIP của Tài khoản (Áp dụng cho tất cả phòng)
        // -------------------------------------------------------
        public RoomPackage CurrentPackage { get; set; } = RoomPackage.Default;
        public DateTime? PackageExpiresAt { get; set; }
    }
}

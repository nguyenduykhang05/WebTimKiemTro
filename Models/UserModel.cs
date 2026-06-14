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
    }
}

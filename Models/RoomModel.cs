using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmartRoomFinder.Models
{
    public enum RoomType
    {
        Apartment,
        Studio,
        House,
        Villa
    }

    public enum RoomDirection
    {
        East,
        West,
        South,
        North,
        SouthEast,
        SouthWest,
        NorthEast,
        NorthWest
    }

    public enum RoomStatus
    {
        Pending,
        Verified,
        Rejected,
        NeedsInfo
    }

    public class RoomDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string FileSize { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class RoomReviewHistory
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public DateTime Time { get; set; } = DateTime.UtcNow;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string? ActorId { get; set; }
        public string ActorName { get; set; } = "Hệ thống";
    }

    public class RoomModel
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string OwnerId { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public double Price { get; set; }

        public double DepositAmount { get; set; } = 500000;

        public string Address { get; set; } = string.Empty;

        public string Location { get; set; } = "TP. Ho Chi Minh";

        public bool IsDraft { get; set; } = false;

        public bool IsFavorite { get; set; } = false;

        public bool IsVerified { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public string MainImageUrl { get; set; } = "/images/default_room.png";

        public virtual ICollection<RoomImageModel> Images { get; set; } = new List<RoomImageModel>();

        public double Rating { get; set; } = 0.0;

        public int TotalReviews { get; set; } = 0;

        public double Latitude { get; set; } = 10.762622; // Default HCMC coords

        public double Longitude { get; set; } = 106.660172;

        public RoomType Type { get; set; } = RoomType.Studio;

        public double Area { get; set; } = 0.0;

        public int Bedrooms { get; set; } = 0;

        // Store List<string> as JSON in SQLite
        public string AmenitiesJson { get; set; } = "[]";

        [NotMapped]
        public List<string> Amenities
        {
            get => JsonSerializer.Deserialize<List<string>>(AmenitiesJson) ?? new();
            set => AmenitiesJson = JsonSerializer.Serialize(value);
        }

        public int ViewCount { get; set; } = 0;

        public int ContactCount { get; set; } = 0;

        public string PostedBy { get; set; } = string.Empty;

        public RoomDirection? Direction { get; set; }

        public DateTime PostedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }

        public RoomStatus ApprovalStatus { get; set; } = RoomStatus.Pending;

        public bool IsReserved { get; set; } = false;

        // Store List<RoomDocument> as JSON in SQLite
        public string DocumentsJson { get; set; } = "[]";

        [NotMapped]
        public List<RoomDocument> Documents
        {
            get => JsonSerializer.Deserialize<List<RoomDocument>>(DocumentsJson) ?? new();
            set => DocumentsJson = JsonSerializer.Serialize(value);
        }

        // Store List<RoomReviewHistory> as JSON in SQLite
        public string ReviewHistoryJson { get; set; } = "[]";

        [NotMapped]
        public List<RoomReviewHistory> ReviewHistory
        {
            get => JsonSerializer.Deserialize<List<RoomReviewHistory>>(ReviewHistoryJson) ?? new();
            set => ReviewHistoryJson = JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

        [NotMapped]
        public int DaysLeft => ExpiresAt.HasValue ? Math.Max(0, (ExpiresAt.Value - DateTime.UtcNow).Days) : 0;

        [NotMapped]
        public string DirectionString => Direction switch
        {
            RoomDirection.East => "Đông",
            RoomDirection.West => "Tây",
            RoomDirection.South => "Nam",
            RoomDirection.North => "Bắc",
            RoomDirection.SouthEast => "Đông Nam",
            RoomDirection.SouthWest => "Tây Nam",
            RoomDirection.NorthEast => "Đông Bắc",
            RoomDirection.NorthWest => "Tây Bắc",
            _ => "Không xác định"
        };
    }
}

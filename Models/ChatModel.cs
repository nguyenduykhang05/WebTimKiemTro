using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace SmartRoomFinder.Models
{
    public class ChatModel
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

        public string RenterName { get; set; } = string.Empty;

        public string LastMessage { get; set; } = string.Empty;

        public DateTime? LastMessageTime { get; set; }

        public string LastSenderId { get; set; } = string.Empty;

        public string ParticipantsJson { get; set; } = "[]";

        [NotMapped]
        public List<string> Participants
        {
            get => JsonSerializer.Deserialize<List<string>>(ParticipantsJson) ?? new();
            set => ParticipantsJson = JsonSerializer.Serialize(value);
        }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public string? ApplicationId { get; set; }
    }
}

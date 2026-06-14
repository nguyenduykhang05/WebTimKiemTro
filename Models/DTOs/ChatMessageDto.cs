namespace SmartRoomFinder.Models.DTOs
{
    public class ChatMessageDto
    {
        public string Id { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Type { get; set; } = "text";
        public string CreatedAtStr { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}

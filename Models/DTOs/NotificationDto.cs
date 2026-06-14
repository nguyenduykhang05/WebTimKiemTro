namespace SmartRoomFinder.Models.DTOs
{
    public class NotificationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string LinkUrl { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public string CreatedAtStr { get; set; } = string.Empty;
    }
}

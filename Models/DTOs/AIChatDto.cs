using System.Collections.Generic;

namespace SmartRoomFinder.Models.DTOs
{
    public class AIChatRequestDto
    {
        public string Message { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
    }

    public class AIChatResponseDto
    {
        public string ReplyText { get; set; } = string.Empty;
        public List<RecommendedRoomDto> SuggestedRooms { get; set; } = new List<RecommendedRoomDto>();
        public ParsedIntentDto ExtractedFilters { get; set; } = new ParsedIntentDto();
        public bool IsError { get; set; } = false;
    }

    public class ParsedIntentDto
    {
        public string? Location { get; set; }
        public string? Landmark { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string? RoomType { get; set; }
        public List<string> Amenities { get; set; } = new List<string>();
    }
}

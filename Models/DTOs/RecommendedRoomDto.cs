namespace SmartRoomFinder.Models.DTOs
{
    public class RecommendedRoomDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Price { get; set; }
        public double Area { get; set; }
        public string Address { get; set; } = string.Empty;
        public string MainImageUrl { get; set; } = string.Empty;
        public double SimilarityScore { get; set; }
        public string MatchReason { get; set; } = string.Empty;
        public System.DateTime PostedAt { get; set; }
        public bool IsVerified { get; set; }
    }
}

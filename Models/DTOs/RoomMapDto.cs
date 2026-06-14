namespace SmartRoomFinder.Models.DTOs
{
    public class RoomMapDto
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public double Price { get; set; }
        public double Area { get; set; }
        public string Address { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public double DistanceKm { get; set; }
    }
}

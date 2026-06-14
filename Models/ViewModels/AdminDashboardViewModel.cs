using System.Collections.Generic;

namespace SmartRoomFinder.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalRooms { get; set; }
        public int TotalReports { get; set; }
        public int PendingRoomsCount { get; set; }
        public int VerifiedRoomsCount { get; set; }
        public int RejectedRoomsCount { get; set; }
        public int NeedInfoRoomsCount { get; set; }
        public int SupportRequestsCount { get; set; }
        
        public IEnumerable<RoomModel> PendingRooms { get; set; } = new List<RoomModel>();
        public IEnumerable<RoomModel> RecentVerifiedRooms { get; set; } = new List<RoomModel>();
        public IEnumerable<ReviewModel> RecentReviews { get; set; } = new List<ReviewModel>();
        public IEnumerable<ReportModel> RecentReports { get; set; } = new List<ReportModel>();
    }
}

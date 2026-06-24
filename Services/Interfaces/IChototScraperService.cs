using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Interfaces
{
    public interface IChototScraperService
    {
        Task<int> ScrapeAndImportRoomsAsync(string adminId, string adminName, int limit);
    }
}

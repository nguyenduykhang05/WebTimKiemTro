using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Interfaces
{
    public interface IFileService
    {
        Task<string> SaveImageAsync(IFormFile file, string folderName = "rooms");
        Task<string> UploadImageAsync(IFormFile file, string folderName = "rooms");
        Task<List<string>> SaveImagesAsync(IEnumerable<IFormFile> files, string folderName = "rooms");
        void DeleteImage(string imageUrl);
    }
}

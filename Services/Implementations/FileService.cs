using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SmartRoomFinder.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Implementations
{
    public class FileService : IFileService
    {
        private readonly IWebHostEnvironment _env;

        public FileService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string> SaveImageAsync(IFormFile file, string folderName = "rooms")
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            var uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", folderName);
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/uploads/{folderName}/{uniqueFileName}";
        }

        // Alias for SaveImageAsync — used by KYC upload
        public Task<string> UploadImageAsync(IFormFile file, string folderName = "rooms")
            => SaveImageAsync(file, folderName);

        public async Task<List<string>> SaveImagesAsync(IEnumerable<IFormFile> files, string folderName = "rooms")
        {
            var imageUrls = new List<string>();
            if (files != null)
            {
                foreach (var file in files)
                {
                    var url = await SaveImageAsync(file, folderName);
                    if (!string.IsNullOrEmpty(url))
                    {
                        imageUrls.Add(url);
                    }
                }
            }
            return imageUrls;
        }

        public void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl) || imageUrl.StartsWith("http"))
                return;

            // Remove starting slash if present
            var relativePath = imageUrl.TrimStart('/');
            var absolutePath = Path.Combine(_env.WebRootPath, relativePath.Replace("/", "\\"));

            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
            }
        }
    }
}

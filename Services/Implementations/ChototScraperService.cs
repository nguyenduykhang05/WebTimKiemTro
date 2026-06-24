using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models;
using SmartRoomFinder.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Implementations
{
    public class ChototScraperService : IChototScraperService
    {
        private readonly HttpClient _httpClient;
        private readonly AppDbContext _context;

        public ChototScraperService(HttpClient httpClient, AppDbContext context)
        {
            _httpClient = httpClient;
            _context = context;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        }

        public async Task<int> ScrapeAndImportRoomsAsync(string adminId, string adminName, int limit)
        {
            int importedCount = 0;
            try
            {
                // Gọi API lấy danh sách phòng trọ cho thuê (Category: 1020 - Nhà ở/Phòng trọ)
                string url = $"https://gateway.chotot.com/v1/public/ad-listing?cg=1020&limit={limit}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return 0;

                string jsonStr = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(jsonStr);
                
                var root = document.RootElement;
                if (root.TryGetProperty("ads", out var ads))
                {
                    foreach (var ad in ads.EnumerateArray())
                    {
                        try
                        {
                            // Kiểm tra trùng lặp theo title hoặc dựa vào Chotot list_id (để vào description)
                            string subject = ad.GetProperty("subject").GetString() ?? "Phòng trọ cho thuê";
                            long listId = ad.GetProperty("list_id").GetInt64();
                            string listIdStr = $"[CHOTOT-{listId}]";

                            if (await _context.Rooms.AnyAsync(r => r.Description.Contains(listIdStr)))
                                continue;

                            double price = 0;
                            if (ad.TryGetProperty("price", out var priceElement))
                                price = priceElement.GetDouble();

                            string address = "";
                            if (ad.TryGetProperty("ward_name", out var ward)) address += ward.GetString() + ", ";
                            if (ad.TryGetProperty("region_name", out var region)) address += region.GetString();

                            double lat = 10.762622;
                            double lng = 106.660172;
                            if (ad.TryGetProperty("latitude", out var latElement) && latElement.ValueKind == JsonValueKind.Number)
                                lat = latElement.GetDouble();
                            if (ad.TryGetProperty("longitude", out var lngElement) && lngElement.ValueKind == JsonValueKind.Number)
                                lng = lngElement.GetDouble();

                            var newRoom = new RoomModel
                            {
                                OwnerId = adminId,
                                Title = subject,
                                Description = $"{subject}\nNguồn: Chợ Tốt {listIdStr}",
                                Price = price,
                                DepositAmount = price > 0 ? price : 500000,
                                Address = string.IsNullOrEmpty(address) ? "TP.HCM" : address.TrimEnd(',', ' '),
                                Location = ad.TryGetProperty("region_name", out var rName) ? rName.GetString() ?? "TP.HCM" : "TP.HCM",
                                PostedBy = string.IsNullOrEmpty(adminName) ? "Admin" : adminName,
                                IsActive = true,
                                IsVerified = true, // Tự động duyệt vì là admin lấy về
                                Latitude = lat,
                                Longitude = lng,
                                Type = RoomType.Studio
                            };

                            // Lấy hình ảnh
                            var images = new List<RoomImageModel>();
                            if (ad.TryGetProperty("images", out var imagesElement))
                            {
                                int imgIndex = 0;
                                if (imagesElement.ValueKind == JsonValueKind.String)
                                {
                                    string imagesStr = imagesElement.GetString() ?? "";
                                    var imgList = imagesStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    foreach (var imgUrl in imgList)
                                    {
                                        if (imgIndex == 0) newRoom.MainImageUrl = imgUrl;
                                        images.Add(new RoomImageModel
                                        {
                                            RoomId = newRoom.Id,
                                            Url = imgUrl,
                                            IsMain = imgIndex == 0
                                        });
                                        imgIndex++;
                                    }
                                }
                                else if (imagesElement.ValueKind == JsonValueKind.Array)
                                {
                                    foreach (var img in imagesElement.EnumerateArray())
                                    {
                                        string imgUrl = img.GetString() ?? "";
                                        if (!string.IsNullOrEmpty(imgUrl))
                                        {
                                            if (imgIndex == 0) newRoom.MainImageUrl = imgUrl;
                                            images.Add(new RoomImageModel
                                            {
                                                RoomId = newRoom.Id,
                                                Url = imgUrl,
                                                IsMain = imgIndex == 0
                                            });
                                            imgIndex++;
                                        }
                                    }
                                }
                            }
                            
                            // Nếu không lấy được mảng images, thử lấy image đơn
                            if (!images.Any() && ad.TryGetProperty("image", out var singleImgElement) && singleImgElement.ValueKind == JsonValueKind.String)
                            {
                                string singleImg = singleImgElement.GetString() ?? "";
                                if (!string.IsNullOrEmpty(singleImg))
                                {
                                    newRoom.MainImageUrl = singleImg;
                                    images.Add(new RoomImageModel
                                    {
                                        RoomId = newRoom.Id,
                                        Url = singleImg,
                                        IsMain = true
                                    });
                                }
                            }

                            if (images.Any())
                            {
                                newRoom.Images = images;
                            }

                            _context.Rooms.Add(newRoom);
                            importedCount++;
                        }
                        catch (Exception)
                        {
                            // Bỏ qua lỗi từng item
                        }
                    }

                    if (importedCount > 0)
                    {
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi cào dữ liệu Chợ Tốt: " + ex.Message);
            }

            return importedCount;
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Net.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;

#r "E:\DoanLapTrinhWeb\bin\Debug\net10.0\SmartRoomFinder.dll"
#r "nuget: Microsoft.EntityFrameworkCore.SqlServer, 9.0.0"
#r "nuget: BCrypt.Net-Next, 4.0.3"

using SmartRoomFinder.Data;
using SmartRoomFinder.Models;
using BCrypt.Net;

var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlServer("Server=.\\SQLEXPRESS;Database=SmartRoomFinder;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");

using var context = new AppDbContext(optionsBuilder.Options);

string[] landlordNames = { "Nguyễn Văn An", "Trần Thị Bình", "Lê Hoàng Cường" };
string[] landlordEmails = { "chutro1@gmail.com", "chutro2@gmail.com", "chutro3@gmail.com" };

var landlords = new System.Collections.Generic.List<UserModel>();

for(int i=0; i<3; i++) {
    var email = landlordEmails[i];
    var user = context.Users.FirstOrDefault(u => u.Email == email);
    if(user == null) {
        user = new UserModel {
            Id = Guid.NewGuid().ToString(),
            Name = landlordNames[i],
            Email = email,
            PhoneNumber = "0900" + i.ToString() + "0000",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = UserRole.Landlord,
            HasSelectedRole = true,
            IsKycVerified = true,
            KycStatus = SmartRoomFinder.Models.DTOs.KycStatus.Verified,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        Console.WriteLine($"Created landlord: {user.Name}");
    } else {
        Console.WriteLine($"Landlord already exists: {user.Name}");
    }
    landlords.Add(user);
}
await context.SaveChangesAsync();

Console.WriteLine("Fetching rooms from Chotot API...");
var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

string url = "https://gateway.chotot.com/v1/public/ad-listing?cg=1020&limit=30";
var response = await httpClient.GetAsync(url);
if (!response.IsSuccessStatusCode) {
    Console.WriteLine("Failed to fetch from Chotot");
    return;
}

string jsonStr = await response.Content.ReadAsStringAsync();
using var document = JsonDocument.Parse(jsonStr);
var root = document.RootElement;

int importedCount = 0;
if (root.TryGetProperty("ads", out var ads))
{
    int landlordIndex = 0;
    foreach (var ad in ads.EnumerateArray())
    {
        try
        {
            string subject = ad.GetProperty("subject").GetString() ?? "Phòng trọ cho thuê";
            if (subject.Length > 200) subject = subject.Substring(0, 197) + "...";
            long listId = ad.GetProperty("list_id").GetInt64();
            string listIdStr = $"[CHOTOT-{listId}]";

            if (context.Rooms.Any(r => r.Description.Contains(listIdStr)))
            {
                continue;
            }

            double price = 0;
            if (ad.TryGetProperty("price", out var priceElement))
                price = priceElement.GetDouble();

            var images = new System.Collections.Generic.List<RoomImageModel>();
            if (ad.TryGetProperty("images", out var imagesElement))
            {
                int imgIndex = 0;
                if (imagesElement.ValueKind == JsonValueKind.String)
                {
                    string imagesStr = imagesElement.GetString() ?? "";
                    var urlArray = imagesStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var imgUrl in urlArray)
                    {
                        if (imgUrl.StartsWith("http"))
                        {
                            images.Add(new RoomImageModel
                            {
                                RoomId = "", // Set later
                                Url = imgUrl,
                                IsMain = imgIndex == 0,
                                SortOrder = imgIndex
                            });
                            imgIndex++;
                        }
                    }
                }
                else if (imagesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var img in imagesElement.EnumerateArray())
                    {
                        string? imgUrl = img.GetString();
                        if (!string.IsNullOrEmpty(imgUrl) && imgUrl.StartsWith("http"))
                        {
                            images.Add(new RoomImageModel
                            {
                                RoomId = "", // Set later
                                Url = imgUrl,
                                IsMain = imgIndex == 0,
                                SortOrder = imgIndex
                            });
                            imgIndex++;
                        }
                    }
                }
            }

            string address = ad.TryGetProperty("address", out var addr) ? addr.GetString() ?? "" : "";
            
            var owner = landlords[landlordIndex % landlords.Count];
            landlordIndex++;

            var newRoom = new RoomModel
            {
                OwnerId = owner.Id,
                Title = subject,
                Description = $"{subject}\nNguồn: Chợ Tốt {listIdStr}",
                Price = price,
                DepositAmount = price > 0 ? price : 500000,
                Address = string.IsNullOrEmpty(address) ? "TP.HCM" : address.TrimEnd(',', ' '),
                Location = ad.TryGetProperty("region_name", out var rName) ? rName.GetString() ?? "TP.HCM" : "TP.HCM",
                PostedBy = owner.Name,
                IsActive = true,
                IsVerified = true,
                Type = RoomType.Studio
            };

            foreach (var img in images)
            {
                img.RoomId = newRoom.Id;
            }
            if (images.Any())
            {
                newRoom.Images = images;
                newRoom.MainImageUrl = images.First(i => i.IsMain).Url;
            }

            context.Rooms.Add(newRoom);
            importedCount++;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error importing room: {ex.Message}");
        }
    }

    if (importedCount > 0)
    {
        await context.SaveChangesAsync();
        Console.WriteLine($"Successfully imported {importedCount} rooms and assigned to {landlords.Count} new landlords.");
    }
    else
    {
        Console.WriteLine("No new rooms were imported (perhaps they already exist).");
    }
}

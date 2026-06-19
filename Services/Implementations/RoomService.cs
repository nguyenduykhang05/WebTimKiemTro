using Microsoft.EntityFrameworkCore;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models.ViewModels;
using SmartRoomFinder.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartRoomFinder.Services.Implementations
{
    public class RoomService : IRoomService
    {
        private readonly AppDbContext _context;
        private readonly IFileService _fileService;

        public RoomService(AppDbContext context, IFileService fileService)
        {
            _context = context;
            _fileService = fileService;
        }

        public async Task<List<RoomModel>> GetLandlordRoomsAsync(string landlordId)
        {
            return await _context.Rooms
                .Where(r => r.OwnerId == landlordId)
                .OrderByDescending(r => r.PostedAt)
                .ToListAsync();
        }

        public async Task<RoomModel?> GetRoomByIdAsync(string id)
        {
            return await _context.Rooms.FindAsync(id);
        }

        public async Task<string> CreateRoomAsync(RoomCreateViewModel model, string ownerId, string ownerName)
        {
            var room = new RoomModel
            {
                Id = Guid.NewGuid().ToString(),
                OwnerId = ownerId,
                PostedBy = ownerName,
                Title = model.Title,
                Description = model.Description,
                Price = model.Price,
                DepositAmount = model.DepositAmount,
                Address = model.Address,
                Location = model.Location,
                Type = model.Type,
                Area = model.Area,
                Bedrooms = model.Bedrooms,
                Direction = model.Direction,
                Latitude = model.Latitude,
                Longitude = model.Longitude,
                PostedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                ApprovalStatus = RoomStatus.Pending,
                IsVerified = false,
                IsActive = true
            };

            // Parse amenities
            if (!string.IsNullOrEmpty(model.AmenitiesInput))
            {
                room.Amenities = model.AmenitiesInput.Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();
            }

            // Upload Main Image
            if (model.MainImage != null)
            {
                room.MainImageUrl = await _fileService.SaveImageAsync(model.MainImage, "rooms");
            }
            else
            {
                room.MainImageUrl = "/images/default_room.png";
            }

            // Upload Sub Images
            if (model.SubImages != null && model.SubImages.Any())
            {
                var subImageUrls = await _fileService.SaveImagesAsync(model.SubImages, "rooms");
                int sortOrder = 1;
                foreach (var url in subImageUrls)
                {
                    room.Images.Add(new RoomImageModel
                    {
                        Url = url,
                        Caption = "",
                        SortOrder = sortOrder++,
                        IsMain = false
                    });
                }
            }

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return room.Id;
        }

        public async Task<bool> UpdateRoomAsync(RoomEditViewModel model, string ownerId)
        {
            var room = await _context.Rooms.FindAsync(model.Id);
            if (room == null || room.OwnerId != ownerId) return false;

            room.Title = model.Title;
            room.Description = model.Description;
            room.Price = model.Price;
            room.DepositAmount = model.DepositAmount;
            room.Address = model.Address;
            room.Location = model.Location;
            room.Type = model.Type;
            room.Area = model.Area;
            room.Bedrooms = model.Bedrooms;
            room.Direction = model.Direction;
            room.Latitude = model.Latitude;
            room.Longitude = model.Longitude;
            room.UpdatedAt = DateTime.UtcNow;
            
            // Note: Normally an update might require re-approval. Setting to Pending if you want to be strict.
            // room.ApprovalStatus = RoomStatus.Pending;

            if (!string.IsNullOrEmpty(model.AmenitiesInput))
            {
                room.Amenities = model.AmenitiesInput.Split(',')
                    .Select(a => a.Trim())
                    .Where(a => !string.IsNullOrEmpty(a))
                    .ToList();
            }

            // Update Main Image if provided
            if (model.MainImage != null)
            {
                // Delete old image
                _fileService.DeleteImage(room.MainImageUrl);
                room.MainImageUrl = await _fileService.SaveImageAsync(model.MainImage, "rooms");
            }

            // Handle Sub Images
            // Load current images explicitly if not loaded
            if (room.Images == null)
            {
                await _context.Entry(room).Collection(r => r.Images).LoadAsync();
            }

            // Remove requested sub images
            if (!string.IsNullOrEmpty(model.RemoveSubImages))
            {
                var toRemoveUrls = model.RemoveSubImages.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var imagesToRemove = room.Images.Where(i => toRemoveUrls.Contains(i.Url)).ToList();
                foreach (var img in imagesToRemove)
                {
                    room.Images.Remove(img);
                    _fileService.DeleteImage(img.Url);
                    _context.RoomImages.Remove(img); // explicitly remove from DB
                }
            }

            // Upload new sub images
            if (model.SubImages != null && model.SubImages.Any())
            {
                var newSubImageUrls = await _fileService.SaveImagesAsync(model.SubImages, "rooms");
                int currentMaxSort = room.Images.Any() ? room.Images.Max(i => i.SortOrder) : 0;
                foreach (var url in newSubImageUrls)
                {
                    currentMaxSort++;
                    room.Images.Add(new RoomImageModel
                    {
                        Url = url,
                        Caption = "",
                        SortOrder = currentMaxSort,
                        IsMain = false
                    });
                }
            }

            _context.Rooms.Update(room);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteRoomAsync(string id, string ownerId)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null || room.OwnerId != ownerId) return false;

            // Delete images
            _fileService.DeleteImage(room.MainImageUrl);
            if (room.Images == null)
            {
                await _context.Entry(room).Collection(r => r.Images).LoadAsync();
            }
            foreach (var img in room.Images)
            {
                _fileService.DeleteImage(img.Url);
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<LandlordStatsViewModel> GetLandlordStatsAsync(string landlordId)
        {
            var landlordRooms = await _context.Rooms.Where(r => r.OwnerId == landlordId).ToListAsync();
            
            var stats = new LandlordStatsViewModel
            {
                TotalRooms = landlordRooms.Count,
                PendingRooms = landlordRooms.Count(r => r.ApprovalStatus == RoomStatus.Pending),
                TotalViews = landlordRooms.Sum(r => r.ViewCount),
                TotalContacts = landlordRooms.Sum(r => r.ContactCount),
                PendingApplications = await _context.Applications.CountAsync(a => a.OwnerId == landlordId && a.Status == "pending")
            };

            return stats;
        }
    }
}

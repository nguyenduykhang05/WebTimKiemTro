using Microsoft.AspNetCore.Http;
using SmartRoomFinder.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartRoomFinder.Models.ViewModels
{
    public class RoomCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tiêu đề bài đăng.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mô tả.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập giá thuê.")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá thuê phải lớn hơn hoặc bằng 0.")]
        public double Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền cọc yêu cầu.")]
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền cọc phải lớn hơn hoặc bằng 0.")]
        public double DepositAmount { get; set; } = 500000;

        public string Address { get; set; } = string.Empty;
        public string Location { get; set; } = "TP. Ho Chi Minh";

        public RoomType Type { get; set; } = RoomType.Studio;

        [Range(0, double.MaxValue, ErrorMessage = "Diện tích phải lớn hơn hoặc bằng 0.")]
        public double Area { get; set; }
        public int Bedrooms { get; set; }
        public RoomDirection? Direction { get; set; }

        public double Latitude { get; set; } = 10.762622;
        public double Longitude { get; set; } = 106.660172;

        // Images
        public IFormFile? MainImage { get; set; }
        public List<IFormFile>? SubImages { get; set; }

        // String arrays from form
        public string AmenitiesInput { get; set; } = string.Empty;
    }

    public class RoomEditViewModel : RoomCreateViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        // Keep track of existing images so we don't overwrite if not uploaded
        public string CurrentMainImageUrl { get; set; } = string.Empty;
        public List<RoomImageModel> CurrentSubImages { get; set; } = new();
        
        // Comma separated list of sub images to remove
        public string RemoveSubImages { get; set; } = string.Empty;
    }
}

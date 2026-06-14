using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartRoomFinder.Models
{
    public static class DbSeeder
    {
        public static void Seed(AppDbContext context)
        {
            // Seed Users
            if (!context.Users.Any())
            {
                var users = new List<UserModel>
                {
                    new UserModel
                    {
                        Id = "user_1",
                        Name = "Nguyễn Văn A",
                        Email = "vana@example.com",
                        ProfileImageUrl = "https://i.pravatar.cc/150?u=vana@example.com",
                        Location = "TP. Hồ Chí Minh",
                        PhoneNumber = "0901234567",
                        Role = UserRole.Renter,
                        HasSelectedRole = true
                    },
                    new UserModel
                    {
                        Id = "user_2",
                        Name = "Trần Thị B",
                        Email = "tranthib@example.com",
                        ProfileImageUrl = "https://i.pravatar.cc/150?u=tranthib@example.com",
                        Location = "Hà Nội",
                        PhoneNumber = "0907654321",
                        Role = UserRole.Renter,
                        HasSelectedRole = true
                    },
                    new UserModel
                    {
                        Id = "landlord_1",
                        Name = "Lê Văn C (Chủ nhà)",
                        Email = "vanc@example.com",
                        ProfileImageUrl = "https://i.pravatar.cc/150?u=vanc@example.com",
                        Location = "TP. Hồ Chí Minh",
                        PhoneNumber = "0918888888",
                        Role = UserRole.Landlord,
                        HasSelectedRole = true
                    },
                    new UserModel
                    {
                        Id = "admin_1",
                        Name = "Admin Hệ Thống",
                        Email = "admin@example.com",
                        ProfileImageUrl = "https://i.pravatar.cc/150?u=admin@example.com",
                        Location = "TP. Hồ Chí Minh",
                        PhoneNumber = "0999999999",
                        Role = UserRole.Admin,
                        HasSelectedRole = true
                    }
                };

                context.Users.AddRange(users);
                context.SaveChanges();
            }

            // Seed Landmarks
            if (!context.Set<LandmarkModel>().Any())
            {
                var landmarks = new List<LandmarkModel>
                {
                    new LandmarkModel { Name = "Đại học HUTECH", Category = "University", Latitude = 10.800, Longitude = 106.711 },
                    new LandmarkModel { Name = "Đại học GTVT", Category = "University", Latitude = 10.805, Longitude = 106.715 },
                    new LandmarkModel { Name = "Đại học Bách Khoa", Category = "University", Latitude = 10.772, Longitude = 106.657 },
                    new LandmarkModel { Name = "Làng Đại học Thủ Đức", Category = "University", Latitude = 10.870, Longitude = 106.800 },
                    new LandmarkModel { Name = "ĐH Kinh Tế (UEH)", Category = "University", Latitude = 10.781, Longitude = 106.690 }
                };
                context.Set<LandmarkModel>().AddRange(landmarks);
                context.SaveChanges();
            }

            // Seed Rooms
            if (context.Rooms.Count() < 8)
            {
                var rooms = new List<RoomModel>
                {
                    new RoomModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        OwnerId = "landlord_1",
                        Title = "Studio Cửa Sổ Lớn Ban Công Thoáng Mát Quận Phú Nhuận",
                        Description = "Phòng Studio mới xây 100%, nội thất cao cấp: Smart TV, Tủ lạnh Inverter, Nệm lò xo, Tủ quần áo kịch trần. Có ban công riêng đón nắng sáng. Giờ giấc tự do, không chung chủ, hệ thống vân tay an ninh.",
                        Price = 5500000,
                        Address = "Huỳnh Văn Bánh, Phú Nhuận, TP. Hồ Chí Minh",
                        Location = "Phú Nhuận",
                        MainImageUrl = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=600&q=80",
                        Images = new List<RoomImageModel> {
                            new RoomImageModel { Url = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=600&q=80", SortOrder = 1 }
                        },
                        Rating = 4.8,
                        TotalReviews = 45,
                        Latitude = 10.795,
                        Longitude = 106.680,
                        Type = RoomType.Studio,
                        Amenities = new List<string> { "Ban công", "Máy giặt chung", "Giờ giấc tự do", "Smart TV", "Tủ lạnh" },
                        IsFavorite = false,
                        IsVerified = true,
                        ApprovalStatus = RoomStatus.Verified,
                        PostedBy = "Lê Văn C (Chủ nhà)",
                        PostedAt = DateTime.UtcNow.AddDays(-1)
                    },
                    new RoomModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        OwnerId = "landlord_1",
                        Title = "Ký Túc Xá Cao Cấp Sleepbox Tiện Nghi Ngay Làng Đại Học",
                        Description = "Ký túc xá mô hình Sleepbox chuẩn Nhật. Mỗi box có nệm, rèm che riêng tư, quạt thông gió, bàn xếp, đèn đọc sách. Bao điện nước, wifi, có máy lạnh 24/7. Thích hợp sinh viên.",
                        Price = 1800000,
                        Address = "Làng Đại Học Thủ Đức, TP. Hồ Chí Minh",
                        Location = "Thủ Đức",
                        MainImageUrl = "https://images.unsplash.com/photo-1555854877-bab0e564b8d5?auto=format&fit=crop&w=600&q=80",
                        Images = new List<RoomImageModel> {
                            new RoomImageModel { Url = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=600&q=80", SortOrder = 1 }
                        },
                        Rating = 4.9,
                        TotalReviews = 112,
                        Latitude = 10.870,
                        Longitude = 106.800,
                        Type = RoomType.Apartment,
                        Amenities = new List<string> { "Bao điện nước", "Sleepbox", "Máy lạnh", "Giữ xe miễn phí" },
                        IsVerified = true,
                        ApprovalStatus = RoomStatus.Verified,
                        PostedBy = "Lê Văn C (Chủ nhà)",
                        PostedAt = DateTime.UtcNow.AddDays(-2)
                    },
                    new RoomModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        OwnerId = "landlord_1",
                        Title = "Phòng Trọ Giá Rẻ Khu Vực Chợ Tân Bình",
                        Description = "Phòng trọ rộng 20m2, có gác lửng, kệ bếp nấu ăn, nhà vệ sinh riêng. Hẻm ba gác, cách chợ Tân Bình 500m. Điện 3.5k, nước 20k/khối.",
                        Price = 2800000,
                        Address = "Lý Thường Kiệt, Tân Bình, TP. Hồ Chí Minh",
                        Location = "Tân Bình",
                        MainImageUrl = "https://images.unsplash.com/photo-1493663284031-b7e3aefcae8e?auto=format&fit=crop&w=600&q=80",
                        Images = new List<RoomImageModel> {
                            new RoomImageModel { Url = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=600&q=80", SortOrder = 1 }
                        },
                        Rating = 4.1,
                        TotalReviews = 23,
                        Latitude = 10.785,
                        Longitude = 106.650,
                        Type = RoomType.Studio,
                        Amenities = new List<string> { "Gác xép", "Kệ bếp", "WC riêng" },
                        IsVerified = false,
                        ApprovalStatus = RoomStatus.Pending,
                        PostedBy = "Lê Văn C (Chủ nhà)",
                        PostedAt = DateTime.UtcNow.AddHours(-5)
                    },
                    new RoomModel
                    {
                        Id = Guid.NewGuid().ToString(),
                        OwnerId = "landlord_1",
                        Title = "Căn Hộ 1 Phòng Ngủ (1PN) Dịch Vụ Có Hồ Bơi Quận 2",
                        Description = "Cho thuê CHDV 1PN cao cấp tại khu Thảo Điền. Nội thất nhập khẩu. Tòa nhà có hồ bơi, phòng gym, bảo vệ 24/24. Thích hợp chuyên gia, người nước ngoài.",
                        Price = 9500000,
                        Address = "Thảo Điền, Quận 2, TP. Hồ Chí Minh",
                        Location = "Quận 2",
                        MainImageUrl = "https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?auto=format&fit=crop&w=600&q=80",
                        Images = new List<RoomImageModel> {
                            new RoomImageModel { Url = "https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?auto=format&fit=crop&w=600&q=80", SortOrder = 1 }
                        },
                        Rating = 5.0,
                        TotalReviews = 15,
                        Latitude = 10.800,
                        Longitude = 106.735,
                        Type = RoomType.Apartment,
                        Amenities = new List<string> { "Hồ bơi", "Phòng gym", "Bảo vệ 24/7", "Full nội thất" },
                        IsVerified = true,
                        ApprovalStatus = RoomStatus.Verified,
                        PostedBy = "Lê Văn C (Chủ nhà)",
                        PostedAt = DateTime.UtcNow.AddDays(-3)
                    }
                };

                context.Rooms.AddRange(rooms);
                context.SaveChanges();
            }

            // Seed Reviews
            if (!context.Reviews.Any())
            {
                var reviews = new List<ReviewModel>
                {
                    new ReviewModel
                    {
                        RoomId = "1",
                        UserId = "user_1",
                        UserName = "Nguyễn Văn A",
                        Rating = 5,
                        Comment = "Phòng trọ rất sạch sẽ, chủ nhà thân thiện hỗ trợ nhiệt tình. Gần trường nên đi lại rất thuận tiện.",
                        RenterUniversity = "ĐH HUTECH",
                        RentalDuration = "6 tháng",
                        CreatedAt = DateTime.UtcNow.AddDays(-10)
                    },
                    new ReviewModel
                    {
                        RoomId = "1",
                        UserId = "user_2",
                        UserName = "Trần Thị B",
                        Rating = 4,
                        Comment = "Không gian phòng thoáng mát, gác gỗ chắc chắn sạch đẹp. Điểm trừ duy nhất là giờ giấc hơi nghiêm ngặt một chút nhưng bù lại an ninh rất tốt.",
                        RenterUniversity = "ĐH Ngoại Thương",
                        RentalDuration = "1 năm",
                        CreatedAt = DateTime.UtcNow.AddDays(-5)
                    },
                    new ReviewModel
                    {
                        RoomId = "2",
                        UserId = "user_1",
                        UserName = "Nguyễn Văn A",
                        Rating = 4.5,
                        Comment = "Ký túc xá giường tầng sạch đẹp, máy lạnh chạy êm ru. Thích hợp cho các bạn sinh viên muốn tiết kiệm chi phí.",
                        RenterUniversity = "ĐH HUTECH",
                        RentalDuration = "3 tháng",
                        CreatedAt = DateTime.UtcNow.AddDays(-2)
                    }
                };

                context.Reviews.AddRange(reviews);
                context.SaveChanges();
            }

            // Seed Applications
            if (!context.Applications.Any())
            {
                var app = new ApplicationModel
                {
                    Id = "app_seeded_1",
                    RoomId = "1",
                    RoomTitle = "Phòng trọ gác gỗ đơn giản Quận 7",
                    RoomImageUrl = "https://images.unsplash.com/photo-1522771739844-6a9f6d5f14af?auto=format&fit=crop&w=600&q=80",
                    OwnerId = "landlord_1",
                    OwnerName = "Lê Văn C (Chủ nhà)",
                    RenterId = "user_1",
                    RenterName = "Nguyễn Văn A",
                    RenterPhone = "0901234567",
                    Message = "Tôi muốn đăng ký thuê phòng trọ.",
                    Status = "approved",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-14)
                };
                context.Applications.Add(app);
                context.SaveChanges();
            }
        }
    }
}

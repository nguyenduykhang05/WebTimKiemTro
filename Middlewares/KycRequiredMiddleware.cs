using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SmartRoomFinder.Data;
using SmartRoomFinder.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace SmartRoomFinder.Middlewares
{
    /// <summary>
    /// Chặn Chủ trọ (Landlord) chưa xác thực KYC khi cố truy cập các trang:
    /// - Tạo bài đăng mới (/Room/Create)
    /// - Quản lý phòng (/Room/Manage)
    /// - Dashboard chủ trọ (/Room/Dashboard)
    /// Chuyển hướng họ về trang nộp hồ sơ KYC.
    /// </summary>
    public class KycRequiredMiddleware
    {
        private readonly RequestDelegate _next;

        // Các path yêu cầu Landlord phải đã KYC
        private static readonly string[] _protectedPaths = new[]
        {
            "/room/create",
            "/room/manage",
            "/room/dashboard",
            "/room/edit",
            "/room/delete"
        };

        public KycRequiredMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Kiểm tra xem path có nằm trong danh sách bảo vệ không
            bool isProtected = false;
            foreach (var p in _protectedPaths)
            {
                if (path.StartsWith(p))
                {
                    isProtected = true;
                    break;
                }
            }

            if (isProtected
                && context.User.Identity?.IsAuthenticated == true
                && context.User.IsInRole("Landlord"))
            {
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                    if (user != null && !user.IsKycVerified)
                    {
                        // Chưa xác thực KYC → chuyển về trang nộp hồ sơ
                        context.Response.Redirect("/Profile/SubmitKyc?reason=required");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}

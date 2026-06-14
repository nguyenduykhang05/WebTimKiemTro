using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SmartRoomFinder.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace SmartRoomFinder.Middlewares
{
    public class MaintenanceMiddleware
    {
        private readonly RequestDelegate _next;

        public MaintenanceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();
            
            // Allow access to auth, api, static files, and admin routes
            if (path != null && (
                path.StartsWith("/auth") || 
                path.StartsWith("/api") || 
                path.StartsWith("/admin") || 
                path.StartsWith("/css") || 
                path.StartsWith("/js") || 
                path.StartsWith("/images") ||
                path.StartsWith("/uploads")))
            {
                await _next(context);
                return;
            }

            // Check if user is Admin
            if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("Admin"))
            {
                await _next(context);
                return;
            }

            // Check maintenance mode in DB
            using (var scope = context.RequestServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var maintenanceModeSetting = await dbContext.SystemSettings.FirstOrDefaultAsync(s => s.Key == "MaintenanceMode");
                
                if (maintenanceModeSetting != null && maintenanceModeSetting.Value.ToLower() == "true")
                {
                    // Render a basic maintenance page
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.ContentType = "text/html; charset=utf-8";
                    
                    var html = @"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <title>Bảo trì hệ thống</title>
                            <link href='https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css' rel='stylesheet'>
                            <link href='https://fonts.googleapis.com/css2?family=Poppins:wght@500;700&display=swap' rel='stylesheet'>
                            <style>
                                body { background-color: #0F172A; color: #fff; font-family: 'Poppins', sans-serif; display: flex; align-items: center; justify-content: center; height: 100vh; margin: 0; }
                                .container { text-align: center; max-width: 600px; padding: 40px; background: #1E293B; border-radius: 20px; box-shadow: 0 20px 50px rgba(0,0,0,0.5); }
                                h1 { color: #F59E0B; font-weight: 700; margin-bottom: 20px; }
                                p { color: #94A3B8; font-size: 1.1rem; line-height: 1.6; }
                                .icon { font-size: 60px; color: #2563EB; margin-bottom: 20px; }
                            </style>
                        </head>
                        <body>
                            <div class='container'>
                                <div class='icon'>🛠️</div>
                                <h1>Hệ thống đang bảo trì</h1>
                                <p>Chúng tôi đang tiến hành nâng cấp hệ thống để mang lại trải nghiệm tốt hơn. Vui lòng quay lại sau ít phút. Xin lỗi vì sự bất tiện này!</p>
                                <a href='#' onclick='location.reload()' class='btn btn-outline-light mt-4 rounded-pill px-4'>Tải lại trang</a>
                                <div style='margin-top: 40px;'>
";
                    if (context.User.Identity?.IsAuthenticated == true)
                    {
                        html += "<a href='/Auth/Logout' style='color: rgba(255,255,255,0.5); text-decoration: none; font-size: 0.9rem;'>Đăng xuất tài khoản hiện tại</a>";
                    }
                    else
                    {
                        html += "<a href='/Auth/Login' style='color: rgba(255,255,255,0.5); text-decoration: none; font-size: 0.9rem;'>Quay lại trang đăng nhập</a>";
                    }
                    
                    html += @"
                                </div>
                            </div>
                        </body>
                        </html>
                    ";
                    await context.Response.WriteAsync(html);
                    return;
                }
            }

            await _next(context);
        }
    }
}

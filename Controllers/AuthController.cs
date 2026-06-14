using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;
using System.Net;
using System.Net.Mail;

using Microsoft.AspNetCore.Identity;

namespace SmartRoomFinder.Controllers
{
    public class AuthController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;
        private readonly IPasswordHasher<UserModel> _passwordHasher;

        public AuthController(AppDbContext context, IMemoryCache cache, IConfiguration configuration, IPasswordHasher<UserModel> passwordHasher)
        {
            _context = context;
            _cache = cache;
            _configuration = configuration;
            _passwordHasher = passwordHasher;
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View();
            }
            if (string.IsNullOrEmpty(user.PasswordHash))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản của bạn là tài khoản cũ chưa có mật khẩu. Vui lòng sử dụng tính năng 'Quên mật khẩu' để thiết lập mật khẩu mới.");
                return View();
            }

            // Verify password
            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            if (!user.HasSelectedRole)
            {
                return RedirectToAction("ChooseRole");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(string name, string email, string phone, string password, UserRole role)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email))
            {
                ModelState.AddModelError(string.Empty, "Email đã được đăng ký.");
                return View();
            }

            var newUser = new UserModel
            {
                Name = name,
                Email = email,
                PhoneNumber = phone,
                Role = role,
                HasSelectedRole = true
            };
            
            newUser.PasswordHash = _passwordHasher.HashPassword(newUser, password);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Auto-login
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, newUser.Id),
                new Claim(ClaimTypes.Name, newUser.Name),
                new Claim(ClaimTypes.Email, newUser.Email),
                new Claim(ClaimTypes.Role, newUser.Role.ToString())
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ChooseRole()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChooseRole(UserRole role)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login");

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Role = role;
                user.HasSelectedRole = true;
                await _context.SaveChangesAsync();

                // Re-sign in to update role claim
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
            }

            return RedirectToAction("Index", "Home");
        }

        // --- FORGOT PASSWORD ---
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email không tồn tại trong hệ thống.");
                return View();
            }

            try
            {
                // Generate 6-digit OTP
                var otp = new Random().Next(100000, 999999).ToString();
                
                // Save to cache for 5 minutes
                _cache.Set($"OTP_{email}", otp, TimeSpan.FromMinutes(5));

                // Send email
                var host = _configuration["Smtp:Host"];
                var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
                var enableSsl = bool.Parse(_configuration["Smtp:EnableSsl"] ?? "true");
                var username = _configuration["Smtp:Username"];
                var password = _configuration["Smtp:Password"];
                var fromEmail = _configuration["Smtp:FromEmail"];
                var fromName = _configuration["Smtp:FromName"];

                using (var client = new SmtpClient(host, port))
                {
                    client.Credentials = new NetworkCredential(username, password);
                    client.EnableSsl = enableSsl;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(fromEmail, fromName),
                        Subject = "Mã xác nhận đặt lại mật khẩu - Smart Room Finder",
                        Body = $@"
<div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 1px solid #e2e8f0;"">
    <div style=""background: linear-gradient(135deg, #f7701d 0%, #e65c00 100%); padding: 30px; text-align: center;"">
        <h2 style=""color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 1px;"">SMART ROOM FINDER</h2>
    </div>
    <div style=""padding: 40px 30px;"">
        <h3 style=""color: #1e293b; margin-top: 0; font-size: 20px;"">Xin chào {user.Name},</h3>
        <p style=""color: #475569; font-size: 16px; line-height: 1.6; margin-bottom: 25px;"">
            Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Vui lòng sử dụng mã OTP dưới đây để hoàn tất quá trình này:
        </p>
        <div style=""background-color: #f8fafc; border: 2px dashed #cbd5e1; border-radius: 8px; padding: 20px; text-align: center; margin-bottom: 25px;"">
            <span style=""font-size: 36px; font-weight: 800; color: #f7701d; letter-spacing: 5px;"">{otp}</span>
        </div>
        <p style=""color: #ef4444; font-size: 14px; font-weight: 600; margin-bottom: 30px; text-align: center;"">
            <span style=""margin-right: 5px;"">⏳</span> Mã này chỉ có hiệu lực trong vòng 5 phút.
        </p>
        <hr style=""border: none; border-top: 1px solid #e2e8f0; margin-bottom: 20px;"" />
        <p style=""color: #64748b; font-size: 13px; line-height: 1.5; margin: 0;"">
            Nếu bạn không yêu cầu đặt lại mật khẩu, xin vui lòng bỏ qua email này hoặc liên hệ với bộ phận hỗ trợ của chúng tôi nếu bạn cảm thấy tài khoản bị đe dọa.
        </p>
    </div>
    <div style=""background-color: #f1f5f9; padding: 20px; text-align: center;"">
        <p style=""color: #94a3b8; font-size: 12px; margin: 0;"">
            &copy; {DateTime.Now.Year} Smart Room Finder. Tất cả quyền được bảo lưu.
        </p>
    </div>
</div>",
                        IsBodyHtml = true
                    };
                    mailMessage.To.Add(email);

                    await client.SendMailAsync(mailMessage);
                }

                TempData["ResetEmail"] = email;
                TempData["SuccessMessage"] = "Đã gửi mã xác nhận OTP qua Email. Vui lòng kiểm tra hộp thư.";
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi gửi email: " + ex.Message);
                return View();
            }
            
            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetEmail"] == null)
            {
                return RedirectToAction("Login");
            }
            ViewBag.Email = TempData["ResetEmail"];
            TempData.Keep("ResetEmail"); // Keep for POST or reload
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(string email, string otp, string newPassword, string confirmPassword)
        {
            ViewBag.Email = email;

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError(string.Empty, "Mật khẩu xác nhận không khớp.");
                return View();
            }

            // Verify OTP
            if (!_cache.TryGetValue($"OTP_{email}", out string? cachedOtp) || cachedOtp != otp)
            {
                ModelState.AddModelError(string.Empty, "Mã OTP không hợp lệ hoặc đã hết hạn.");
                return View();
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
                await _context.SaveChangesAsync();
                
                // Remove OTP after successful reset
                _cache.Remove($"OTP_{email}");

                TempData["SuccessMessage"] = "Khôi phục mật khẩu thành công! Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}

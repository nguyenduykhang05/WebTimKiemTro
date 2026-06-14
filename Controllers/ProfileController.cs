using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartRoomFinder.Models;
using SmartRoomFinder.Data;

namespace SmartRoomFinder.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly AppDbContext _context;

        public ProfileController(AppDbContext context)
        {
            _context = context;
        }

        // View profile page
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        // Edit profile page (GET)
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return RedirectToAction("Login", "Auth");

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return View(user);
        }

        // Edit profile page (POST)
        [HttpPost]
        public async Task<IActionResult> Edit(UserModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null || model.Id != userId) return Forbid();

            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.Name = model.Name;
            user.PhoneNumber = model.PhoneNumber;
            user.Location = model.Location;
            user.ProfileImageUrl = model.ProfileImageUrl;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Refresh name claim if changed
            return RedirectToAction("Index");
        }
    }
}

using Assignment.Models.Data;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(EnglishCenterDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // 1. GET: Hiển thị hồ sơ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Login", "Account");

            // Kiểm tra Học viên
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => (s.User != null && s.User.Username == username) || s.StudentCode == username);

            if (student != null)
            {
                ViewBag.RoleName = "Học viên";
                return View(student);
            }

            // Nếu là Admin / Giáo vụ
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Username == username);
            if (user != null)
            {
                var role = user.Role?.RoleName ?? "Admin";

                // Chuẩn hóa tên vai trò
                ViewBag.RoleName = role.Equals("GiaoVu", StringComparison.OrdinalIgnoreCase) || role.Equals("Giáo vụ", StringComparison.OrdinalIgnoreCase)
                    ? "Giáo vụ"
                    : "Admin";

                return View(new Student { StudentCode = user.Username, FullName = user.Username });
            }

            return NotFound("Không tìm thấy dữ liệu.");
        }

        // 2. POST: Đổi avatar (Chỉ cho Học viên)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(int studentId, IFormFile? avatarFile)
        {
            var student = await _context.Students.FindAsync(studentId);
            if (student != null && avatarFile?.Length > 0)
            {
                var folder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(avatarFile.FileName)}";
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create))
                {
                    await avatarFile.CopyToAsync(stream);
                }

                student.AvatarUrl = $"/uploads/avatars/{fileName}";
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật ảnh thành công!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
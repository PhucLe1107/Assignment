using Assignment.Models.Data;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers
{
    [Authorize] // Yêu cầu sinh viên phải đăng nhập
    public class ProfileController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public ProfileController(EnglishCenterDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // 1. GET: Lấy thông tin học viên hiển thị ra trang Hồ sơ
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;

            if (string.IsNullOrEmpty(username))
            {
                return RedirectToAction("Login", "Account");
            }

            // Tìm thông tin sinh viên theo tài khoản đang đăng nhập
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => (s.User != null && s.User.Username == username) || s.StudentCode == username);

            if (student == null)
            {
                return NotFound("Không tìm thấy dữ liệu hồ sơ học viên.");
            }

            return View(student);
        }

        // 2. POST: Chỉ cập nhật Ảnh đại diện
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(int studentId, IFormFile? avatarFile)
        {
            // Lấy thông tin sinh viên gốc từ CSDL
            var student = await _context.Students.FindAsync(studentId);

            if (student == null)
            {
                return NotFound("Không tìm thấy dữ liệu học viên.");
            }

            // Kiểm tra xem người dùng có chọn file ảnh mới không
            if (avatarFile != null && avatarFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Tạo tên file duy nhất tránh bị trùng tên
                var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(avatarFile.FileName);
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await avatarFile.CopyToAsync(fileStream);
                }

                // Chỉ cập nhật đường dẫn Avatar
                student.AvatarUrl = "/uploads/avatars/" + uniqueFileName;

                _context.Update(student);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật ảnh đại diện thành công!";
            }
            else
            {
                TempData["SuccessMessage"] = "Bạn chưa chọn ảnh mới nào.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
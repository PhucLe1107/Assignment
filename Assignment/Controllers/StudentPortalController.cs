using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Assignment.Models.Data;
using Assignment.Models.Entities;

namespace Assignment.Controllers
{
    // Cổng thông tin dành riêng cho học viên: chỉ xem dữ liệu của chính mình
    [Authorize(Roles = "SinhVien")]
    public class StudentPortalController : Controller
    {
        private readonly EnglishCenterDbContext _context;

        public StudentPortalController(EnglishCenterDbContext context)
        {
            _context = context;
        }

        // GET: /student-portal/my-learning
        public async Task<IActionResult> MyLearning()
        {
            var query = StudentWithClasses()
                .Include(s => s.Enrollments!)
                    .ThenInclude(e => e.Attendances);

            // null: tài khoản chưa gắn hồ sơ học viên, view sẽ hiện thông báo
            return View(await FindCurrentStudentAsync(query));
        }

        // GET: /student-portal/my-attendance
        public async Task<IActionResult> MyAttendance()
        {
            var query = StudentWithClasses()
                .Include(s => s.Enrollments!)
                    .ThenInclude(e => e.Attendances);

            return View(await FindCurrentStudentAsync(query));
        }

        // GET: /student-portal/my-grades
        public async Task<IActionResult> MyGrades()
        {
            var query = StudentWithClasses()
                .Include(s => s.Enrollments!)
                    .ThenInclude(e => e.Grades);

            return View(await FindCurrentStudentAsync(query));
        }

        private IQueryable<Student> StudentWithClasses()
        {
            return _context.Students
                .Include(s => s.Enrollments!)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course);
        }

        // UserId được lưu vào claim NameIdentifier lúc đăng nhập (AccountController)
        private async Task<Student?> FindCurrentStudentAsync(IQueryable<Student> query)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return null;
            }

            return await query
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment.Models.Common;
using Assignment.Models.Data;
using Assignment.Models.Entities;
using Microsoft.Extensions.Localization;

namespace Assignment.Controllers
{
    [Authorize(Roles = "Admin,GiaoVu")]
    public class ClassController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public ClassController(EnglishCenterDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: /Class?search=abc&courseId=1&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string search, int? courseId, int? page)
        {
            ViewData["CurrentFilter"] = search;
            ViewData["CurrentCourseId"] = courseId;

            ViewBag.CourseList = new SelectList(await _context.Courses.Where(c => c.IsActive).ToListAsync(), "CourseId", "CourseName", courseId);

            var query = _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Enrollments)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.ClassCode.Contains(search)
                                      || c.ClassName.Contains(search)
                                      || (c.LecturerName != null && c.LecturerName.Contains(search)));
            }

            if (courseId.HasValue && courseId.Value > 0)
            {
                query = query.Where(c => c.CourseId == courseId.Value);
            }

            query = query.OrderByDescending(c => c.ClassId);

            int pageSize = 5;
            int currentPage = page ?? 1;
            var pagedClasses = await PagedList<Class>.CreateAsync(query, currentPage, pageSize);

            return View(pagedClasses);
        }

        // GET: /Class/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var @class = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Enrollments)
                    .ThenInclude(e => e.Student)
                .FirstOrDefaultAsync(m => m.ClassId == id);

            if (@class == null) return NotFound();

            return View(@class);
        }

        // GET: /Class/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? selectedCourseId)
        {
            await PopulateCoursesDropDownList(selectedCourseId);
            return View(new Class { StartDate = DateTime.Today });
        }

        // POST: /Class/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Class @class)
        {
            // Kiểm tra trùng mã lớp
            if (await _context.Classes.AnyAsync(c => c.ClassCode == @class.ClassCode))
            {
                ModelState.AddModelError("ClassCode", _localizer["Mã lớp này đã tồn tại trên hệ thống."]);
            }

            // Gỡ bỏ kiểm tra navigation property để tránh ModelState bị false
            ModelState.Remove(nameof(@class.Course));
            ModelState.Remove(nameof(@class.Enrollments));

            if (ModelState.IsValid)
            {
                _context.Classes.Add(@class);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Thêm mới lớp học thành công!"].Value;
                return RedirectToAction(nameof(Index));
            }

            await PopulateCoursesDropDownList(@class.CourseId);
            return View(@class);
        }

        // GET: /Class/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var @class = await _context.Classes.FindAsync(id);
            if (@class == null) return NotFound();

            await PopulateCoursesDropDownList(@class.CourseId);
            return View(@class);
        }

        // POST: /Class/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Class @class)
        {
            if (id != @class.ClassId) return NotFound();

            ModelState.Remove(nameof(@class.Course));
            ModelState.Remove(nameof(@class.Enrollments));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Classes.Update(@class);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = _localizer["Cập nhật lớp học thành công!"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassExists(@class.ClassId)) return NotFound();
                    throw;
                }
            }

            await PopulateCoursesDropDownList(@class.CourseId);
            return View(@class);
        }

        // GET: /Class/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var @class = await _context.Classes
                .Include(c => c.Course)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(m => m.ClassId == id);

            if (@class == null) return NotFound();

            return View(@class);
        }

        // POST: /Class/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var @class = await _context.Classes
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.ClassId == id);

            if (@class != null)
            {
                // Kiểm tra ràng buộc khóa ngoại với Enrollment
                if (@class.Enrollments != null && @class.Enrollments.Any())
                {
                    TempData["ErrorMessage"] = _localizer["Không thể xóa lớp học vì đã có học viên đăng ký!"].Value;
                    return RedirectToAction(nameof(Index));
                }

                _context.Classes.Remove(@class);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Đã xóa lớp học thành công!"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ClassExists(int id)
        {
            return _context.Classes.Any(e => e.ClassId == id);
        }

        private async Task PopulateCoursesDropDownList(object? selectedCourse = null)
        {
            var coursesQuery = await _context.Courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.CourseName)
                .ToListAsync();

            ViewBag.CourseId = new SelectList(coursesQuery, "CourseId", "CourseName", selectedCourse);
        }
    }
}

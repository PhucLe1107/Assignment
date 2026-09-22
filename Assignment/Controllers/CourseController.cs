using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Assignment.Models.Data;
using Assignment.Models.Entities;
using Assignment.Models.Common;

namespace Assignment.Controllers
{
    [Authorize(Roles = "Admin,GiaoVu")]
    public class CourseController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public CourseController(EnglishCenterDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: /Course
        public async Task<IActionResult> Index(string search, int? page)
        {
            ViewData["CurrentFilter"] = search;

            var coursesQuery = _context.Courses.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                coursesQuery = coursesQuery.Where(c => c.CourseName.Contains(search));
            }

            coursesQuery = coursesQuery.OrderByDescending(c => c.CourseId);

            int pageSize = 5;
            int currentPage = page ?? 1;

            var pagedCourses = await PagedList<Course>.CreateAsync(coursesQuery, currentPage, pageSize);

            return View(pagedCourses);
        }

        // GET: /Course/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .Include(c => c.Classes)
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // GET: /Course/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Course/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Course course)
        {
            if (ModelState.IsValid)
            {
                if (course.ThumbnailFile != null)
                {
                    course.ThumbnailUrl = await UploadImageAsync(course.ThumbnailFile);
                }
                else
                {
                    course.ThumbnailUrl = "/img/default-course.jpg";
                }

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thêm mới khóa học thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(course);
        }

        // GET: /Course/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses.FindAsync(id);
            if (course == null) return NotFound();

            return View(course);
        }

        // POST: /Course/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Course course)
        {
            if (id != course.CourseId) return NotFound();

            ModelState.Remove(nameof(course.ThumbnailUrl));
            ModelState.Remove(nameof(course.ThumbnailFile));

            if (ModelState.IsValid)
            {
                try
                {
                    var existingCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == id);
                    if (existingCourse == null) return NotFound();

                    if (course.ThumbnailFile != null)
                    {
                        if (!string.IsNullOrEmpty(existingCourse.ThumbnailUrl) && !existingCourse.ThumbnailUrl.Contains("default-course.jpg"))
                        {
                            DeleteImageFile(existingCourse.ThumbnailUrl);
                        }

                        course.ThumbnailUrl = await UploadImageAsync(course.ThumbnailFile);
                    }
                    else
                    {
                        course.ThumbnailUrl = !string.IsNullOrEmpty(existingCourse.ThumbnailUrl)
                            ? existingCourse.ThumbnailUrl
                            : "/img/default-course.jpg";
                    }

                    _context.Courses.Update(course);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật khóa học thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CourseExists(course.CourseId)) return NotFound();
                    throw;
                }
            }

            return View(course);
        }

        // GET: /Course/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
                .FirstOrDefaultAsync(m => m.CourseId == id);

            if (course == null) return NotFound();

            return View(course);
        }

        // POST: /Course/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Classes)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course != null)
            {
                if (course.Classes != null && course.Classes.Any())
                {
                    TempData["ErrorMessage"] = "Không thể xóa vì khóa học đã có các lớp học liên kết!";
                    return RedirectToAction(nameof(Index));
                }

                // Xóa file ảnh trên ổ cứng
                DeleteImageFile(course.ThumbnailUrl);

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa khóa học thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }

        private async Task<string> UploadImageAsync(IFormFile file)
        {
            string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "courses");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return "/uploads/courses/" + uniqueFileName;
        }

        private void DeleteImageFile(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;

            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/'));
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}
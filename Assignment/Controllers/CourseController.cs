using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Assignment.Models.Data;
using Assignment.Models.Entities;
using Assignment.Models.Common;
using Assignment.Storage;
using Microsoft.Extensions.Localization;

namespace Assignment.Controllers
{
    [Authorize(Roles = "Admin,GiaoVu")]
    public class CourseController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IImageStorage _imageStorage;
        private readonly IStringLocalizer<SharedResource> _localizer;
        private readonly ILogger<CourseController> _logger;

        public CourseController(
            EnglishCenterDbContext context,
            IImageStorage imageStorage,
            IStringLocalizer<SharedResource> localizer,
            ILogger<CourseController> logger)
        {
            _context = context;
            _imageStorage = imageStorage;
            _localizer = localizer;
            _logger = logger;
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
                string? uploadedImageUrl = null;
                try
                {
                    if (course.ThumbnailFile != null)
                    {
                        uploadedImageUrl = await _imageStorage.UploadAsync(
                            course.ThumbnailFile,
                            ImageStoragePaths.CourseThumbnails);
                        course.ThumbnailUrl = uploadedImageUrl;
                    }
                    else
                    {
                        course.ThumbnailUrl = "/img/default-course.jpg";
                    }

                    _context.Courses.Add(course);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = _localizer["Thêm mới khóa học thành công!"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch
                {
                    await TryDeleteImageAsync(uploadedImageUrl);
                    throw;
                }
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
                string? uploadedImageUrl = null;
                try
                {
                    var existingCourse = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.CourseId == id);
                    if (existingCourse == null) return NotFound();

                    if (course.ThumbnailFile != null)
                    {
                        uploadedImageUrl = await _imageStorage.UploadAsync(
                            course.ThumbnailFile,
                            ImageStoragePaths.CourseThumbnails);
                        course.ThumbnailUrl = uploadedImageUrl;
                    }
                    else
                    {
                        course.ThumbnailUrl = !string.IsNullOrEmpty(existingCourse.ThumbnailUrl)
                            ? existingCourse.ThumbnailUrl
                            : "/img/default-course.jpg";
                    }

                    _context.Courses.Update(course);
                    await _context.SaveChangesAsync();

                    if (course.ThumbnailFile != null)
                    {
                        await TryDeleteImageAsync(existingCourse.ThumbnailUrl);
                    }

                    TempData["SuccessMessage"] = _localizer["Cập nhật khóa học thành công!"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    await TryDeleteImageAsync(uploadedImageUrl);
                    if (!CourseExists(course.CourseId)) return NotFound();
                    throw;
                }
                catch
                {
                    await TryDeleteImageAsync(uploadedImageUrl);
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
                    TempData["ErrorMessage"] = _localizer["Không thể xóa vì khóa học đã có các lớp học liên kết!"].Value;
                    return RedirectToAction(nameof(Index));
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                await TryDeleteImageAsync(course.ThumbnailUrl);
                TempData["SuccessMessage"] = _localizer["Đã xóa khóa học thành công!"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }

        private async Task TryDeleteImageAsync(string? imageUrl)
        {
            try
            {
                await _imageStorage.DeleteAsync(imageUrl);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Could not delete course image {ImageUrl} from R2.", imageUrl);
            }
        }
    }
}

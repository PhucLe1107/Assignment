using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Assignment.Models.Common;
using Assignment.Models.Data;
using Assignment.Models.Entities;

namespace Assignment.Controllers
{
    [Authorize(Roles = "Admin,GiaoVu")]
    public class StudentController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public StudentController(EnglishCenterDbContext context, IWebHostEnvironment webHostEnvironment, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _localizer = localizer;
        }

        // GET: /Student?search=abc&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? page)
        {
            ViewData["CurrentSearch"] = search;

            var query = _context.Students
                .Include(s => s.Enrollments)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.StudentCode.Contains(search)
                                      || s.FullName.Contains(search)
                                      || s.Email.Contains(search)
                                      || s.PhoneNumber.Contains(search));
            }

            query = query.OrderByDescending(s => s.StudentId);

            int pageSize = 10;
            int currentPage = page ?? 1;
            var pagedStudents = await PagedList<Student>.CreateAsync(query, currentPage, pageSize);

            return View(pagedStudents);
        }

        // GET: /Student/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.User)
                .Include(s => s.Enrollments!)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // GET: /Student/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Student
            {
                DateOfBirth = new DateTime(2000, 1, 1),
                Gender = true,
                EntryLevel = "Beginner"
            });
        }

        // POST: /Student/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Student student, bool createAccount = false, string? accountUsername = null, string? accountPassword = null)
        {
            // 1. Kiểm tra trùng thông tin
            if (await _context.Students.AnyAsync(s => s.StudentCode == student.StudentCode))
            {
                ModelState.AddModelError("StudentCode", _localizer["Mã sinh viên này đã tồn tại."]);
            }
            if (await _context.Students.AnyAsync(s => s.Email == student.Email))
            {
                ModelState.AddModelError("Email", _localizer["Địa chỉ email này đã được sử dụng."]);
            }
            if (await _context.Students.AnyAsync(s => s.PhoneNumber == student.PhoneNumber))
            {
                ModelState.AddModelError("PhoneNumber", _localizer["Số điện thoại này đã được sử dụng."]);
            }

            string usernameToCreate = string.IsNullOrWhiteSpace(accountUsername) ? student.StudentCode.Trim() : accountUsername.Trim();

            if (createAccount && await _context.Users.AnyAsync(u => u.Username == usernameToCreate))
            {
                ModelState.AddModelError("", _localizer["Tên đăng nhập '{0}' đã có người sử dụng.", usernameToCreate]);
            }

            // Xử lý upload Avatar
            if (student.AvatarFile != null && student.AvatarFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(student.AvatarFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await student.AvatarFile.CopyToAsync(fileStream);
                }
                student.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
            }
            else
            {
                student.AvatarUrl ??= "/img/undraw_profile.svg";
            }

            student.Address ??= string.Empty;
            student.EntryLevel ??= "Beginner";

            ModelState.Remove(nameof(student.User));
            ModelState.Remove(nameof(student.Enrollments));

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (createAccount)
                    {
                        var newUser = new User
                        {
                            Username = usernameToCreate,
                            Password = string.IsNullOrWhiteSpace(accountPassword) ? "123456" : accountPassword,
                            IsActive = true,
                            RoleId = 3
                        };

                        _context.Users.Add(newUser);
                        await _context.SaveChangesAsync();

                        student.UserId = newUser.UserId;
                    }

                    _context.Students.Add(student);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = createAccount
                        ? _localizer["Thêm học viên thành công! Tài khoản: {0} (MK: 123456)", usernameToCreate].Value
                        : _localizer["Thêm mới học viên thành công!"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", _localizer["Đã có lỗi xảy ra: {0}", ex.Message]);
                }
            }

            return View(student);
        }

        // GET: /Student/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students.FindAsync(id);
            if (student == null) return NotFound();

            return View(student);
        }

        // POST: /Student/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Student student)
        {
            if (id != student.StudentId) return NotFound();

            if (await _context.Students.AnyAsync(s => s.StudentCode == student.StudentCode && s.StudentId != id))
            {
                ModelState.AddModelError("StudentCode", _localizer["Mã sinh viên này đã tồn tại trên hệ thống."]);
            }

            if (await _context.Students.AnyAsync(s => s.Email == student.Email && s.StudentId != id))
            {
                ModelState.AddModelError("Email", _localizer["Địa chỉ email này đã được sử dụng bởi học viên khác."]);
            }

            if (await _context.Students.AnyAsync(s => s.PhoneNumber == student.PhoneNumber && s.StudentId != id))
            {
                ModelState.AddModelError("PhoneNumber", _localizer["Số điện thoại này đã được sử dụng bởi học viên khác."]);
            }

            // Xử lý upload ảnh mới nếu có
            if (student.AvatarFile != null && student.AvatarFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(student.AvatarFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await student.AvatarFile.CopyToAsync(fileStream);
                }
                student.AvatarUrl = "/uploads/avatars/" + uniqueFileName;
            }

            student.AvatarUrl ??= "/img/undraw_profile.svg";
            student.Address ??= string.Empty;
            student.EntryLevel ??= "Beginner";

            ModelState.Remove(nameof(student.User));
            ModelState.Remove(nameof(student.Enrollments));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Students.Update(student);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = _localizer["Cập nhật thông tin học viên thành công!"].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.StudentId)) return NotFound();
                    throw;
                }
            }

            return View(student);
        }

        // GET: /Student/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var student = await _context.Students
                .Include(s => s.Enrollments!)
                    .ThenInclude(e => e.Class)
                .FirstOrDefaultAsync(m => m.StudentId == id);

            if (student == null) return NotFound();

            return View(student);
        }

        // POST: /Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students
                .Include(s => s.Enrollments)
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student != null)
            {
                if (student.Enrollments != null && student.Enrollments.Any())
                {
                    TempData["ErrorMessage"] = _localizer["Không thể xóa học viên này vì đang có dữ liệu đăng ký lớp học!"].Value;
                    return RedirectToAction(nameof(Index));
                }

                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Đã xóa hồ sơ học viên thành công!"].Value;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.StudentId == id);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Assignment.Localization;
using Assignment.Models;
using Assignment.Models.Common;
using Assignment.Models.Data;
using Assignment.Models.Entities;

namespace Assignment.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public UserController(EnglishCenterDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: /User?search=...&roleId=...&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? roleId, int? page)
        {
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentRoleId"] = roleId;

            ViewBag.Roles = new SelectList(LocalizeRoles(await _context.Roles.ToListAsync()), "RoleId", "Name", roleId);

            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Student)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(u => u.Username.Contains(search)
                                      || (u.Student != null && (u.Student.FullName.Contains(search) || u.Student.StudentCode.Contains(search))));
            }

            if (roleId.HasValue && roleId.Value > 0)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            query = query.OrderByDescending(u => u.UserId);

            int pageSize = 10;
            int currentPage = page ?? 1;
            var pagedUsers = await PagedList<User>.CreateAsync(query, currentPage, pageSize);

            return View(pagedUsers);
        }

        // GET: /User/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownData();
            return View(new UserCreateViewModel());
        }

        // POST: /User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            var selectedRole = await _context.Roles.FindAsync(model.RoleId);
            bool isSinhVienRole = selectedRole?.RoleName == "SinhVien";

            if (isSinhVienRole)
            {
                if (!model.StudentId.HasValue || model.StudentId.Value <= 0)
                {
                    ModelState.AddModelError("StudentId", _localizer["Tài khoản có vai trò Sinh viên bắt buộc phải liên kết với một hồ sơ Học viên!"]);
                }
                else
                {
                    bool studentAlreadyHasAccount = await _context.Students.AnyAsync(s => s.StudentId == model.StudentId.Value && s.UserId != null);
                    if (studentAlreadyHasAccount)
                    {
                        ModelState.AddModelError("StudentId", _localizer["Học viên này đã được cấp tài khoản khác trước đó!"]);
                    }
                }
            }

            if (!isSinhVienRole)
            {
                model.StudentId = null;
            }

            if (await _context.Users.AnyAsync(u => u.Username == model.Username.Trim()))
            {
                ModelState.AddModelError("Username", _localizer["Tên đăng nhập này đã được sử dụng!"]);
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = new User
                    {
                        Username = model.Username.Trim(),
                        Password = BCrypt.Net.BCrypt.HashPassword(model.Password),
                        RoleId = model.RoleId,
                        IsActive = model.IsActive
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    if (isSinhVienRole && model.StudentId.HasValue)
                    {
                        var student = await _context.Students.FindAsync(model.StudentId.Value);
                        if (student != null)
                        {
                            student.UserId = user.UserId;
                            _context.Students.Update(student);
                            await _context.SaveChangesAsync();
                        }
                    }

                    await transaction.CommitAsync();
                    TempData["SuccessMessage"] = _localizer["Tạo thành công tài khoản [{0}]!", user.Username].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", _localizer["Đã xảy ra lỗi khi tạo tài khoản: {0}", ex.Message]);
                }
            }

            await LoadDropdownData(model.RoleId, model.StudentId);
            return View(model);
        }

        // GET: /User/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            var model = new UserEditViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                RoleId = user.RoleId,
                IsActive = user.IsActive,
                StudentId = user.Student?.StudentId,
                LinkedStudentName = user.Student?.FullName,
                LinkedStudentCode = user.Student?.StudentCode
            };

            await LoadDropdownData(user.RoleId, user.Student?.StudentId);
            return View(model);
        }

        // POST: /User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, UserEditViewModel model)
        {
            if (id != model.UserId) return NotFound();

            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            var selectedRole = await _context.Roles.FindAsync(model.RoleId);
            bool isSinhVienRole = selectedRole?.RoleName == "SinhVien";

            if (isSinhVienRole)
            {
                int? currentStudentId = user.Student?.StudentId ?? model.StudentId;
                if (!currentStudentId.HasValue || currentStudentId.Value <= 0)
                {
                    ModelState.AddModelError("StudentId", _localizer["Tài khoản Sinh viên bắt buộc phải liên kết với một hồ sơ Học viên!"]);
                }
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    user.RoleId = model.RoleId;
                    user.IsActive = model.IsActive;

                    if (!string.IsNullOrWhiteSpace(model.NewPassword))
                    {
                        user.Password = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                    }

                    if (!isSinhVienRole && user.Student != null)
                    {
                        user.Student.UserId = null;
                        _context.Students.Update(user.Student);
                    }
                    else if (isSinhVienRole && model.StudentId.HasValue && (user.Student == null || user.Student.StudentId != model.StudentId.Value))
                    {
                        if (user.Student != null)
                        {
                            user.Student.UserId = null;
                            _context.Students.Update(user.Student);
                        }

                        var newStudent = await _context.Students.FindAsync(model.StudentId.Value);
                        if (newStudent != null)
                        {
                            newStudent.UserId = user.UserId;
                            _context.Students.Update(newStudent);
                        }
                    }

                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = _localizer["Cập nhật tài khoản [{0}] thành công!", user.Username].Value;
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", _localizer["Đã có lỗi xảy ra: {0}", ex.Message]);
                }
            }

            await LoadDropdownData(model.RoleId, model.StudentId);
            return View(model);
        }

        // POST: /User/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                if (user.Username == User.Identity?.Name)
                {
                    TempData["ErrorMessage"] = _localizer["Bạn không thể tự khóa tài khoản của chính mình!"].Value;
                    return RedirectToAction(nameof(Index));
                }

                user.IsActive = !user.IsActive;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = user.IsActive
                    ? _localizer["Đã kích hoạt tài khoản [{0}] thành công!", user.Username].Value
                    : _localizer["Đã khóa tài khoản [{0}] thành công!", user.Username].Value;
            }
            return RedirectToAction(nameof(Index));
        }

        // POST: /User/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword("123456");
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Đã reset mật khẩu tài khoản [{0}] về mặc định: [123456]!", user.Username].Value;
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /User/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // POST: /User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user != null)
            {
                if (user.Username == User.Identity?.Name)
                {
                    TempData["ErrorMessage"] = _localizer["Không thể xóa tài khoản của chính bạn đang đăng nhập!"].Value;
                    return RedirectToAction(nameof(Index));
                }

                if (user.Student != null)
                {
                    TempData["ErrorMessage"] = _localizer["Tài khoản [{0}] đang gắn với hồ sơ học viên [{1}]! Vui lòng xóa học viên hoặc hủy liên kết trước khi xóa tài khoản.", user.Username, user.Student.FullName].Value;
                    return RedirectToAction(nameof(Index));
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Đã xóa vĩnh viễn tài khoản [{0}]!", user.Username].Value;
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdownData(int? selectedRoleId = null, int? selectedStudentId = null)
        {
            var roles = await _context.Roles.ToListAsync();
            ViewBag.RoleId = new SelectList(LocalizeRoles(roles), "RoleId", "Name", selectedRoleId);
            // Script trong view so sánh theo RoleId vì tên vai trò hiển thị đã được dịch
            ViewBag.StudentRoleId = roles.FirstOrDefault(r => r.RoleName == "SinhVien")?.RoleId;

            var unlinkedStudents = await _context.Students
                .Where(s => s.UserId == null || (selectedStudentId.HasValue && s.StudentId == selectedStudentId.Value))
                .OrderBy(s => s.FullName)
                .Select(s => new { s.StudentId, DisplayText = $"{s.StudentCode} - {s.FullName} ({s.Email})" })
                .ToListAsync();

            ViewBag.UnlinkedStudents = new SelectList(unlinkedStudents, "StudentId", "DisplayText", selectedStudentId);
        }

        private IEnumerable<object> LocalizeRoles(IEnumerable<Role> roles) =>
            roles.Select(r => new { r.RoleId, Name = _localizer[DisplayLabels.Role(r.RoleName)].Value });
    }
}
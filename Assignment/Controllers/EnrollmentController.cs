using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment.Models.Common;
using Assignment.Models.Data;
using Assignment.Models.Entities;

namespace Assignment.Controllers
{
    [Authorize(Roles = "Admin,GiaoVu")]
    public class EnrollmentController : Controller
    {
        private readonly EnglishCenterDbContext _context;

        public EnrollmentController(EnglishCenterDbContext context)
        {
            _context = context;
        }

        // GET: /Enrollment?search=abc&classId=1&paymentStatus=ConNo&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? classId, string? paymentStatus, int? page)
        {
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentClassId"] = classId;
            ViewData["CurrentPaymentStatus"] = paymentStatus;

            ViewBag.ClassList = new SelectList(
                await _context.Classes.OrderByDescending(c => c.ClassId).ToListAsync(),
                "ClassId",
                "ClassName",
                classId
            );

            var query = _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(e => e.Student.FullName.Contains(search)
                                      || e.Student.StudentCode.Contains(search)
                                      || e.Class.ClassCode.Contains(search)
                                      || e.Class.ClassName.Contains(search));
            }

            if (classId.HasValue && classId.Value > 0)
            {
                query = query.Where(e => e.ClassId == classId.Value);
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(e => e.PaymentStatus == paymentStatus);
            }

            query = query.OrderByDescending(e => e.EnrollmentId);

            int pageSize = 5;
            int currentPage = page ?? 1;
            var pagedEnrollments = await PagedList<Enrollment>.CreateAsync(query, currentPage, pageSize);

            return View(pagedEnrollments);
        }

        // GET: /Enrollment/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .Include(e => e.Attendances)
                .Include(e => e.Grades)
                .FirstOrDefaultAsync(m => m.EnrollmentId == id);

            if (enrollment == null) return NotFound();

            return View(enrollment);
        }

        // GET: /Enrollment/Create
        [HttpGet]
        public async Task<IActionResult> Create(int? classId, int? studentId)
        {
            await PopulateDropDowns(classId, studentId);

            var enrollment = new Enrollment
            {
                EnrollmentDate = DateTime.Now,
                PaymentStatus = "ConNo",
                LearningStatus = "DangHoc",
                PaidAmount = 0
            };

            // Giá học phí của lớp học được chọn
            if (classId.HasValue)
            {
                enrollment.ClassId = classId.Value;
                var cls = await _context.Classes.Include(c => c.Course).FirstOrDefaultAsync(c => c.ClassId == classId.Value);
                if (cls?.Course != null)
                {
                    enrollment.ActualFee = cls.Course.BaseTuitionFee;
                }
            }

            if (studentId.HasValue)
            {
                enrollment.StudentId = studentId.Value;
            }

            return View(enrollment);
        }

        // POST: /Enrollment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Enrollment enrollment)
        {
            bool isAlreadyEnrolled = await _context.Enrollments.AnyAsync(e =>
                e.StudentId == enrollment.StudentId && e.ClassId == enrollment.ClassId);

            if (isAlreadyEnrolled)
            {
                ModelState.AddModelError("", "Học viên này đã được ghi danh vào lớp học được chọn!");
            }

            var targetClass = await _context.Classes
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.ClassId == enrollment.ClassId);

            if (targetClass != null && (targetClass.Enrollments?.Count ?? 0) >= targetClass.MaxCapacity)
            {
                ModelState.AddModelError("ClassId", $"Lớp học đã đủ sĩ số tối đa ({targetClass.MaxCapacity} học viên). Không thể thêm mới!");
            }

            if (enrollment.PaidAmount >= enrollment.ActualFee && enrollment.ActualFee > 0)
            {
                enrollment.PaymentStatus = "DaNopDu";
            }
            else
            {
                enrollment.PaymentStatus = "ConNo";
            }

            ModelState.Remove(nameof(enrollment.Student));
            ModelState.Remove(nameof(enrollment.Class));
            ModelState.Remove(nameof(enrollment.Attendances));
            ModelState.Remove(nameof(enrollment.Grades));

            if (ModelState.IsValid)
            {
                _context.Enrollments.Add(enrollment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ghi danh học viên vào lớp thành công!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropDowns(enrollment.ClassId, enrollment.StudentId);
            return View(enrollment);
        }

        // GET: /Enrollment/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment == null) return NotFound();

            await PopulateDropDowns(enrollment.ClassId, enrollment.StudentId);
            return View(enrollment);
        }

        // POST: /Enrollment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Enrollment enrollment)
        {
            if (id != enrollment.EnrollmentId) return NotFound();

            // Kiểm tra nếu đổi sang lớp khác mà sinh viên đã tồn tại
            bool isDuplicate = await _context.Enrollments.AnyAsync(e =>
                e.EnrollmentId != id &&
                e.StudentId == enrollment.StudentId &&
                e.ClassId == enrollment.ClassId);

            if (isDuplicate)
            {
                ModelState.AddModelError("", "Học viên này đã tồn tại trong lớp đã chọn!");
            }

            // Tự động chuẩn hóa trạng thái học phí
            if (enrollment.PaidAmount >= enrollment.ActualFee && enrollment.ActualFee > 0)
            {
                enrollment.PaymentStatus = "DaNopDu";
            }
            else
            {
                enrollment.PaymentStatus = "ConNo";
            }

            ModelState.Remove(nameof(enrollment.Student));
            ModelState.Remove(nameof(enrollment.Class));
            ModelState.Remove(nameof(enrollment.Attendances));
            ModelState.Remove(nameof(enrollment.Grades));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Enrollments.Update(enrollment);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật thông tin ghi danh thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EnrollmentExists(enrollment.EnrollmentId)) return NotFound();
                    throw;
                }
            }

            await PopulateDropDowns(enrollment.ClassId, enrollment.StudentId);
            return View(enrollment);
        }

        // GET: /Enrollment/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var enrollment = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                    .ThenInclude(c => c.Course)
                .FirstOrDefaultAsync(m => m.EnrollmentId == id);

            if (enrollment == null) return NotFound();

            return View(enrollment);
        }

        // POST: /Enrollment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var enrollment = await _context.Enrollments
                .Include(e => e.Attendances)
                .Include(e => e.Grades)
                .FirstOrDefaultAsync(e => e.EnrollmentId == id);

            if (enrollment != null)
            {
                if ((enrollment.Attendances != null && enrollment.Attendances.Any()) ||
                    (enrollment.Grades != null && enrollment.Grades.Any()))
                {
                    TempData["ErrorMessage"] = "Không thể hủy ghi danh vì học viên này đã có dữ liệu điểm danh hoặc bảng điểm!";
                    return RedirectToAction(nameof(Index));
                }

                _context.Enrollments.Remove(enrollment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã hủy ghi danh học viên khỏi lớp thành công!";
            }

            return RedirectToAction(nameof(Index));
        }

        // Lấy học phí gốc của lớp học qua JSON khi thay đổi dropdown
        [HttpGet]
        public async Task<IActionResult> GetClassDefaultFee(int classId)
        {
            var target = await _context.Classes
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (target?.Course != null)
            {
                return Json(new { success = true, fee = target.Course.BaseTuitionFee });
            }
            return Json(new { success = false, fee = 0 });
        }

        private bool EnrollmentExists(int id)
        {
            return _context.Enrollments.Any(e => e.EnrollmentId == id);
        }

        private async Task PopulateDropDowns(object? selectedClass = null, object? selectedStudent = null)
        {
            var classes = await _context.Classes
                .Include(c => c.Course)
                .OrderByDescending(c => c.ClassId)
                .Select(c => new {
                    c.ClassId,
                    Display = c.ClassCode + " - " + c.ClassName + " (" + (c.Course != null ? c.Course.CourseName : "") + ")"
                })
                .ToListAsync();

            var students = await _context.Students
                .OrderBy(s => s.FullName)
                .Select(s => new {
                    s.StudentId,
                    Display = s.StudentCode + " - " + s.FullName + " (" + s.PhoneNumber + ")"
                })
                .ToListAsync();

            ViewBag.ClassId = new SelectList(classes, "ClassId", "Display", selectedClass);
            ViewBag.StudentId = new SelectList(students, "StudentId", "Display", selectedStudent);
        }
    }
}
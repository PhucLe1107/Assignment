using Assignment.Models;
using Assignment.Models.Common;
using Assignment.Models.Data;
using Assignment.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace Assignment.Controllers
{
    [Authorize(Roles = "Admin,GiaoVu")]
    public class AttendanceController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public AttendanceController(EnglishCenterDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: /Attendance?search=...&classId=1&sessionNumber=1&date=...&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? classId, int? sessionNumber, DateTime? date, int? page)
        {
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentClassId"] = classId;
            ViewData["CurrentSession"] = sessionNumber;
            ViewData["CurrentDate"] = date?.ToString("yyyy-MM-dd");

            ViewBag.ClassList = new SelectList(
                await _context.Classes.OrderByDescending(c => c.ClassId).ToListAsync(),
                "ClassId",
                "ClassName",
                classId
            );

            var query = _context.Attendances
                .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Class)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(a => a.Enrollment.Student.FullName.Contains(search)
                                      || a.Enrollment.Student.StudentCode.Contains(search)
                                      || a.Enrollment.Class.ClassCode.Contains(search));
            }

            if (classId.HasValue && classId.Value > 0)
            {
                query = query.Where(a => a.Enrollment.ClassId == classId.Value);
            }

            if (sessionNumber.HasValue && sessionNumber.Value > 0)
            {
                query = query.Where(a => a.SessionNumber == sessionNumber.Value);
            }

            if (date.HasValue)
            {
                query = query.Where(a => a.AttendanceDate.Date == date.Value.Date);
            }

            query = query.OrderByDescending(a => a.AttendanceDate)
                         .ThenByDescending(a => a.SessionNumber);

            int pageSize = 10;
            int currentPage = page ?? 1;
            var pagedData = await PagedList<Attendance>.CreateAsync(query, currentPage, pageSize);

            return View(pagedData);
        }

        // GET: /Attendance/TakeAttendance?classId=1&sessionNumber=1&attendanceDate=2026-09-22
        [HttpGet]
        public async Task<IActionResult> TakeAttendance(int? classId, int? sessionNumber, DateTime? attendanceDate)
        {
            var classes = await _context.Classes.OrderByDescending(c => c.ClassId).ToListAsync();
            ViewBag.ClassId = new SelectList(classes, "ClassId", "ClassName", classId);

            var model = new AttendanceSheetViewModel
            {
                SessionNumber = sessionNumber ?? 1,
                AttendanceDate = attendanceDate ?? DateTime.Today
            };

            if (classId.HasValue && classId.Value > 0)
            {
                var targetClass = await _context.Classes
                    .Include(c => c.Course)
                    .Include(c => c.Enrollments)
                        .ThenInclude(e => e.Student)
                    .FirstOrDefaultAsync(c => c.ClassId == classId.Value);

                if (targetClass != null)
                {
                    model.ClassId = targetClass.ClassId;
                    model.ClassName = targetClass.ClassName;
                    model.ClassCode = targetClass.ClassCode;
                    model.TotalSessions = targetClass.Course?.TotalSessions ?? 0;

                    // Lấy học viên đang học trong lớp
                    var activeEnrollments = targetClass.Enrollments
                        .Where(e => e.LearningStatus == "DangHoc")
                        .OrderBy(e => e.Student.FullName)
                        .ToList();

                    // Kiểm tra buổi học đã được điểm danh trước đó chưa
                    var existingAttendances = await _context.Attendances
                        .Where(a => a.Enrollment.ClassId == targetClass.ClassId && a.SessionNumber == model.SessionNumber)
                        .ToDictionaryAsync(a => a.EnrollmentId);

                    foreach (var en in activeEnrollments)
                    {
                        if (existingAttendances.TryGetValue(en.EnrollmentId, out var existing))
                        {
                            model.Students.Add(new AttendanceStudentItem
                            {
                                EnrollmentId = en.EnrollmentId,
                                AttendanceId = existing.AttendanceId,
                                StudentCode = en.Student.StudentCode,
                                FullName = en.Student.FullName,
                                PhoneNumber = en.Student.PhoneNumber,
                                IsPresent = existing.IsPresent,
                                Note = existing.Note
                            });
                            model.AttendanceDate = existing.AttendanceDate;
                        }
                        else
                        {
                            model.Students.Add(new AttendanceStudentItem
                            {
                                EnrollmentId = en.EnrollmentId,
                                AttendanceId = null,
                                StudentCode = en.Student.StudentCode,
                                FullName = en.Student.FullName,
                                PhoneNumber = en.Student.PhoneNumber,
                                IsPresent = true,
                                Note = string.Empty
                            });
                        }
                    }
                }
            }

            return View(model);
        }

        // POST: /Attendance/SaveAttendance
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveAttendance(AttendanceSheetViewModel model)
        {
            if (model.ClassId <= 0)
            {
                ModelState.AddModelError("ClassId", _localizer["Vui lòng chọn lớp học!"]);
            }

            if (ModelState.IsValid)
            {
                foreach (var item in model.Students)
                {
                    string safeNote = item.Note ?? "";

                    if (item.AttendanceId.HasValue && item.AttendanceId.Value > 0)
                    {
                        var existing = await _context.Attendances.FindAsync(item.AttendanceId.Value);
                        if (existing != null)
                        {
                            existing.SessionNumber = model.SessionNumber;
                            existing.AttendanceDate = model.AttendanceDate;
                            existing.IsPresent = item.IsPresent;
                            existing.Note = safeNote; // Dùng safeNote
                            _context.Attendances.Update(existing);
                        }
                    }
                    else
                    {
                        var newAtt = new Attendance
                        {
                            EnrollmentId = item.EnrollmentId,
                            SessionNumber = model.SessionNumber,
                            AttendanceDate = model.AttendanceDate,
                            IsPresent = item.IsPresent,
                            Note = safeNote // Dùng safeNote
                        };
                        _context.Attendances.Add(newAtt);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Lưu điểm danh Buổi {0} thành công!", model.SessionNumber].Value;
                return RedirectToAction(nameof(TakeAttendance), new { classId = model.ClassId, sessionNumber = model.SessionNumber });
            }

            var classes = await _context.Classes.OrderByDescending(c => c.ClassId).ToListAsync();
            ViewBag.ClassId = new SelectList(classes, "ClassId", "ClassName", model.ClassId);
            return View("TakeAttendance", model);
        }

        // GET: /Attendance/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var attendance = await _context.Attendances
                .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(a => a.Enrollment)
                    .ThenInclude(e => e.Class)
                .FirstOrDefaultAsync(m => m.AttendanceId == id);

            if (attendance == null) return NotFound();

            return View(attendance);
        }

        // POST: /Attendance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var att = await _context.Attendances.FindAsync(id);
            if (att != null)
            {
                _context.Attendances.Remove(att);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Đã xóa bản ghi điểm danh thành công!"].Value;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}

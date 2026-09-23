using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Assignment.Models;
using Assignment.Models.Common;
using Assignment.Models.Data;
using Assignment.Localization;
using Assignment.Models.Entities;
using Microsoft.Extensions.Localization;

namespace Assignment.Controllers
{
    [Authorize(Roles = "Admin,GiaoVu")]
    public class GradeController : Controller
    {
        private readonly EnglishCenterDbContext _context;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public GradeController(EnglishCenterDbContext context, IStringLocalizer<SharedResource> localizer)
        {
            _context = context;
            _localizer = localizer;
        }

        // GET: /Grade?search=...&classId=1&examType=GiuaKy&page=1
        [HttpGet]
        public async Task<IActionResult> Index(string? search, int? classId, string? examType, int? page)
        {
            ViewData["CurrentSearch"] = search;
            ViewData["CurrentClassId"] = classId;
            ViewData["CurrentExamType"] = examType;

            ViewBag.ClassList = new SelectList(
                await _context.Classes.OrderByDescending(c => c.ClassId).ToListAsync(),
                "ClassId",
                "ClassName",
                classId
            );

            var query = _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Class)
                        .ThenInclude(c => c.Course)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(g => g.Enrollment.Student.FullName.Contains(search)
                                      || g.Enrollment.Student.StudentCode.Contains(search)
                                      || g.Enrollment.Class.ClassCode.Contains(search));
            }

            if (classId.HasValue && classId.Value > 0)
            {
                query = query.Where(g => g.Enrollment.ClassId == classId.Value);
            }

            if (!string.IsNullOrWhiteSpace(examType))
            {
                query = query.Where(g => g.ExamType == examType);
            }

            query = query.OrderByDescending(g => g.GradeId);

            int pageSize = 10;
            int currentPage = page ?? 1;
            var pagedGrades = await PagedList<Grade>.CreateAsync(query, currentPage, pageSize);

            return View(pagedGrades);
        }

        // GET: /Grade/EnterGrades?classId=1&examType=GiuaKy
        [HttpGet]
        public async Task<IActionResult> EnterGrades(int? classId, string? examType)
        {
            var classes = await _context.Classes.OrderByDescending(c => c.ClassId).ToListAsync();
            ViewBag.ClassId = new SelectList(classes, "ClassId", "ClassName", classId);

            var model = new GradeSheetViewModel
            {
                ExamType = string.IsNullOrWhiteSpace(examType) ? "GiuaKy" : examType
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
                    model.CourseName = targetClass.Course?.CourseName ?? "";

                    var activeEnrollments = targetClass.Enrollments
                        .Where(e => e.LearningStatus == "DangHoc")
                        .OrderBy(e => e.Student.FullName)
                        .ToList();

                    var existingGrades = await _context.Grades
                        .Where(g => g.Enrollment.ClassId == targetClass.ClassId && g.ExamType == model.ExamType)
                        .ToDictionaryAsync(g => g.EnrollmentId);

                    foreach (var en in activeEnrollments)
                    {
                        if (existingGrades.TryGetValue(en.EnrollmentId, out var existing))
                        {
                            model.Students.Add(new GradeStudentItem
                            {
                                EnrollmentId = en.EnrollmentId,
                                GradeId = existing.GradeId,
                                StudentCode = en.Student.StudentCode,
                                FullName = en.Student.FullName,
                                ListeningScore = existing.ListeningScore,
                                ReadingScore = existing.ReadingScore,
                                WritingScore = existing.WritingScore,
                                SpeakingScore = existing.SpeakingScore,
                                OverallScore = existing.OverallScore,
                                TeacherFeedback = existing.TeacherFeedback
                            });
                        }
                        else
                        {
                            model.Students.Add(new GradeStudentItem
                            {
                                EnrollmentId = en.EnrollmentId,
                                GradeId = null,
                                StudentCode = en.Student.StudentCode,
                                FullName = en.Student.FullName
                            });
                        }
                    }
                }
            }

            return View(model);
        }

        // POST: /Grade/SaveGrades
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveGrades(GradeSheetViewModel model)
        {
            if (model.ClassId <= 0)
            {
                ModelState.AddModelError("ClassId", _localizer["Vui lòng chọn lớp học!"]);
            }

            if (ModelState.IsValid)
            {
                var currentClass = await _context.Classes
                    .Include(c => c.Course)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ClassId == model.ClassId);

                string courseName = currentClass?.Course?.CourseName ?? "";

                foreach (var item in model.Students)
                {
                    // Tính Overall theo từng hệ chứng chỉ
                    double? overall = CalculateOverall(courseName, model.ExamType, item.ListeningScore, item.ReadingScore, item.WritingScore, item.SpeakingScore);
                    string safeFeedback = item.TeacherFeedback ?? "";

                    bool hasData = item.ListeningScore.HasValue || item.ReadingScore.HasValue ||
                                   item.WritingScore.HasValue || item.SpeakingScore.HasValue ||
                                   !string.IsNullOrWhiteSpace(item.TeacherFeedback);

                    if (item.GradeId.HasValue && item.GradeId.Value > 0)
                    {
                        var existing = await _context.Grades.FindAsync(item.GradeId.Value);
                        if (existing != null)
                        {
                            existing.ExamType = model.ExamType;
                            existing.ListeningScore = item.ListeningScore;
                            existing.ReadingScore = item.ReadingScore;
                            existing.WritingScore = item.WritingScore;
                            existing.SpeakingScore = item.SpeakingScore;
                            existing.OverallScore = overall;
                            existing.TeacherFeedback = safeFeedback;
                            _context.Grades.Update(existing);
                        }
                    }
                    else if (hasData)
                    {
                        var newGrade = new Grade
                        {
                            EnrollmentId = item.EnrollmentId,
                            ExamType = model.ExamType,
                            ListeningScore = item.ListeningScore,
                            ReadingScore = item.ReadingScore,
                            WritingScore = item.WritingScore,
                            SpeakingScore = item.SpeakingScore,
                            OverallScore = overall,
                            TeacherFeedback = safeFeedback
                        };
                        _context.Grades.Add(newGrade);
                    }
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Lưu sổ điểm kỳ [{0}] thành công!", _localizer[DisplayLabels.ExamType(model.ExamType)]].Value;
                return RedirectToAction(nameof(EnterGrades), new { classId = model.ClassId, examType = model.ExamType });
            }

            var classes = await _context.Classes.OrderByDescending(c => c.ClassId).ToListAsync();
            ViewBag.ClassId = new SelectList(classes, "ClassId", "ClassName", model.ClassId);
            return View("EnterGrades", model);
        }

        // GET: /Grade/Delete/5
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var grade = await _context.Grades
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Student)
                .Include(g => g.Enrollment)
                    .ThenInclude(e => e.Class)
                .FirstOrDefaultAsync(m => m.GradeId == id);

            if (grade == null) return NotFound();

            return View(grade);
        }

        // POST: /Grade/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = _localizer["Đã xóa bản ghi điểm thành công!"].Value;
            }
            return RedirectToAction(nameof(Index));
        }

        private double? CalculateOverall(string courseName, string examType, double? l, double? r, double? w, double? s)
        {
            bool isIelts = courseName.Contains("IELTS", StringComparison.OrdinalIgnoreCase) || examType.Contains("IELTS", StringComparison.OrdinalIgnoreCase);
            bool isToeic = courseName.Contains("TOEIC", StringComparison.OrdinalIgnoreCase) || examType.Contains("TOEIC", StringComparison.OrdinalIgnoreCase);

            // Tính TOEIC
            if (isToeic)
            {
                double total = (l ?? 0) + (r ?? 0);
                return (l.HasValue || r.HasValue) ? total : null;
            }

            // Tính IELTS hoặc điểm trung bình khác
            var scores = new List<double>();
            if (l.HasValue) scores.Add(l.Value);
            if (r.HasValue) scores.Add(r.Value);
            if (w.HasValue) scores.Add(w.Value);
            if (s.HasValue) scores.Add(s.Value);

            if (!scores.Any()) return null;

            double avg = scores.Average();

            if (isIelts)
            {
                double floor = Math.Floor(avg);
                double rem = avg - floor;
                if (rem < 0.25) return floor;
                if (rem < 0.75) return floor + 0.5;
                return floor + 1.0;
            }

            return Math.Round(avg, 1);
        }
    }
}

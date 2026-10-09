using Assignment.Models.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Controllers;

[Authorize(Roles = "Admin")]
public class StatisticsController : Controller
{
    [HttpGet("/statistics")]
    public IActionResult Index() => View();
}

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/statistics")]
public class StatisticsApiController : ControllerBase
{
    private readonly EnglishCenterDbContext _db;

    public StatisticsApiController(EnglishCenterDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Get(DateTime? from, DateTime? to, int? courseId, int? classId, CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from.Value.Date > to.Value.Date)
            return BadRequest(new { message = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc." });

        var enrollments = _db.Enrollments.AsNoTracking().AsQueryable();
        if (from.HasValue) enrollments = enrollments.Where(e => e.EnrollmentDate >= from.Value.Date);
        if (to.HasValue) enrollments = enrollments.Where(e => e.EnrollmentDate < to.Value.Date.AddDays(1));
        if (courseId.HasValue) enrollments = enrollments.Where(e => e.Class.CourseId == courseId.Value);
        if (classId.HasValue) enrollments = enrollments.Where(e => e.ClassId == classId.Value);

        var attendance = _db.Attendances.AsNoTracking().AsQueryable();
        if (from.HasValue) attendance = attendance.Where(a => a.AttendanceDate >= from.Value.Date);
        if (to.HasValue) attendance = attendance.Where(a => a.AttendanceDate < to.Value.Date.AddDays(1));
        if (courseId.HasValue) attendance = attendance.Where(a => a.Enrollment.Class.CourseId == courseId.Value);
        if (classId.HasValue) attendance = attendance.Where(a => a.Enrollment.ClassId == classId.Value);

        var studentIds = enrollments.Select(e => e.StudentId).Distinct();
        var studentCount = await studentIds.CountAsync(cancellationToken);
        var courseQuery = _db.Courses.AsNoTracking();
        if (courseId.HasValue) courseQuery = courseQuery.Where(c => c.CourseId == courseId.Value);
        if (classId.HasValue) courseQuery = courseQuery.Where(c => c.Classes!.Any(cl => cl.ClassId == classId.Value));
        var courseCount = await courseQuery.CountAsync(cancellationToken);
        var activeCourseCount = await courseQuery.CountAsync(c => c.IsActive, cancellationToken);
        var classQuery = _db.Classes.AsNoTracking();
        if (courseId.HasValue) classQuery = classQuery.Where(c => c.CourseId == courseId.Value);
        if (classId.HasValue) classQuery = classQuery.Where(c => c.ClassId == classId.Value);
        var classCount = await classQuery.CountAsync(cancellationToken);
        var filterClassQuery = _db.Classes.AsNoTracking();
        if (courseId.HasValue) filterClassQuery = filterClassQuery.Where(c => c.CourseId == courseId.Value);
        var enrollmentCount = await enrollments.CountAsync(cancellationToken);
        var activeEnrollmentCount = await enrollments.CountAsync(e => e.LearningStatus == "DangHoc", cancellationToken);

        var feeTotals = await enrollments
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Expected = g.Sum(e => e.ActualFee),
                Paid = g.Sum(e => e.PaidAmount)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var attendanceTotals = await attendance
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Present = g.Count(a => a.IsPresent) })
            .FirstOrDefaultAsync(cancellationToken);

        var averageScore = await _db.Grades
            .Where(g => enrollments.Select(e => e.EnrollmentId).Contains(g.EnrollmentId))
            .Where(g => g.OverallScore.HasValue)
            .Select(g => (double?)g.OverallScore)
            .AverageAsync(cancellationToken);

        var learningStatuses = await enrollments
            .GroupBy(e => e.LearningStatus)
            .Select(g => new { status = g.Key, count = g.Count() })
            .OrderBy(x => x.status)
            .ToListAsync(cancellationToken);

        var paymentStatuses = await enrollments
            .GroupBy(e => e.PaymentStatus)
            .Select(g => new { status = g.Key, count = g.Count() })
            .OrderBy(x => x.status)
            .ToListAsync(cancellationToken);

        var studentsByGender = await _db.Students.AsNoTracking().Where(s => studentIds.Contains(s.StudentId))
            .GroupBy(s => s.Gender)
            .Select(g => new { label = g.Key ? "Nam" : "Nữ", count = g.Count() })
            .OrderBy(x => x.label)
            .ToListAsync(cancellationToken);

        var enrollmentsByCourse = await enrollments
            .GroupBy(e => e.Class.Course.CourseName)
            .Select(g => new { course = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToListAsync(cancellationToken);

        return Ok(new
        {
            summary = new
            {
                studentCount,
                courseCount,
                activeCourseCount,
                classCount,
                enrollmentCount,
                activeEnrollmentCount,
                expectedTuition = feeTotals?.Expected ?? 0,
                collectedTuition = feeTotals?.Paid ?? 0,
                outstandingTuition = Math.Max(0, (feeTotals?.Expected ?? 0) - (feeTotals?.Paid ?? 0)),
                attendanceCount = attendanceTotals?.Total ?? 0,
                presentCount = attendanceTotals?.Present ?? 0,
                attendanceRate = attendanceTotals is { Total: > 0 }
                    ? Math.Round((double)attendanceTotals.Present / attendanceTotals.Total * 100, 1)
                    : 0,
                averageScore = averageScore.HasValue ? Math.Round(averageScore.Value, 2) : (double?)null
            },
            learningStatuses,
            paymentStatuses,
            studentsByGender,
            enrollmentsByCourse,
            filters = new
            {
                courses = await _db.Courses.AsNoTracking().OrderBy(c => c.CourseName)
                    .Select(c => new { id = c.CourseId, name = c.CourseName }).ToListAsync(cancellationToken),
                classes = await filterClassQuery.OrderBy(c => c.ClassName)
                    .Select(c => new { id = c.ClassId, name = c.ClassName, courseId = c.CourseId }).ToListAsync(cancellationToken)
            }
        });
    }
}

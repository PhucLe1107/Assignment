using Assignment.Controllers;
using Assignment.Models;
using Assignment.Models.Entities;
using Assignment.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests.Controllers;

public class GradeControllerTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly SeedData _seed;

    public GradeControllerTests() => _seed = _db.Seed();

    public void Dispose() => _db.Dispose();

    private GradeController CreateController() =>
        new GradeController(_db.CreateContext(), new PassThroughLocalizer<SharedResource>()).WithContext("admin");

    private async Task<Grade> SaveSingleGradeAsync(int classId, int enrollmentId, string examType,
        double? listening, double? reading, double? writing, double? speaking)
    {
        await CreateController().SaveGrades(new GradeSheetViewModel
        {
            ClassId = classId,
            ExamType = examType,
            Students =
            [
                new GradeStudentItem
                {
                    EnrollmentId = enrollmentId,
                    ListeningScore = listening,
                    ReadingScore = reading,
                    WritingScore = writing,
                    SpeakingScore = speaking
                }
            ]
        });

        using var context = _db.CreateContext();
        return await context.Grades.SingleAsync(g => g.EnrollmentId == enrollmentId && g.ExamType == examType);
    }

    // IELTS: trung bình 4 kỹ năng, làm tròn tới 0.5 gần nhất (.25 lên .5, .75 lên 1.0)
    [Theory]
    [InlineData(6.0, 6.0, 6.0, 6.5, 6.0)]   // TB 6.125
    [InlineData(6.0, 6.5, 6.5, 6.5, 6.5)]   // TB 6.375
    [InlineData(6.5, 6.5, 7.0, 7.0, 7.0)]   // TB 6.75
    [InlineData(5.0, 5.0, 5.5, 5.5, 5.5)]   // TB 5.25
    [InlineData(7.0, 7.0, 7.0, 7.0, 7.0)]
    public async Task SaveGrades_IeltsCourse_RoundsOverallToNearestHalfBand(
        double l, double r, double w, double s, double expectedOverall)
    {
        var grade = await SaveSingleGradeAsync(_seed.IeltsClassId, _seed.AnIeltsEnrollmentId, "GiuaKy", l, r, w, s);

        Assert.Equal(expectedOverall, grade.OverallScore);
    }

    [Fact]
    public async Task SaveGrades_ToeicCourse_OverallIsListeningPlusReading()
    {
        var grade = await SaveSingleGradeAsync(_seed.ToeicClassId, _seed.AnToeicEnrollmentId, "CuoiKy", 400, 350, 150, 160);

        Assert.Equal(750, grade.OverallScore);
    }

    [Fact]
    public async Task SaveGrades_OtherCourse_OverallIsAverageOfEnteredSkillsRoundedToOneDecimal()
    {
        int classId, enrollmentId;
        using (var context = _db.CreateContext())
        {
            var course = new Course { CourseName = "Giao tiếp", BaseTuitionFee = 1, TotalSessions = 10, DescriptionHtml = "x" };
            var cls = TestDatabase.NewClass("GT-01", course);
            var enrollment = new Enrollment { StudentId = _seed.BinhId, Class = cls, ActualFee = 1 };
            context.Enrollments.Add(enrollment);
            await context.SaveChangesAsync();
            (classId, enrollmentId) = (cls.ClassId, enrollment.EnrollmentId);
        }

        // Chỉ nhập 3 kỹ năng: (7 + 8 + 8) / 3 = 7.666... -> 7.7
        var grade = await SaveSingleGradeAsync(classId, enrollmentId, "GiuaKy", 7, 8, 8, null);

        Assert.Equal(7.7, grade.OverallScore);
    }

    [Fact]
    public async Task SaveGrades_RowWithoutAnyData_IsNotSaved()
    {
        await CreateController().SaveGrades(new GradeSheetViewModel
        {
            ClassId = _seed.IeltsClassId,
            ExamType = "GiuaKy",
            Students = [new GradeStudentItem { EnrollmentId = _seed.AnIeltsEnrollmentId, TeacherFeedback = "  " }]
        });

        using var context = _db.CreateContext();
        Assert.False(await context.Grades.AnyAsync());
    }

    [Fact]
    public async Task SaveGrades_ExistingGrade_IsUpdatedInsteadOfDuplicated()
    {
        var first = await SaveSingleGradeAsync(_seed.IeltsClassId, _seed.AnIeltsEnrollmentId, "GiuaKy", 5, 5, 5, 5);

        await CreateController().SaveGrades(new GradeSheetViewModel
        {
            ClassId = _seed.IeltsClassId,
            ExamType = "GiuaKy",
            Students =
            [
                new GradeStudentItem
                {
                    EnrollmentId = _seed.AnIeltsEnrollmentId,
                    GradeId = first.GradeId,
                    ListeningScore = 8, ReadingScore = 8, WritingScore = 8, SpeakingScore = 8
                }
            ]
        });

        using var context = _db.CreateContext();
        var grade = await context.Grades.SingleAsync();
        Assert.Equal(first.GradeId, grade.GradeId);
        Assert.Equal(8, grade.OverallScore);
    }

    [Fact]
    public async Task SaveGrades_WithoutClass_ReturnsEnterGradesViewWithError()
    {
        var controller = CreateController();

        var result = await controller.SaveGrades(new GradeSheetViewModel { ClassId = 0 });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("EnterGrades", view.ViewName);
        Assert.False(controller.ModelState.IsValid);
    }
}

using Assignment.Controllers;
using Assignment.Models.Entities;
using Assignment.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests.Controllers;

public class CourseControllerTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly SeedData _seed;

    public CourseControllerTests() => _seed = _db.Seed();

    public void Dispose() => _db.Dispose();

    private CourseController CreateController() =>
        new CourseController(_db.CreateContext(), new FakeWebHostEnvironment(), new PassThroughLocalizer<SharedResource>())
            .WithContext("admin");

    [Fact]
    public async Task Create_WithoutThumbnail_UsesDefaultImage()
    {
        await CreateController().Create(new Course
        {
            CourseName = "Giao tiếp cơ bản",
            BaseTuitionFee = 3_000_000,
            TotalSessions = 20,
            DescriptionHtml = "<p>...</p>"
        });

        using var context = _db.CreateContext();
        var course = await context.Courses.SingleAsync(c => c.CourseName == "Giao tiếp cơ bản");
        Assert.Equal("/img/default-course.jpg", course.ThumbnailUrl);
    }

    [Fact]
    public async Task Delete_CourseWithClasses_IsBlocked()
    {
        var controller = CreateController();

        await controller.DeleteConfirmed(_seed.IeltsCourseId);

        Assert.Equal("Không thể xóa vì khóa học đã có các lớp học liên kết!", controller.TempData["ErrorMessage"]);
        using var context = _db.CreateContext();
        Assert.True(await context.Courses.AnyAsync(c => c.CourseId == _seed.IeltsCourseId));
    }

    [Fact]
    public async Task Delete_CourseWithoutClasses_RemovesIt()
    {
        int courseId;
        using (var context = _db.CreateContext())
        {
            var course = new Course { CourseName = "Tạm", BaseTuitionFee = 1, TotalSessions = 1, DescriptionHtml = "x" };
            context.Courses.Add(course);
            await context.SaveChangesAsync();
            courseId = course.CourseId;
        }

        var controller = CreateController();
        await controller.DeleteConfirmed(courseId);

        Assert.Equal("Đã xóa khóa học thành công!", controller.TempData["SuccessMessage"]);
        using var verify = _db.CreateContext();
        Assert.False(await verify.Courses.AnyAsync(c => c.CourseId == courseId));
    }
}

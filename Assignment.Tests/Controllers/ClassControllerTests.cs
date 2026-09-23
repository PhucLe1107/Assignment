using Assignment.Controllers;
using Assignment.Models.Entities;
using Assignment.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests.Controllers;

public class ClassControllerTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly SeedData _seed;

    public ClassControllerTests() => _seed = _db.Seed();

    public void Dispose() => _db.Dispose();

    private ClassController CreateController() =>
        new ClassController(_db.CreateContext(), new PassThroughLocalizer<SharedResource>()).WithContext("admin");

    private Class NewClass(string code) => new()
    {
        ClassCode = code,
        ClassName = code,
        CourseId = _seed.IeltsCourseId,
        Schedule = "3-5-7",
        StartDate = new DateTime(2026, 11, 1)
    };

    [Fact]
    public async Task Create_ValidClass_Saves()
    {
        var result = await CreateController().Create(NewClass("IELTS-02"));

        Assert.IsType<RedirectToActionResult>(result);
        using var context = _db.CreateContext();
        Assert.True(await context.Classes.AnyAsync(c => c.ClassCode == "IELTS-02"));
    }

    [Fact]
    public async Task Create_DuplicateClassCode_ReturnsViewWithError()
    {
        var controller = CreateController();

        var result = await controller.Create(NewClass("IELTS-01"));

        Assert.IsType<ViewResult>(result);
        Assert.Equal("Mã lớp này đã tồn tại trên hệ thống.", controller.ModelState["ClassCode"]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Delete_ClassWithEnrollments_IsBlocked()
    {
        var controller = CreateController();

        await controller.DeleteConfirmed(_seed.IeltsClassId);

        Assert.Equal("Không thể xóa lớp học vì đã có học viên đăng ký!", controller.TempData["ErrorMessage"]);
        using var context = _db.CreateContext();
        Assert.True(await context.Classes.AnyAsync(c => c.ClassId == _seed.IeltsClassId));
    }

    [Fact]
    public async Task Delete_EmptyClass_RemovesIt()
    {
        await CreateController().Create(NewClass("EMPTY-01"));
        int emptyClassId;
        using (var context = _db.CreateContext())
        {
            emptyClassId = (await context.Classes.SingleAsync(c => c.ClassCode == "EMPTY-01")).ClassId;
        }

        var controller = CreateController();
        await controller.DeleteConfirmed(emptyClassId);

        Assert.Equal("Đã xóa lớp học thành công!", controller.TempData["SuccessMessage"]);
        using var verify = _db.CreateContext();
        Assert.False(await verify.Classes.AnyAsync(c => c.ClassId == emptyClassId));
    }
}

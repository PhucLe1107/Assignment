using Assignment.Controllers;
using Assignment.Models.Entities;
using Assignment.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests.Controllers;

public class EnrollmentControllerTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly SeedData _seed;

    public EnrollmentControllerTests() => _seed = _db.Seed();

    public void Dispose() => _db.Dispose();

    private EnrollmentController CreateController() =>
        new EnrollmentController(_db.CreateContext(), new PassThroughLocalizer<SharedResource>()).WithContext("admin");

    [Fact]
    public async Task Create_ValidEnrollment_SavesAndRedirectsToIndex()
    {
        var result = await CreateController().Create(new Enrollment
        {
            StudentId = _seed.BinhId,
            ClassId = _seed.ToeicClassId,
            ActualFee = 4_000_000
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        using var context = _db.CreateContext();
        Assert.True(await context.Enrollments.AnyAsync(e => e.StudentId == _seed.BinhId && e.ClassId == _seed.ToeicClassId));
    }

    [Fact]
    public async Task Create_StudentAlreadyInClass_ReturnsViewWithError()
    {
        var controller = CreateController();

        var result = await controller.Create(new Enrollment { StudentId = _seed.AnId, ClassId = _seed.IeltsClassId, ActualFee = 1 });

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage == "Học viên này đã được ghi danh vào lớp học được chọn!");
    }

    [Fact]
    public async Task Create_ClassAtMaxCapacity_ReturnsViewWithError()
    {
        // Lớp IELTS có sĩ số tối đa 2, An đã học; thêm Bình là đủ lớp
        await CreateController().Create(new Enrollment { StudentId = _seed.BinhId, ClassId = _seed.IeltsClassId, ActualFee = 1 });

        var controller = CreateController();
        var result = await controller.Create(new Enrollment { StudentId = _seed.CuongId, ClassId = _seed.IeltsClassId, ActualFee = 1 });

        Assert.IsType<ViewResult>(result);
        Assert.Contains("sĩ số tối đa (2 học viên)", controller.ModelState["ClassId"]!.Errors.Single().ErrorMessage);
        using var context = _db.CreateContext();
        Assert.Equal(2, await context.Enrollments.CountAsync(e => e.ClassId == _seed.IeltsClassId));
    }

    [Theory]
    [InlineData(4_000_000, 4_000_000, "DaNopDu")]
    [InlineData(4_000_000, 5_000_000, "DaNopDu")]
    [InlineData(4_000_000, 3_999_999, "ConNo")]
    [InlineData(0, 0, "ConNo")]
    public async Task Create_SetsPaymentStatusFromAmounts(decimal actualFee, decimal paidAmount, string expectedStatus)
    {
        await CreateController().Create(new Enrollment
        {
            StudentId = _seed.CuongId,
            ClassId = _seed.ToeicClassId,
            ActualFee = actualFee,
            PaidAmount = paidAmount,
            PaymentStatus = "ignored"
        });

        using var context = _db.CreateContext();
        var saved = await context.Enrollments.SingleAsync(e => e.StudentId == _seed.CuongId);
        Assert.Equal(expectedStatus, saved.PaymentStatus);
    }

    [Fact]
    public async Task Edit_MoveToClassStudentIsAlreadyIn_ReturnsViewWithError()
    {
        var controller = CreateController();
        var enrollment = new Enrollment
        {
            EnrollmentId = _seed.AnToeicEnrollmentId,
            StudentId = _seed.AnId,
            ClassId = _seed.IeltsClassId,
            ActualFee = 4_000_000
        };

        var result = await controller.Edit(_seed.AnToeicEnrollmentId, enrollment);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(controller.ModelState[string.Empty]!.Errors,
            e => e.ErrorMessage == "Học viên này đã tồn tại trong lớp đã chọn!");
    }

    [Fact]
    public async Task Edit_IdMismatch_ReturnsNotFound()
    {
        var result = await CreateController().Edit(999, new Enrollment { EnrollmentId = 1 });

        Assert.IsType<NotFoundResult>(result);
    }
}

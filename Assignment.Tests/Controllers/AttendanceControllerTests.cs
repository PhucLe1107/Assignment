using Assignment.Controllers;
using Assignment.Models;
using Assignment.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests.Controllers;

public class AttendanceControllerTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly SeedData _seed;

    public AttendanceControllerTests() => _seed = _db.Seed();

    public void Dispose() => _db.Dispose();

    private AttendanceController CreateController() =>
        new AttendanceController(_db.CreateContext(), new PassThroughLocalizer<SharedResource>()).WithContext("admin");

    [Fact]
    public async Task SaveAttendance_NewSession_CreatesRecordsAndRedirectsBackToSession()
    {
        var result = await CreateController().SaveAttendance(new AttendanceSheetViewModel
        {
            ClassId = _seed.IeltsClassId,
            SessionNumber = 3,
            AttendanceDate = new DateTime(2026, 10, 7),
            Students = [new AttendanceStudentItem { EnrollmentId = _seed.AnIeltsEnrollmentId, IsPresent = false, Note = null }]
        });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("TakeAttendance", redirect.ActionName);
        Assert.Equal(3, redirect.RouteValues!["sessionNumber"]);

        using var context = _db.CreateContext();
        var saved = await context.Attendances.SingleAsync();
        Assert.False(saved.IsPresent);
        Assert.Equal(3, saved.SessionNumber);
        Assert.Equal(string.Empty, saved.Note); // Note null được lưu thành chuỗi rỗng (cột NOT NULL)
    }

    [Fact]
    public async Task SaveAttendance_ExistingRecord_IsUpdated()
    {
        await CreateController().SaveAttendance(new AttendanceSheetViewModel
        {
            ClassId = _seed.IeltsClassId,
            SessionNumber = 1,
            Students = [new AttendanceStudentItem { EnrollmentId = _seed.AnIeltsEnrollmentId, IsPresent = true }]
        });
        int attendanceId;
        using (var context = _db.CreateContext())
        {
            attendanceId = (await context.Attendances.SingleAsync()).AttendanceId;
        }

        await CreateController().SaveAttendance(new AttendanceSheetViewModel
        {
            ClassId = _seed.IeltsClassId,
            SessionNumber = 1,
            Students =
            [
                new AttendanceStudentItem
                {
                    EnrollmentId = _seed.AnIeltsEnrollmentId, AttendanceId = attendanceId, IsPresent = false, Note = "Ốm"
                }
            ]
        });

        using var verify = _db.CreateContext();
        var saved = await verify.Attendances.SingleAsync();
        Assert.False(saved.IsPresent);
        Assert.Equal("Ốm", saved.Note);
    }

    [Fact]
    public async Task SaveAttendance_WithoutClass_ReturnsViewWithError()
    {
        var controller = CreateController();

        var result = await controller.SaveAttendance(new AttendanceSheetViewModel { ClassId = 0 });

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("TakeAttendance", view.ViewName);
        Assert.Equal("Vui lòng chọn lớp học!", controller.ModelState["ClassId"]!.Errors.Single().ErrorMessage);
    }
}

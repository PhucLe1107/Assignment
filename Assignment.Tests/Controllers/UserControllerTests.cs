using Assignment.Controllers;
using Assignment.Models;
using Assignment.Models.Entities;
using Assignment.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests.Controllers;

public class UserControllerTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly SeedData _seed;
    private readonly int _adminUserId;
    private readonly int _linkedStudentUserId;

    public UserControllerTests()
    {
        _seed = _db.Seed();
        using var context = _db.CreateContext();
        var admin = new User { Username = "admin", Password = BCrypt.Net.BCrypt.HashPassword("admin@123"), RoleId = _seed.AdminRoleId };
        var anAccount = new User { Username = "sv001", Password = "x", RoleId = _seed.SinhVienRoleId };
        context.Users.AddRange(admin, anAccount);
        context.SaveChanges();
        context.Students.Find(_seed.AnId)!.UserId = anAccount.UserId;
        context.SaveChanges();
        (_adminUserId, _linkedStudentUserId) = (admin.UserId, anAccount.UserId);
    }

    public void Dispose() => _db.Dispose();

    // Người đang đăng nhập là "admin"
    private UserController CreateController() =>
        new UserController(_db.CreateContext(), new PassThroughLocalizer<SharedResource>()).WithContext("admin");

    [Fact]
    public async Task Create_StaffAccount_HashesPasswordAndIgnoresStudentLink()
    {
        var result = await CreateController().Create(new UserCreateViewModel
        {
            Username = " giaovu02 ",
            Password = "secret123",
            ConfirmPassword = "secret123",
            RoleId = _seed.GiaoVuRoleId,
            StudentId = _seed.BinhId
        });

        Assert.IsType<RedirectToActionResult>(result);
        using var context = _db.CreateContext();
        var user = await context.Users.SingleAsync(u => u.Username == "giaovu02"); // đã trim khoảng trắng
        Assert.True(BCrypt.Net.BCrypt.Verify("secret123", user.Password));
        Assert.Null((await context.Students.FindAsync(_seed.BinhId))!.UserId);
    }

    [Fact]
    public async Task Create_StudentAccount_LinksSelectedStudent()
    {
        await CreateController().Create(new UserCreateViewModel
        {
            Username = "sv002", Password = "123456", ConfirmPassword = "123456",
            RoleId = _seed.SinhVienRoleId, StudentId = _seed.BinhId
        });

        using var context = _db.CreateContext();
        var user = await context.Users.SingleAsync(u => u.Username == "sv002");
        Assert.Equal(user.UserId, (await context.Students.FindAsync(_seed.BinhId))!.UserId);
    }

    [Fact]
    public async Task Create_StudentAccountWithoutStudent_ReturnsViewWithError()
    {
        var controller = CreateController();

        var result = await controller.Create(new UserCreateViewModel
        {
            Username = "sv009", Password = "123456", RoleId = _seed.SinhVienRoleId, StudentId = null
        });

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState["StudentId"]!.Errors.Count > 0);
    }

    [Fact]
    public async Task Create_StudentThatAlreadyHasAccount_ReturnsViewWithError()
    {
        var controller = CreateController();

        await controller.Create(new UserCreateViewModel
        {
            Username = "sv-an-2", Password = "123456", RoleId = _seed.SinhVienRoleId, StudentId = _seed.AnId
        });

        Assert.Equal("Học viên này đã được cấp tài khoản khác trước đó!", controller.ModelState["StudentId"]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Create_DuplicateUsername_ReturnsViewWithError()
    {
        var controller = CreateController();

        await controller.Create(new UserCreateViewModel { Username = "admin", Password = "123456", RoleId = _seed.AdminRoleId });

        Assert.Equal("Tên đăng nhập này đã được sử dụng!", controller.ModelState["Username"]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task ToggleStatus_OwnAccount_IsRejected()
    {
        var controller = CreateController();

        await controller.ToggleStatus(_adminUserId);

        Assert.Equal("Bạn không thể tự khóa tài khoản của chính mình!", controller.TempData["ErrorMessage"]);
        using var context = _db.CreateContext();
        Assert.True((await context.Users.FindAsync(_adminUserId))!.IsActive);
    }

    [Fact]
    public async Task ToggleStatus_OtherAccount_LocksThenUnlocks()
    {
        await CreateController().ToggleStatus(_linkedStudentUserId);
        using (var context = _db.CreateContext())
        {
            Assert.False((await context.Users.FindAsync(_linkedStudentUserId))!.IsActive);
        }

        await CreateController().ToggleStatus(_linkedStudentUserId);
        using var verify = _db.CreateContext();
        Assert.True((await verify.Users.FindAsync(_linkedStudentUserId))!.IsActive);
    }

    [Fact]
    public async Task ResetPassword_SetsHashOfDefaultPassword()
    {
        await CreateController().ResetPassword(_linkedStudentUserId);

        using var context = _db.CreateContext();
        Assert.True(BCrypt.Net.BCrypt.Verify("123456", (await context.Users.FindAsync(_linkedStudentUserId))!.Password));
    }

    [Fact]
    public async Task Delete_OwnAccount_IsRejected()
    {
        var controller = CreateController();

        await controller.DeleteConfirmed(_adminUserId);

        Assert.Equal("Không thể xóa tài khoản của chính bạn đang đăng nhập!", controller.TempData["ErrorMessage"]);
    }

    [Fact]
    public async Task Delete_AccountLinkedToStudent_IsRejected()
    {
        var controller = CreateController();

        await controller.DeleteConfirmed(_linkedStudentUserId);

        Assert.StartsWith("Tài khoản [sv001] đang gắn với hồ sơ học viên", (string?)controller.TempData["ErrorMessage"]);
        using var context = _db.CreateContext();
        Assert.NotNull(await context.Users.FindAsync(_linkedStudentUserId));
    }
}

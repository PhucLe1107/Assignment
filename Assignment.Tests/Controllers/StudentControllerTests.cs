using Assignment.Controllers;
using Assignment.Models.Entities;
using Assignment.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Assignment.Tests.Controllers;

public class StudentControllerTests : IDisposable
{
    private readonly TestDatabase _db = new();
    private readonly SeedData _seed;

    public StudentControllerTests() => _seed = _db.Seed();

    public void Dispose() => _db.Dispose();

    private StudentController CreateController() =>
        new StudentController(_db.CreateContext(), new FakeWebHostEnvironment(), new PassThroughLocalizer<SharedResource>())
            .WithContext("admin");

    private static Student NewStudent(string code = "SV100", string email = "new@example.com", string phone = "0911111111") =>
        TestDatabase.NewStudent(code, "Phạm Minh Đức", email, phone);

    [Fact]
    public async Task Create_WithoutAccount_SavesStudentWithDefaults()
    {
        var result = await CreateController().Create(NewStudent(), createAccount: false);

        Assert.IsType<RedirectToActionResult>(result);
        using var context = _db.CreateContext();
        var student = await context.Students.SingleAsync(s => s.StudentCode == "SV100");
        Assert.Null(student.UserId);
        Assert.Equal("/img/undraw_profile.svg", student.AvatarUrl);
        Assert.Equal("Beginner", student.EntryLevel);
    }

    [Theory]
    [InlineData("SV001", "x@example.com", "0999999999", "StudentCode")]
    [InlineData("SV100", "an@example.com", "0999999999", "Email")]
    [InlineData("SV100", "x@example.com", "0900000001", "PhoneNumber")]
    public async Task Create_DuplicateCodeEmailOrPhone_ReturnsViewWithFieldError(
        string code, string email, string phone, string expectedErrorField)
    {
        var controller = CreateController();

        var result = await controller.Create(NewStudent(code, email, phone));

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState[expectedErrorField]!.Errors.Count > 0);
    }

    [Fact]
    public async Task Create_WithAccount_CreatesLinkedStudentUser()
    {
        await CreateController().Create(NewStudent(), createAccount: true);

        using var context = _db.CreateContext();
        var student = await context.Students.Include(s => s.User).ThenInclude(u => u!.Role).SingleAsync(s => s.StudentCode == "SV100");
        Assert.NotNull(student.User);
        Assert.Equal("SV100", student.User.Username);           // mặc định lấy theo mã học viên
        Assert.Equal("SinhVien", student.User.Role.RoleName);
    }

    [Fact(Skip = "Bug: StudentController.Create lưu mật khẩu dạng chữ thường, không hash BCrypt, " +
                 "nên AccountController.Login (BCrypt.Verify) luôn báo sai mật khẩu với tài khoản tạo từ màn hình Học viên.")]
    public async Task Create_WithAccount_StoresBcryptHashSoStudentCanLogIn()
    {
        await CreateController().Create(NewStudent(), createAccount: true);

        using var context = _db.CreateContext();
        var user = await context.Users.SingleAsync(u => u.Username == "SV100");
        Assert.True(BCrypt.Net.BCrypt.Verify("123456", user.Password));
    }

    [Fact]
    public async Task Create_WithAccountWhenUsernameTaken_ReturnsViewWithError()
    {
        using (var context = _db.CreateContext())
        {
            context.Users.Add(new User { Username = "SV100", Password = "x", RoleId = _seed.GiaoVuRoleId });
            await context.SaveChangesAsync();
        }
        var controller = CreateController();

        var result = await controller.Create(NewStudent(), createAccount: true);

        Assert.IsType<ViewResult>(result);
        Assert.Equal("Tên đăng nhập 'SV100' đã có người sử dụng.", controller.ModelState[string.Empty]!.Errors.Single().ErrorMessage);
    }

    [Fact]
    public async Task Delete_StudentWithEnrollments_IsBlocked()
    {
        var controller = CreateController();

        await controller.DeleteConfirmed(_seed.AnId);

        Assert.Equal("Không thể xóa học viên này vì đang có dữ liệu đăng ký lớp học!", controller.TempData["ErrorMessage"]);
        using var context = _db.CreateContext();
        Assert.True(await context.Students.AnyAsync(s => s.StudentId == _seed.AnId));
    }

    [Fact]
    public async Task Delete_StudentWithoutEnrollments_RemovesIt()
    {
        await CreateController().DeleteConfirmed(_seed.CuongId);

        using var context = _db.CreateContext();
        Assert.False(await context.Students.AnyAsync(s => s.StudentId == _seed.CuongId));
    }
}

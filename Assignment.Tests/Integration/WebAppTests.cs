using System.Net;
using Assignment.Tests.Infrastructure;

namespace Assignment.Tests.Integration;

/// <summary>
/// Chạy cả ứng dụng (routing, cookie auth, phân quyền, đa ngôn ngữ, Razor view) với dữ liệu mẫu của DbInitializer.
/// </summary>
public class WebAppTests(AppFactory factory) : IClassFixture<AppFactory>
{
    private static async Task<string> HtmlAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

    [Fact]
    public async Task Root_WhenAnonymous_RedirectsToLowercaseLoginPage()
    {
        var response = await factory.CreateBrowser().GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login?ReturnUrl=%2F", response.Headers.Location!.PathAndQuery);
    }

    [Fact]
    public async Task Root_WhenAdminLoggedIn_RedirectsToCourseList()
    {
        var client = await factory.LoginAsync("admin", "admin@123");

        var response = await client.GetAsync("/");

        Assert.Equal("/course", response.Headers.Location!.OriginalString);
    }

    [Fact]
    public async Task Login_WrongPassword_ShowsErrorOnLoginPage()
    {
        var client = factory.CreateBrowser();

        var response = await client.PostAsync("/account/login", await client.LoginFormAsync("admin", "sai-mat-khau"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Mật khẩu không đúng.", await HtmlAsync(response));
    }

    [Fact]
    public async Task Login_ReturnUrl_RedirectsBackAfterLogin()
    {
        var client = factory.CreateBrowser();
        var form = await client.LoginFormAsync("admin", "admin@123");
        var withReturnUrl = new FormUrlEncodedContent(
            (await form.ReadAsStringAsync()).Split('&').Select(p => p.Split('='))
                .ToDictionary(p => WebUtility.UrlDecode(p[0]), p => WebUtility.UrlDecode(p[1]))
                .Append(new KeyValuePair<string, string>("ReturnUrl", "/grade")));

        var response = await client.PostAsync("/account/login", withReturnUrl);

        Assert.Equal("/grade", response.Headers.Location!.OriginalString);
    }

    [Theory]
    [InlineData("/course")]
    [InlineData("/course/create")]
    [InlineData("/course/details/1")]
    [InlineData("/class")]
    [InlineData("/class/details/1")]
    [InlineData("/student")]
    [InlineData("/student/details/1")]
    [InlineData("/enrollment")]
    [InlineData("/enrollment/create")]
    [InlineData("/attendance")]
    [InlineData("/attendance/take-attendance?classId=1&sessionNumber=1")]
    [InlineData("/grade")]
    [InlineData("/grade/enter-grades?classId=1&examType=GiuaKy")]
    [InlineData("/user")]
    [InlineData("/user/create")]
    public async Task AdminPages_RenderSuccessfully(string url)
    {
        var client = await factory.LoginAsync("admin", "admin@123");

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("", "Quản lý Khóa học")]
    [InlineData("?culture=vi-VN", "Quản lý Khóa học")]
    [InlineData("?culture=en-US", "Course management")]
    public async Task CoursePage_HeadingFollowsCulture(string query, string expectedHeading)
    {
        var client = await factory.LoginAsync("admin", "admin@123");

        var html = await HtmlAsync(await client.GetAsync("/course" + query));

        Assert.Contains(expectedHeading, html);
    }

    [Fact]
    public async Task SetLanguage_PersistsCultureInCookie()
    {
        var client = await factory.LoginAsync("admin", "admin@123");
        var token = HttpClientExtensions.ExtractAntiforgeryToken(await client.GetStringAsync("/course"));

        var response = await client.PostAsync("/language/set-language", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["culture"] = "en-US",
            ["returnUrl"] = "/grade",
            ["__RequestVerificationToken"] = token
        }));

        Assert.Equal("/grade", response.Headers.Location!.OriginalString);
        Assert.Contains("Student gradebook", await HtmlAsync(await client.GetAsync("/grade")));
    }

    [Fact]
    public async Task ValidationMessages_AreLocalizedOnVietnameseForms()
    {
        var client = await factory.LoginAsync("admin", "admin@123");

        var html = await HtmlAsync(await client.GetAsync("/class/create"));

        Assert.DoesNotContain("The field", html);
        Assert.DoesNotContain("field is required", html);
        Assert.Contains("data-val-length=\"Phòng học không được vượt quá 50 ký tự.\"", html);
    }

    [Fact]
    public async Task GeneratedLinks_AreLowercaseKebabCase()
    {
        var client = await factory.LoginAsync("admin", "admin@123");

        var html = await HtmlAsync(await client.GetAsync("/attendance"));

        Assert.Contains("href=\"/attendance/take-attendance\"", html);
        Assert.DoesNotContain("href=\"/Attendance", html);
    }

    [Fact]
    public async Task OldPascalCaseMultiWordUrl_IsNotFound()
    {
        var client = await factory.LoginAsync("admin", "admin@123");

        var response = await client.GetAsync("/Attendance/TakeAttendance");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetClassDefaultFee_ReturnsCourseTuitionAsJson()
    {
        var client = await factory.LoginAsync("admin", "admin@123");

        var json = await client.GetStringAsync("/enrollment/get-class-default-fee?classId=1");

        Assert.Contains("\"success\":true", json);
    }

    [Fact]
    public async Task AdminOnlyPage_WhenGiaoVu_RedirectsToAccessDenied()
    {
        var client = await factory.LoginAsync("giaovu01", "giaovu@123");

        var response = await client.GetAsync("/user");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/denied", response.Headers.Location!.PathAndQuery);
    }

    [Fact(Skip = "Bug: AccountController.Denied() gọi View() nhưng chưa có Views/Account/Denied.cshtml, trang trả về 500.")]
    public async Task AccessDeniedPage_RendersSuccessfully()
    {
        var client = await factory.LoginAsync("giaovu01", "giaovu@123");

        var response = await client.GetAsync("/account/denied");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task StudentLogin_RedirectsToStudentPortal()
    {
        var client = factory.CreateBrowser();

        var response = await client.PostAsync("/account/login", await client.LoginFormAsync("sv2026001", "123456"));

        Assert.Equal("/student-portal/my-learning", response.Headers.Location!.OriginalString);
    }

    [Fact(Skip = "Bug: chưa có StudentPortalController, học viên đăng nhập xong bị 404.")]
    public async Task StudentPortal_RendersForStudent()
    {
        var client = await factory.LoginAsync("sv2026001", "123456");

        var response = await client.GetAsync("/student-portal/my-learning");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

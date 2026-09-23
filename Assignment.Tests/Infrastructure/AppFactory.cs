using System.Net;
using System.Text.RegularExpressions;
using Assignment.Models.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Assignment.Tests.Infrastructure;

/// <summary>
/// Chạy toàn bộ web app trong bộ nhớ, thay SQL Server bằng SQLite in-memory.
/// DbInitializer trong Program.cs tự tạo schema và dữ liệu mẫu (admin, giaovu01, sv2026001...).
/// </summary>
public sealed class AppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public AppFactory() => _connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "unused-in-tests");

        builder.ConfigureServices(services =>
        {
            // Bỏ cấu hình UseSqlServer của Program.cs rồi đăng ký lại bằng SQLite
            var sqlServerRegistrations = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<EnglishCenterDbContext>)
                         || d.ServiceType == typeof(IDbContextOptionsConfiguration<EnglishCenterDbContext>))
                .ToList();
            foreach (var descriptor in sqlServerRegistrations)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<EnglishCenterDbContext>(options => options.UseSqlite(_connection));
        });
    }

    /// <summary>Client không tự đi theo redirect, giữ cookie giữa các request.</summary>
    public HttpClient CreateBrowser() =>
        CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    public async Task<HttpClient> LoginAsync(string username, string password)
    {
        var client = CreateBrowser();
        var response = await client.PostAsync("/account/login", await client.LoginFormAsync(username, password));
        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            throw new InvalidOperationException($"Đăng nhập '{username}' thất bại: {(int)response.StatusCode}");
        }
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _connection.Dispose();
    }
}

public static partial class HttpClientExtensions
{
    /// <summary>Lấy antiforgery token từ trang đăng nhập rồi dựng form POST.</summary>
    public static async Task<FormUrlEncodedContent> LoginFormAsync(this HttpClient client, string username, string password)
    {
        var html = await client.GetStringAsync("/account/login");
        return new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = username,
            ["Password"] = password,
            ["__RequestVerificationToken"] = ExtractAntiforgeryToken(html)
        });
    }

    public static string ExtractAntiforgeryToken(string html) =>
        AntiforgeryToken().Match(html) is { Success: true } match
            ? match.Groups[1].Value
            : throw new InvalidOperationException("Không tìm thấy __RequestVerificationToken trong trang.");

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();
}

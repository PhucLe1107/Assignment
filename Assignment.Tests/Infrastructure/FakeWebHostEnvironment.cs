using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Assignment.Tests.Infrastructure;

/// <summary>Môi trường giả, WebRootPath trỏ tới thư mục tạm để test không ghi vào wwwroot thật.</summary>
public sealed class FakeWebHostEnvironment : IWebHostEnvironment
{
    public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "assignment-tests-wwwroot");
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ApplicationName { get; set; } = "Assignment";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Path.GetTempPath();
    public string EnvironmentName { get; set; } = "Testing";
}

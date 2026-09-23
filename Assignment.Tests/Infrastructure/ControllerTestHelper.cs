using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;

namespace Assignment.Tests.Infrastructure;

public static class ControllerTestHelper
{
    /// <summary>Gắn HttpContext, TempData và người dùng đăng nhập (nếu có) cho controller.</summary>
    public static T WithContext<T>(this T controller, string? username = null, string role = "Admin")
        where T : Controller
    {
        var identity = username == null
            ? new ClaimsIdentity()
            : new ClaimsIdentity([new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, role)], "Test");

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider());
        return controller;
    }

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private IDictionary<string, object> _data = new Dictionary<string, object>();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) => _data = values;
    }
}

/// <summary>Localizer giả: trả về chính key (chuỗi tiếng Việt) sau khi format tham số.</summary>
public sealed class PassThroughLocalizer<T> : IStringLocalizer<T>
{
    public LocalizedString this[string name] => new(name, name);
    public LocalizedString this[string name, params object[] arguments] => new(name, string.Format(name, arguments));
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
}

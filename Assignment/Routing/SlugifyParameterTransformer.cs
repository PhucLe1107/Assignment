using System.Text.RegularExpressions;

namespace Assignment.Routing;

/// <summary>
/// Đổi tên controller/action trên URL sang dạng kebab-case chữ thường:
/// TakeAttendance -> take-attendance, StudentPortal -> student-portal.
/// </summary>
public sealed partial class SlugifyParameterTransformer : IOutboundParameterTransformer
{
    public string? TransformOutbound(object? value)
    {
        var text = value?.ToString();
        return string.IsNullOrEmpty(text) ? null : WordBoundary().Replace(text, "$1-$2").ToLowerInvariant();
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex WordBoundary();
}

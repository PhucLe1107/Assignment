using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Assignment.Tests.Localization;

/// <summary>
/// Canh file SharedResource.en.resx: mọi chuỗi dùng trong L["..."], _localizer["..."] và
/// [Display]/ErrorMessage phải có bản dịch tiếng Anh. Thêm chuỗi mới mà quên dịch thì test này đỏ.
/// </summary>
public partial class ResourceFileTests
{
    private static readonly string ProjectDir = FindProjectDir();
    private static readonly List<(string Key, string Value)> Entries = LoadEntries();

    [Fact]
    public void EveryLocalizedStringInCode_HasEnglishTranslation()
    {
        var keys = Entries.Select(e => e.Key).ToHashSet(StringComparer.Ordinal);

        var missing = UsedKeys().Where(k => !keys.Contains(k.Key))
            .Select(k => $"{k.Key}   ({k.File})")
            .Distinct()
            .ToList();

        Assert.True(missing.Count == 0, "Thiếu bản dịch trong SharedResource.en.resx:\n" + string.Join("\n", missing));
    }

    [Fact]
    public void ResourceKeys_AreUniqueIgnoringCase()
    {
        // resx so khớp tên không phân biệt hoa thường: "Học viên" và "học viên" bị coi là trùng, một key bị bỏ khi build
        var duplicates = Entries.GroupBy(e => e.Key, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => string.Join(" | ", g.Select(e => e.Key)))
            .ToList();

        Assert.True(duplicates.Count == 0, "Key trùng (không phân biệt hoa thường):\n" + string.Join("\n", duplicates));
    }

    [Fact]
    public void Translations_KeepTheSamePlaceholders()
    {
        var mismatched = Entries
            .Where(e => !Placeholders(e.Key).SetEquals(Placeholders(e.Value)))
            .Select(e => $"{e.Key}  ->  {e.Value}")
            .ToList();

        Assert.True(mismatched.Count == 0, "Bản dịch khác số tham số {0}, {1}...:\n" + string.Join("\n", mismatched));
    }

    private static IEnumerable<(string Key, string File)> UsedKeys()
    {
        var sources = Directory.EnumerateFiles(Path.Combine(ProjectDir, "Views"), "*.cshtml", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(Path.Combine(ProjectDir, "Controllers"), "*.cs"))
            .Concat(Directory.EnumerateFiles(Path.Combine(ProjectDir, "Localization"), "*.cs"));

        foreach (var file in sources)
        {
            var text = File.ReadAllText(file);
            var name = Path.GetRelativePath(ProjectDir, file);
            foreach (Match m in LocalizerCall().Matches(text)) yield return (m.Groups[1].Value, name);

            // Nhãn trả về từ DisplayLabels và thông báo mặc định của LocalizedValidationMetadataProvider
            if (file.EndsWith("DisplayLabels.cs") || file.EndsWith("LocalizedValidationMetadataProvider.cs"))
            {
                foreach (Match m in SwitchArm().Matches(text)) yield return (m.Groups[1].Value, name);
            }
        }

        foreach (var file in Directory.EnumerateFiles(Path.Combine(ProjectDir, "Models"), "*.cs", SearchOption.AllDirectories))
        {
            foreach (var line in File.ReadLines(file).Where(l => ValidationAttribute().IsMatch(l)))
            {
                foreach (Match m in AttributeText().Matches(line)) yield return (m.Groups[1].Value, Path.GetRelativePath(ProjectDir, file));
            }
        }
    }

    private static HashSet<string> Placeholders(string text) =>
        PlaceholderPattern().Matches(text).Select(m => m.Value).ToHashSet();

    private static List<(string, string)> LoadEntries() =>
        XDocument.Load(Path.Combine(ProjectDir, "Resources", "SharedResource.en.resx"))
            .Root!.Elements("data")
            .Select(d => ((string)d.Attribute("name")!, (string?)d.Element("value") ?? string.Empty))
            .ToList();

    private static string FindProjectDir()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Assignment.slnx")))
            {
                return Path.Combine(dir.FullName, "Assignment");
            }
        }
        throw new DirectoryNotFoundException("Không tìm thấy Assignment.slnx từ thư mục chạy test.");
    }

    [GeneratedRegex(@"(?:\bL|_localizer)\[""((?:[^""\\]|\\.)*)""")]
    private static partial Regex LocalizerCall();

    [GeneratedRegex(@"=> ""([^""]+)""")]
    private static partial Regex SwitchArm();

    [GeneratedRegex(@"\[(Display|Required|StringLength|Range|EmailAddress|RegularExpression|Compare|MaxLength|MinLength|Phone)\(")]
    private static partial Regex ValidationAttribute();

    [GeneratedRegex(@"(?:Name|ErrorMessage) = ""([^""]*)""")]
    private static partial Regex AttributeText();

    [GeneratedRegex(@"\{\d+\}")]
    private static partial Regex PlaceholderPattern();
}

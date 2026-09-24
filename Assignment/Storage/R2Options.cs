using System.ComponentModel.DataAnnotations;

namespace Assignment.Storage;

public sealed class R2Options
{
    public const string SectionName = "R2";

    [Required]
    public string AccountId { get; set; } = string.Empty;

    [Required]
    public string AccessKeyId { get; set; } = string.Empty;

    [Required]
    public string SecretAccessKey { get; set; } = string.Empty;

    [Required]
    public string BucketName { get; set; } = string.Empty;

    [Required, Url]
    public string PublicBaseUrl { get; set; } = string.Empty;
}

using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Assignment.Storage;

public sealed class R2ImageStorage : IImageStorage
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".gif",
        ".jpeg",
        ".jpg",
        ".png",
        ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/gif",
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private readonly IAmazonS3 _s3Client;
    private readonly R2Options _options;
    private readonly string _publicUrlPrefix;

    public R2ImageStorage(IAmazonS3 s3Client, IOptions<R2Options> options)
    {
        _s3Client = s3Client;
        _options = options.Value;
        _publicUrlPrefix = _options.PublicBaseUrl.TrimEnd('/') + "/";
    }

    public async Task<string> UploadAsync(
        IFormFile file,
        string pathPrefix,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (file.Length <= 0 || file.Length > MaxFileSize)
        {
            throw new InvalidOperationException("Image size must be between 1 byte and 5 MB.");
        }

        if (!AllowedExtensions.Contains(extension) || !AllowedContentTypes.Contains(file.ContentType))
        {
            throw new InvalidOperationException("Only JPEG, PNG, GIF, and WebP images are supported.");
        }

        var safePathPrefix = pathPrefix.Trim('/');
        var pathSegments = safePathPrefix.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments.Length == 0
            || pathSegments.Any(segment => segment.Any(character =>
                !(char.IsLetterOrDigit(character) || character is '-' or '_'))))
        {
            throw new ArgumentException("The storage path prefix is invalid.", nameof(pathPrefix));
        }

        var objectKey = $"{string.Join('/', pathSegments)}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}{extension}";
        await using var stream = file.OpenReadStream();

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = stream,
            ContentType = file.ContentType,
            DisablePayloadSigning = true,
            DisableDefaultChecksumValidation = true
        };
        request.Headers.CacheControl = "public, max-age=31536000, immutable";

        await _s3Client.PutObjectAsync(request, cancellationToken);
        return _publicUrlPrefix + objectKey;
    }

    public async Task DeleteAsync(
        string? publicUrl,
        CancellationToken cancellationToken = default)
    {
        var objectKey = GetObjectKey(publicUrl);
        if (objectKey == null)
        {
            return;
        }

        await _s3Client.DeleteObjectAsync(
            new DeleteObjectRequest
            {
                BucketName = _options.BucketName,
                Key = objectKey
            },
            cancellationToken);
    }

    private string? GetObjectKey(string? publicUrl)
    {
        if (string.IsNullOrWhiteSpace(publicUrl)
            || !publicUrl.StartsWith(_publicUrlPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var key = publicUrl[_publicUrlPrefix.Length..];
        return string.IsNullOrWhiteSpace(key) ? null : Uri.UnescapeDataString(key);
    }
}

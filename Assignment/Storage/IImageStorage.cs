using Microsoft.AspNetCore.Http;

namespace Assignment.Storage;

public interface IImageStorage
{
    Task<string> UploadAsync(
        IFormFile file,
        string pathPrefix,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string? publicUrl,
        CancellationToken cancellationToken = default);
}

using Microsoft.AspNetCore.Http;

namespace ImageProcessing.Application.Abstractions.Storage;

public interface IFileService
{
    Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default);
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}

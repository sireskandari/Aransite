using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ImageProcessing.Application.Abstractions.Storage;

namespace ImageProcessing.Infrastructure.Storage;

public sealed class FileService : IFileService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private readonly IHostEnvironment _env;
    private readonly ILogger<FileService> _logger;
    private readonly string _uploadRoot;

    public FileService(IHostEnvironment env, IConfiguration config, ILogger<FileService> logger)
    {
        _env = env;
        _logger = logger;

        var uploadPath = config["FileStorage:UploadPath"] ?? "uploads";
        _uploadRoot = Path.Combine(_env.ContentRootPath, uploadPath);

        if (!Directory.Exists(_uploadRoot))
            Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<string> SaveAsync(IFormFile file, string folder, CancellationToken ct = default)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("Empty file.");

        var ext = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(ext))
            throw new InvalidOperationException($"Unsupported file type: {ext}");

        // Sanitize and normalize folder path
        folder = folder.Replace('\\', '/').Trim('/');
        var dateFolder = DateTime.UtcNow.ToString("yyyy-MM");
        var targetFolder = Path.Combine(_uploadRoot, folder, dateFolder);
        Directory.CreateDirectory(targetFolder);

        var fileName = $"{Guid.NewGuid()}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(targetFolder, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        {
            await file.CopyToAsync(stream, ct);
        }

        var relativePath = Path.Combine(folder, dateFolder, fileName).Replace("\\", "/");
        _logger.LogInformation("Saved file: {RelativePath}", relativePath);
        return relativePath;
    }

    public Task DeleteAsync(string? relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return Task.CompletedTask;

        var fullPath = Path.Combine(_uploadRoot, relativePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
            _logger.LogInformation("Deleted file: {RelativePath}", relativePath);
        }

        return Task.CompletedTask;
    }
}

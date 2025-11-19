using MySqlConnector;

public interface ITimelapseMaintenance
{
    /// <summary>
    /// Deletes all generated timelapse files/folders.
    /// </summary>
    Task DeleteAllAsync(CancellationToken ct);
}
public sealed class TimelapseMaintenance : ITimelapseMaintenance
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _cfg;
    private readonly ILogger<TimelapseMaintenance> _logger;

    public TimelapseMaintenance(
        IWebHostEnvironment env,
        IConfiguration cfg,
        ILogger<TimelapseMaintenance> logger)
    {
        _env = env;
        _cfg = cfg;
        _logger = logger;
    }

    private string GetTimelapseRoot()
    {
        // Same base as your timelapse service / controller
        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            // In Docker this is usually "/app"
            webRoot = AppContext.BaseDirectory;
        }

        var sub = _cfg["FFMPEG:OUTPUT_SUBFOLDER"] ?? "uploads/timelapses";
        sub = sub.TrimStart('\\', '/')
                 .Replace('\\', '/');

        var root = Path.Combine(
            webRoot,
            sub.Replace('/', Path.DirectorySeparatorChar));

        return root;
    }

    public Task DeleteAllAsync(CancellationToken ct)
    {
        var root = GetTimelapseRoot();

        if (!Directory.Exists(root))
        {
            _logger.LogInformation("TimelapseMaintenance: root folder not found at {Root}. Nothing to delete.", root);
            return Task.CompletedTask;
        }

        _logger.LogInformation("TimelapseMaintenance: deleting all timelapse content under {Root}", root);

        // Delete subdirectories (each timelapse folder)
        foreach (var dir in Directory.EnumerateDirectories(root))
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                Directory.Delete(dir, recursive: true);
                _logger.LogInformation("TimelapseMaintenance: deleted directory {Dir}", dir);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TimelapseMaintenance: failed to delete directory {Dir}", dir);
            }
        }

        // Delete any files directly in the root (if any)
        foreach (var file in Directory.EnumerateFiles(root))
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                File.Delete(file);
                _logger.LogInformation("TimelapseMaintenance: deleted file {File}", file);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "TimelapseMaintenance: failed to delete file {File}", file);
            }
        }

        return Task.CompletedTask;
    }
}

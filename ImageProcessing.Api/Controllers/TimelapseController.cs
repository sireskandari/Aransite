// Controllers/v1/TimelapseController.cs
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Ocsp;
using System.Runtime.Intrinsics.Arm;

public sealed class GenerateFromEdgeRequest
{
    public string? Search { get; set; }
    public int Fps { get; set; } = 20;
    public int Width { get; set; } = 0;          // 0 = keep native resolution
    public int MaxFrames { get; set; } = 5000;
    public int Crf { get; set; } = 18;           // lower = higher quality
    public string Preset { get; set; } = "veryfast"; // "slow" for higher quality
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class TimelapseController : ControllerBase
{
    private readonly ITimelapseFromEdgeEventsService _svc;
    private readonly ILogger<TimelapseController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    public TimelapseController(
        ITimelapseFromEdgeEventsService svc,
        ILogger<TimelapseController> logger, IWebHostEnvironment env, IConfiguration configuration)
    {
        _svc = svc;
        _logger = logger;
        _env = env;
        _configuration = configuration;
    }

    /// <summary>
    /// Build a timelapse from EdgeEvents frames (ordered by CaptureTimestampUtc).
    /// Saves to /wwwroot/uploads/timelapses/{guid}/video.mp4
    /// Returns { downloadUrl } like "/uploads/timelapses/{guid}/video.mp4"
    /// </summary>
    [HttpPost("generate-from-edge")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateFromEdge([FromBody] GenerateFromEdgeRequest req, CancellationToken ct)
    {

        try
        {
            var url = await _svc.GenerateAsync(
                search: req.Search,
                fromUtc: req.FromUtc,
                toUtc: req.ToUtc,
                fps: req.Fps,
                width: req.Width,
                maxFrames: req.MaxFrames,
                ffmpegPath: _configuration["FFMPEG:FFMPEG_PATH"] ?? "C:\tools\ffmpeg\bin\ffmpeg.exe",
                outputSubFolder: _configuration["FFMPEG:OUTPUT_SUBFOLDER"] ?? "uploads\timelapses",
                crf: req.Crf,
                preset: req.Preset,
                ct: ct);

            return Ok(new { downloadUrl = url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timelapse generation failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("from-edge/stream")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> StreamFromEdge(
        [FromQuery] string? search,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] string? quality,   // "low", "medium", "high"
        [FromQuery] int? fps,          // optional manual override
        [FromQuery] int? width,        // optional manual override
        [FromQuery] int? maxFrames,    // optional manual override
        CancellationToken ct)
    {
        try
        {
            // Resolve quality → concrete ffmpeg options
            var opts = ResolveTimelapseOptions(quality, fps, width, maxFrames);

            var relativePath = await _svc.GenerateAsync(
                search: search,
                fromUtc: fromUtc,
                toUtc: toUtc,
                fps: opts.fps,
                width: opts.width,
                maxFrames: opts.maxFrames,
                ffmpegPath: _configuration["FFMPEG:FFMPEG_PATH"] ?? "C:\\tools\\ffmpeg\\bin\\ffmpeg.exe",
                outputSubFolder: _configuration["FFMPEG:OUTPUT_SUBFOLDER"] ?? "uploads\\timelapses",
                crf: opts.crf,
                preset: opts.preset,
                ct: ct
            );

            // Base path fix (same as we just did)
            var webRoot = _env.WebRootPath;
            if (string.IsNullOrWhiteSpace(webRoot))
            {
                webRoot = AppContext.BaseDirectory; // "/app" in Docker
            }

            var trimmed = relativePath.TrimStart('/', '\\')
                                      .Replace('/', Path.DirectorySeparatorChar);

            var physicalPath = Path.Combine(webRoot, trimmed);
            _logger.LogInformation("Timelapse: serving video from {PhysicalPath}", physicalPath);

            if (!System.IO.File.Exists(physicalPath))
            {
                _logger.LogError("Timelapse: generated video not found at {PhysicalPath}", physicalPath);
                return NotFound(new { error = "Generated video not found." });
            }

            var stream = new FileStream(
                physicalPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);

            var fileName = Path.GetFileName(physicalPath);
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";

            return File(stream, "video/mp4", enableRangeProcessing: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timelapse generation (stream) failed - TIMELAPSE_ERROR_MARKER_V2");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "TIMELAPSE_ERROR_MARKER_V2: " + ex.Message });
        }
    }

    private static (int fps, int width, int maxFrames, int crf, string preset)
    ResolveTimelapseOptions(string? qualityProfile, int? fps, int? width, int? maxFrames)
    {
        // Defaults if user doesn’t specify anything
        var q = (qualityProfile ?? "medium").ToLowerInvariant();

        int resultFps;
        int resultWidth;
        int resultMaxFrames;
        int crf;
        string preset;

        switch (q)
        {
            case "low":
                // Smallest size / fastest to generate
                resultFps = fps ?? 10;     // fewer frames per second
                resultWidth = width ?? 720;    // downscale
                resultMaxFrames = maxFrames ?? 2000;   // avoid insane length
                crf = 26;                 // more compression
                preset = "faster";
                break;

            case "high":
                // Highest quality (big files)
                resultFps = fps ?? 24;
                resultWidth = width ?? 0;      // keep native
                resultMaxFrames = maxFrames ?? 8000;
                crf = 18;                 // higher quality
                preset = "slow";             // better compression, more CPU
                break;

            case "medium":
            default:
                // Balanced default
                resultFps = fps ?? 20;
                resultWidth = width ?? 1280;
                resultMaxFrames = maxFrames ?? 4000;
                crf = 22;
                preset = "medium";
                break;
        }

        // Safety clamps
        if (resultFps < 5) resultFps = 5;
        if (resultFps > 60) resultFps = 60;

        if (resultWidth < 0) resultWidth = 0; // 0 = keep native

        if (resultMaxFrames <= 0) resultMaxFrames = 1000;

        return (resultFps, resultWidth, resultMaxFrames, crf, preset);
    }

}

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
    [FromQuery] DateTime? toUtc, CancellationToken ct)
    {
        try
        {
            // DEFAULTS – or pull from config if you want
            const int defaultFps = 20;
            const int defaultWidth = 0;      // keep original
            const int defaultMaxFrames = 5000;
            const int defaultCrf = 18;
            const string defaultPreset = "veryfast";

            var relativePath = await _svc.GenerateAsync(
                search: search,
                fromUtc: fromUtc,
                toUtc: toUtc,
                fps: defaultFps,
                width: defaultWidth,
                maxFrames: defaultMaxFrames,
                ffmpegPath: _configuration["FFMPEG:FFMPEG_PATH"] ?? "C:\tools\ffmpeg\bin\ffmpeg.exe",
                outputSubFolder: _configuration["FFMPEG:OUTPUT_SUBFOLDER"] ?? "uploads\timelapses",
                crf: defaultCrf,
                preset: defaultPreset,
                ct: ct
            );

            var webRoot = _env.WebRootPath;
            var physicalPath = Path.Combine(webRoot, relativePath.TrimStart('/', '\\'));

            if (!System.IO.File.Exists(physicalPath))
                return NotFound(new { error = "Generated video not found." });

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
            _logger.LogError(ex, "Timelapse generation (stream) failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }


}

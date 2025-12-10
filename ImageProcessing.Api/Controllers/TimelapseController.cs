using Asp.Versioning;
using ImageProcessing.Api.Models;
using ImageProcessing.Application.Timelapses;
using ImageProcessing.Application.Common;
using ImageProcessing.Application.Timelapse;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Net;
using System.Text.Json;
using ImageProcessing.Api.Jobs;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class TimelapseController : ControllerBase
{
    private readonly ITimelapseJobService _jobSvc;
    private readonly ITimelapseService _timelapseService;
    private readonly ILogger<TimelapseController> _logger;
    private readonly IWebHostEnvironment _env;

    public TimelapseController(
        IWebHostEnvironment env,
        ITimelapseJobService jobSvc,
        ITimelapseService timelapseService,
        ILogger<TimelapseController> logger)
    {
        _env = env;
        _jobSvc = jobSvc;
        _timelapseService = timelapseService;
        _logger = logger;
    }

    [HttpPost("generate-from-edge")]
    [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
    public async Task<IActionResult> GenerateFromEdge([FromBody] GenerateFromEdgeRequest req, CancellationToken ct)
    {
        try
        {
            var timelapseId = await _jobSvc.EnqueueGenerateFromEdgeAsync(req, ct);

            // Frontend can poll GET /api/v1/timelapse/{id} for status & download URL
            return Accepted(new
            {
                timelapseId,
                statusUrl = Url.Action("GetById", "Timelapse", new { id = timelapseId }, Request.Scheme)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Timelapse enqueue failed");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    [HttpGet("{id:guid}/stream")]
    public async Task<IActionResult> Stream(Guid id, CancellationToken ct)
    {
        var tl = await _timelapseService.GetByIdAsync(id, ct);
        if (tl is null || string.IsNullOrWhiteSpace(tl.FilePath))
        {
            // 404 should NOT be cached
            Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
            return NotFound(new { error = "Timelapse not found." });
        }

        var webRoot = _env.WebRootPath ?? AppContext.BaseDirectory;
        var physicalPath = Path.Combine(
            webRoot,
            tl.FilePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar)
        );

        if (!System.IO.File.Exists(physicalPath))
        {
            Response.Headers["Cache-Control"] = "no-store, no-cache, max-age=0";
            return NotFound(new { error = "File not found." });
        }

        var stream = new FileStream(physicalPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        // this 200 CAN be cached long-term
        Response.Headers["Cache-Control"] = "public, max-age=31536000"; // 1 year
        return File(stream, "video/mp4", enableRangeProcessing: true);
    }


    // Example status endpoint using your TimelapseService
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var tl = await _timelapseService.GetByIdAsync(id, ct);
        if (tl is null) return NotFound();

        // Optionally map to DTO with full download URL
        return Ok(tl);
    }

    // GET: api/v1/Timelapses?search=ahmad&pageNumber=1&pageSize=10
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "TimelapsesListPolicy")]
    public async Task<ActionResult<ApiResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            // Fetch from DB/service
            PagedResult<TimelapseResponse> result = await _timelapseService.ListAsync(search, pageNumber, pageSize, ct);

            // Add pagination header
            var pagination = new Pagination(result.PageNumber, result.PageSize, result.TotalCount);
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

            return Ok(ApiResponse.Ok(result.Items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List Timelapses failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/Timelapses/all
    [HttpGet("all", Name = "Timelapses.GetAll")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "TimelapsesGetAllPolicy")]
    public async Task<ActionResult<ApiResponse>> GetAll(
        [FromQuery] string? search,
        CancellationToken ct = default)
    {
        try
        {
            // Fetch from DB/service
            List<TimelapseResponse> result = await _timelapseService.GetAll(search, ct);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List Timelapses failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }
}

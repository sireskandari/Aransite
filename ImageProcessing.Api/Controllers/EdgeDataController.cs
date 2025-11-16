using Asp.Versioning;
using FluentValidation;
using ImageProcessing.Api.Models;
using ImageProcessing.Api.Security;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Abstractions.Storage;
using ImageProcessing.Application.Auth;
using ImageProcessing.Application.EdgeEvents;
using ImageProcessing.Application.Common;
using ImageProcessing.Application.EdgeEvents;
using ImageProcessing.Domain.Entities.EdgeEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Org.BouncyCastle.Ocsp;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImageProcessing.Api.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class EdgeDataController : ControllerBase
{
    private readonly ILogger<EdgeDataController> _logger;
    private readonly IEdgeEventsService _EdgeEvents;
    private readonly IValidator<CreateEdgeEventsRequest> _createValidator;

    public EdgeDataController(ILogger<EdgeDataController> logger, IEdgeEventsService edgeEventsService, IValidator<CreateEdgeEventsRequest> createValidator)
    {
        _EdgeEvents = edgeEventsService;
        _createValidator = createValidator;
        _logger = logger;
    }


    // GET: api/v1/EdgeEvents?search=ahmad&pageNumber=1&pageSize=10
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "EdgeEventsListPolicy")]
    public async Task<ActionResult<ApiResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            // Fetch from DB/service
            PagedResult<EdgeEventsResponse> result = await _EdgeEvents.ListAsync(search, pageNumber, pageSize, ct);

            // Add pagination header
            var pagination = new Pagination(result.PageNumber, result.PageSize, result.TotalCount);
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

            return Ok(ApiResponse.Ok(result.Items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List EdgeEvents failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/EdgeEvents/all
    [HttpGet("all", Name = "EdgeEvents.GetAll")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "EdgeEventsGetAllPolicy")]
    public async Task<ActionResult<ApiResponse>> GetAll(
        [FromQuery] string? search,
<<<<<<< HEAD
        [FromQuery] DateTime? fromUTC,
        [FromQuery] DateTime? toUTC,
=======
>>>>>>> b186aa7 (v4)
        CancellationToken ct = default)
    {
        try
        {
            // Fetch from DB/service
<<<<<<< HEAD
            List<EdgeEventsResponse> result = await _EdgeEvents.GetAll(search, fromUTC, toUTC, ct);
=======
            List<EdgeEventsResponse> result = await _EdgeEvents.GetAll(search, ct);
>>>>>>> b186aa7 (v4)
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List EdgeEvents failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/EdgeEvents/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "EdgeEventByIdPolicy")]
    public async Task<ActionResult<ApiResponse>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        try
        {
            //Logic
            var EdgeEvent = await _EdgeEvents.GetByIdAsync(id, ct);
            if (EdgeEvent is null)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "EdgeEvent not found"));

            return Ok(ApiResponse.Ok(EdgeEvent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get EdgeEvent failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ApiResponse>> Ingest(
        [FromForm] EdgeIngestRequest request,
        [FromServices] IFileService fileService,
        [FromServices] IAppDbContext db,
        [FromServices] IOutputCacheStore cache,
        CancellationToken ct)
    {
        string meta = request.Meta;
        IFormFile? frame_raw = request.Frame_Raw;
        IFormFile? frame_annotated = request.Frame_Annotated;
        EdgeMeta? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize<EdgeMeta>(meta, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            });

            if (parsed is null)
                return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "Invalid meta JSON."));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse meta JSON");
            return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "Failed to parse meta JSON."));
        }

        // Basic validation
        if (string.IsNullOrWhiteSpace(parsed.CameraId))
            return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "EdgeEvent_id is required."));
        if (string.IsNullOrWhiteSpace(parsed.TimestampUtc))
            return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "timestamp_utc is required."));


        string? rawRel = null, annRel = null;
        if (frame_raw is not null)
            rawRel = await fileService.SaveAsync(frame_raw, "edge-frames/raw", ct);
        if (frame_annotated is not null)
            annRel = await fileService.SaveAsync(frame_annotated, "edge-frames/annotated", ct);
        if (parsed == null)
            return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "meta_data is required."));
        if (rawRel == null)
            return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "frame_raw is required."));

        try
        {
            var req = new CreateEdgeEventsRequest(
                DateTime.Parse(parsed.TimestampUtc),
                parsed.CameraId,
                parsed.Compute != null ? parsed.Compute.Model : "Unknown",
                parsed.Compute.InferenceMs,
                parsed.Image.Width,
                parsed.Image.Height,
                parsed.Detections != null ? parsed.Detections.ToArray().ToString() : "",
                $"{Request.Scheme}://{Request.Host}/uploads/{rawRel}",
               annRel != null ? $"{Request.Scheme}://{Request.Host}/uploads/{annRel}" : "");

            var validation = await _createValidator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var created = await _EdgeEvents.CreateAsync(req, ct);

            // Bust OutputCache for all EdgeEvents GETs
            await cache.EvictByTagAsync("EdgeEvents", ct);
            return Ok();
            //return CreatedAtAction(nameof(GetById), new { version = "1.0", id = created.Id }, ApiResponse.Created(created));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create EdgeEvent failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }
}


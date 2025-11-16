using Asp.Versioning;
using FluentValidation;
using ImageProcessing.Api.Models;
using ImageProcessing.Application.Auth;
using ImageProcessing.Application.Cameras;
using ImageProcessing.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Net;
using System.Text.Json;

namespace ImageProcessing.Api.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class CamerasController : ControllerBase
{
    private readonly ILogger<CamerasController> _logger;
    private readonly ICamerasService _Cameras;
    private readonly IValidator<CreateCameraRequest> _createValidator;

    public CamerasController(
        ILogger<CamerasController> logger,
        ICamerasService Cameras,
        IValidator<CreateCameraRequest> createValidator)
    {
        _logger = logger;
        _Cameras = Cameras;
        _createValidator = createValidator;
    }

    // GET: api/v1/Cameras?search=ahmad&pageNumber=1&pageSize=10
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "CamerasListPolicy")]
    public async Task<ActionResult<ApiResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            // Fetch from DB/service
            PagedResult<CameraResponse> result = await _Cameras.ListAsync(search, pageNumber, pageSize, ct);

            // Add pagination header
            var pagination = new Pagination(result.PageNumber, result.PageSize, result.TotalCount);
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

            return Ok(ApiResponse.Ok(result.Items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List Cameras failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/Cameras/all
    [HttpGet("all", Name = "Cameras.GetAll")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "CamerasGetAllPolicy")]
    public async Task<ActionResult<ApiResponse>> GetAll(
        [FromQuery] string? search,
        CancellationToken ct = default)
    {
        try
        {
            // Fetch from DB/service
            List<CameraResponse> result = await _Cameras.GetAll(search, ct);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List Cameras failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/Cameras/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "CameraByIdPolicy")]
    public async Task<ActionResult<ApiResponse>> GetById(
        [FromRoute] Guid id,
        CancellationToken ct)
    {
        try
        {
            //Logic
            var Camera = await _Cameras.GetByIdAsync(id, ct);
            if (Camera is null)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "Camera not found"));

            return Ok(ApiResponse.Ok(Camera));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get Camera failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // POST: api/v1/Cameras
    [HttpPost]
    [Authorize(Policy = Policies.CanCreate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> Create(
        [FromBody] CreateCameraRequest req,
        [FromServices] IOutputCacheStore cache,
        CancellationToken ct)
    {
        try
        {
            var validation = await _createValidator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var created = await _Cameras.CreateAsync(req, ct);

            // Bust OutputCache for all Cameras GETs
            await cache.EvictByTagAsync("Cameras", ct);

            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = created.Id }, ApiResponse.Created(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create Camera failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // PUT: api/v1/Cameras/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanCreate)] // or define a CanUpdate policy if you prefer
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCameraRequest req,
        [FromServices] IOutputCacheStore cache,
        CancellationToken ct)
    {
        try
        {
            // Update by route id (do not rely on req.Id)
            var updated = await _Cameras.UpdateAsync(id, req, ct);
            if (updated is null)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "Camera not found"));

            // Invalidate cached GETs tagged "Cameras"
            await cache.EvictByTagAsync("Cameras", ct);

            // Return the updated resource (200 OK)
            return Ok(ApiResponse.Ok(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update Camera failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }


    // DELETE: api/v1/Cameras/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(
        [FromRoute] Guid id,
        [FromServices] IOutputCacheStore cache,
        CancellationToken ct)
    {
        try
        {
            var ok = await _Cameras.DeleteAsync(id, ct);
            if (!ok)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "Camera not found"));

            // Bust OutputCache for all Cameras GETs
            await cache.EvictByTagAsync("Cameras", ct);

            return StatusCode(StatusCodes.Status204NoContent, ApiResponse.NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete Camera failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }
}

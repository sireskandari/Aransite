using Asp.Versioning;
using FluentValidation;
using ImageProcessing.Api.Models;
using ImageProcessing.Api.Security;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Abstractions.Storage;
using ImageProcessing.Application.Auth;
using ImageProcessing.Application.Common;
using ImageProcessing.Application.EdgeDevices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Distributed;
using System.Net;
using System.Text.Json;

namespace ImageProcessing.Api.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class EdgeDevicesController : ControllerBase
{
    private readonly ILogger<EdgeDevicesController> _logger;
    private readonly IEdgeDevicesService _EdgeDevices;
    private readonly IValidator<CreateEdgeDeviceRequest> _createValidator;

    public EdgeDevicesController(
        ILogger<EdgeDevicesController> logger,
        IEdgeDevicesService EdgeDevices,
        IValidator<CreateEdgeDeviceRequest> createValidator)
    {
        _logger = logger;
        _EdgeDevices = EdgeDevices;
        _createValidator = createValidator;
    }


    // GET: api/v1/EdgeDevices?search=ahmad&pageNumber=1&pageSize=10
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "EdgeDevicesListPolicy")]
    public async Task<ActionResult<ApiResponse>> List([FromServices] IDistributedCache distributedCache, [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        try
        {
            // ✅ Create unique cache key per search and page
            string normalizedSearch = string.IsNullOrWhiteSpace(search) ? "all" : search.Trim().ToLowerInvariant();
            string cacheKey = $"EdgeDevices:{normalizedSearch}:page{pageNumber}:size{pageSize}";

            // Try cache first
            var cached = await distributedCache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
            {
                var cachedEdgeDevices = JsonSerializer.Deserialize<List<EdgeDeviceResponse>>(cached)!;
                return Ok(ApiResponse.Ok(cachedEdgeDevices));
            }

            // Fetch from DB/service
            PagedResult<EdgeDeviceResponse> result = await _EdgeDevices.ListAsync(search, pageNumber, pageSize, ct);

            // Add pagination header
            var pagination = new Pagination(result.PageNumber, result.PageSize, result.TotalCount);
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

            // Cache result for 60 minutes
            await distributedCache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(result.Items),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60)
                },
                ct);

            return Ok(ApiResponse.Ok(result.Items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List EdgeDevices failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/EdgeDevices/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "EdgeDeviceByIdPolicy")]
    public async Task<ActionResult<ApiResponse>> GetById([FromRoute] Guid id, [FromServices] IDistributedCache distributedCache, CancellationToken ct)
    {
        try
        {
            // Try cache first
            var cacheKey = $"EdgeDevice:{id}";
            var cached = await distributedCache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return Ok(ApiResponse.Ok(JsonSerializer.Deserialize<EdgeDeviceResponse>(cached)!));

            //Logic
            var EdgeDevice = await _EdgeDevices.GetByIdAsync(id, ct);
            if (EdgeDevice is null)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "EdgeDevice not found"));

            // Cache for 60m
            await distributedCache.SetStringAsync(
                cacheKey, JsonSerializer.Serialize(EdgeDevice),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) }, ct);

            return Ok(ApiResponse.Ok(EdgeDevice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get EdgeDevice failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // POST: api/v1/EdgeDevices
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] CreateEdgeDeviceRequest req, [FromServices] IOutputCacheStore cache, CancellationToken ct)
    {
        try
        {
            var validation = await _createValidator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var created = await _EdgeDevices.CreateAsync(req, ct);

            // Bust caches: the list and the specific EdgeDevice id (if any cached)
            await cache.EvictByTagAsync("EdgeDevices", ct);
            await cache.EvictByTagAsync($"EdgeDevice-{created.Id}", ct);


            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = created.Id }, ApiResponse.Created(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create EdgeDevice failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // DELETE: api/v1/EdgeDevices/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete([FromRoute] Guid id, [FromServices] IOutputCacheStore cache, CancellationToken ct)
    {
        try
        {
            var ok = await _EdgeDevices.DeleteAsync(id, ct);
            if (!ok)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "EdgeDevice not found"));

            // Bust caches: the list and the specific EdgeDevice id (if any cached)
            await cache.EvictByTagAsync("EdgeDevices", ct);
            await cache.EvictByTagAsync($"EdgeDevice-{id}", ct);

            return StatusCode(StatusCodes.Status204NoContent, ApiResponse.NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete EdgeDevice failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }



}

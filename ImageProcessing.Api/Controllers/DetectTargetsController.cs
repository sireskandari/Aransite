using Asp.Versioning;
using FluentValidation;
using ImageProcessing.Api.Models;
using ImageProcessing.Application.Auth;
using ImageProcessing.Application.Common;
using ImageProcessing.Application.DetectTargets;
using ImageProcessing.Domain.Entities.DetectTargets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Net;
using System.Text.Json;

namespace ImageProcessing.Api.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class DetectTargetsController : ControllerBase
{
    private readonly ILogger<DetectTargetsController> _logger;
    private readonly IDetectTargetsService _detectTargets;
    private readonly IValidator<CreateDetectTargetRequest> _createValidator;

    public DetectTargetsController(
        ILogger<DetectTargetsController> logger,
        IDetectTargetsService detectTargets,
        IValidator<CreateDetectTargetRequest> createValidator)
    {
        _logger = logger;
        _detectTargets = detectTargets;
        _createValidator = createValidator;
    }

    // GET: api/v1/DetectTargets?search=ahmad&pageNumber=1&pageSize=10
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "DetectTargetsListPolicy")]
    public async Task<ActionResult<ApiResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _detectTargets.ListAsync(search, pageNumber, pageSize, ct);

            var pagination = new Pagination(result.PageNumber, result.PageSize, result.TotalCount);
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

            return Ok(ApiResponse.Ok(result.Items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List DetectTargets failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/DetectTargets/all
    [HttpGet("all", Name = "DetectTargets.GetAll")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "DetectTargetsGetAllPolicy")]
    public async Task<ActionResult<ApiResponse>> GetAll(
        [FromQuery] string? search,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _detectTargets.GetAll(search, ct);
            return Ok(ApiResponse.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAll DetectTargets failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/DetectTargets/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "DetectTargetByIdPolicy")]
    public async Task<ActionResult<ApiResponse>> GetById([FromRoute] Guid id, CancellationToken ct)
    {
        try
        {
            var detectTarget = await _detectTargets.GetByIdAsync(id, ct);
            if (detectTarget is null)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "DetectTarget not found"));

            return Ok(ApiResponse.Ok(detectTarget));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get DetectTarget failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // POST: api/v1/DetectTargets
    [HttpPost]
    [Authorize(Policy = Policies.CanCreate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> Create(
        [FromBody] CreateDetectTargetRequest req,
        [FromServices] IOutputCacheStore cache,
        CancellationToken ct)
    {
        try
        {
            var validation = await _createValidator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return BadRequest(ApiResponse.Fail(
                    HttpStatusCode.BadRequest,
                    validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var created = await _detectTargets.CreateAsync(req, ct);

            await cache.EvictByTagAsync("DetectTargets", ct);
            await cache.EvictByTagAsync($"DetectTarget-{created.Id}", ct);

            return CreatedAtAction(
                nameof(GetById),
                new { version = "1.0", id = created.Id },
                ApiResponse.Created(created)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create DetectTarget failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // PUT: api/v1/DetectTargets/{id}
    [HttpPut("{id:guid}")]
    [Authorize(Policy = Policies.CanCreate)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateDetectTargetRequest req,
        [FromServices] IOutputCacheStore cache,
        CancellationToken ct)
    {
        try
        {
            var updated = await _detectTargets.UpdateAsync(id, req, ct);
            if (updated is null)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "DetectTarget not found"));

            await cache.EvictByTagAsync("DetectTargets", ct);

            return Ok(ApiResponse.Ok(updated));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update DetectTarget failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // DELETE: api/v1/DetectTargets/{id}
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
            var ok = await _detectTargets.DeleteAsync(id, ct);
            if (!ok)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "DetectTarget not found"));

            await cache.EvictByTagAsync("DetectTargets", ct);
            await cache.EvictByTagAsync($"DetectTarget-{id}", ct);

            return StatusCode(StatusCodes.Status204NoContent, ApiResponse.NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete DetectTarget failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }
}

using Asp.Versioning;
using FluentValidation;
using ImageProcessing.Api.Models;
using ImageProcessing.Application.Auth;
using ImageProcessing.Application.Common;
using ImageProcessing.Application.LogEvents;
using ImageProcessing.Domain.Entities.Cameras;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Net;
using System.Text.Json;

namespace ImageProcessing.Api.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public sealed class LogEventsController : ControllerBase
{
    private readonly ILogger<LogEventsController> _logger;
    private readonly ILogEventService _LogEvents;

    public LogEventsController(ILogger<LogEventsController> logger, ILogEventService LogEvents)
    {
        _logger = logger;
        _LogEvents = LogEvents;
    }

    // GET: api/v1/LogEvents?search=ahmad&pageNumber=1&pageSize=10
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "LogEventsListPolicy")]
    public async Task<ActionResult<ApiResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        try
        {
            // Fetch from DB/service
            PagedResult<LogEventResponse> result = await _LogEvents.ListAsync(search, pageNumber, pageSize, ct);

            // Add pagination header
            var pagination = new Pagination(result.PageNumber, result.PageSize, result.TotalCount);
            Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(pagination));

            return Ok(ApiResponse.Ok(result.Items));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List LogEvents failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/LogEvents/{id}
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "LogEventByIdPolicy")]
    public async Task<ActionResult<ApiResponse>> GetById(
        [FromRoute] int id,
        CancellationToken ct)
    {
        try
        {
            //Logic
            var LogEvent = await _LogEvents.GetByIdAsync(id, ct);
            if (LogEvent is null)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "LogEvent not found"));

            return Ok(ApiResponse.Ok(LogEvent));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get LogEvent failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }


    [HttpDelete]
    [Authorize(Policy = Policies.CanDelete)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(
        [FromServices] IOutputCacheStore cache,
        CancellationToken ct)
    {
        try
        {
            var ok = await _LogEvents.DeleteAllAsync(ct);
            if (!ok)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "LogEvent not found"));

            // Bust OutputCache for all LogEvents GETs
            await cache.EvictByTagAsync("LogEvents", ct);

            return StatusCode(StatusCodes.Status204NoContent, ApiResponse.NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete LogEvent failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }
}

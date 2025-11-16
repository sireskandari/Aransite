using Asp.Versioning;
using FluentValidation;
using ImageProcessing.Api.Models;
using ImageProcessing.Api.Security;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Abstractions.Storage;
using ImageProcessing.Application.Auth;
using ImageProcessing.Application.Common;
using ImageProcessing.Application.Users;
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
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IUsersService _users;
    private readonly IValidator<CreateUserRequest> _createValidator;

    public UsersController(
        ILogger<UsersController> logger,
        IUsersService users,
        IValidator<CreateUserRequest> createValidator)
    {
        _logger = logger;
        _users = users;
        _createValidator = createValidator;
    }

    // GET: api/v1/users?search=ahmad&pageNumber=1&pageSize=10
    [HttpGet]
    [Authorize(Policy = Policies.CanReadUsers)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [OutputCache(PolicyName = "UsersListPolicy")]
    public async Task<ActionResult<ApiResponse>> List([FromServices] IDistributedCache distributedCache, [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        try
        {
            // ✅ Create unique cache key per search and page
            string normalizedSearch = string.IsNullOrWhiteSpace(search) ? "all" : search.Trim().ToLowerInvariant();
            string cacheKey = $"users:{normalizedSearch}:page{pageNumber}:size{pageSize}";

            // Try cache first
            var cached = await distributedCache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
            {
                var cachedUsers = JsonSerializer.Deserialize<List<UserResponse>>(cached)!;
                return Ok(ApiResponse.Ok(cachedUsers));
            }

            // Fetch from DB/service
            PagedResult<UserResponse> result = await _users.ListAsync(search, pageNumber, pageSize, ct);

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
            _logger.LogError(ex, "List users failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // GET: api/v1/users/{id}
    [HttpGet("{id:guid}")]
    [Authorize(Policy = Policies.CanReadUsers)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = "UserByIdPolicy")]
    public async Task<ActionResult<ApiResponse>> GetById([FromRoute] Guid id, [FromServices] IDistributedCache distributedCache, CancellationToken ct)
    {
        try
        {
            // Try cache first
            var cacheKey = $"user:{id}";
            var cached = await distributedCache.GetStringAsync(cacheKey, ct);
            if (cached is not null)
                return Ok(ApiResponse.Ok(JsonSerializer.Deserialize<UserResponse>(cached)!));

            //Logic
            var user = await _users.GetByIdAsync(id, ct);
            if (user is null)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "User not found"));

            // Cache for 60m
            await distributedCache.SetStringAsync(
                cacheKey, JsonSerializer.Serialize(user),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) }, ct);

            return Ok(ApiResponse.Ok(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get user failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // POST: api/v1/users
    [HttpPost]
    [Authorize(Policy = Policies.CanCreateUser)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse>> Create([FromBody] CreateUserRequest req, [FromServices] IOutputCacheStore cache, CancellationToken ct)
    {
        try
        {
            var validation = await _createValidator.ValidateAsync(req, ct);
            if (!validation.IsValid)
                return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, validation.Errors.Select(e => e.ErrorMessage).ToArray()));

            var created = await _users.CreateAsync(req, ct);

            // Bust caches: the list and the specific user id (if any cached)
            await cache.EvictByTagAsync("users", ct);
            await cache.EvictByTagAsync($"user-{created.Id}", ct);


            return CreatedAtAction(nameof(GetById), new { version = "1.0", id = created.Id }, ApiResponse.Created(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create user failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }

    // DELETE: api/v1/users/{id}
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Policies.CanDeleteUser)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete([FromRoute] Guid id, [FromServices] IOutputCacheStore cache, CancellationToken ct)
    {
        try
        {
            var ok = await _users.DeleteAsync(id, ct);
            if (!ok)
                return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "User not found"));

            // Bust caches: the list and the specific user id (if any cached)
            await cache.EvictByTagAsync("users", ct);
            await cache.EvictByTagAsync($"user-{id}", ct);

            return StatusCode(StatusCodes.Status204NoContent, ApiResponse.NoContent());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete user failed");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse.Fail(HttpStatusCode.InternalServerError, ex.Message));
        }
    }


    [HttpPost("{id:guid}/upload")]
    [Authorize(Policy = Policies.CanCreateUser)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10_000_000)] // 10MB
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status415UnsupportedMediaType)]
    [SkipSanitization]
    public async Task<ActionResult<ApiResponse>> UploadProfileImage(
        Guid id,
        IFormFile file,
        [FromServices] IFileService fileService,
        [FromServices] IAppDbContext db,
        CancellationToken ct)
    {
        var user = await db.Users.FindAsync(new object?[] { id }, ct);
        if (user is null)
            return NotFound(ApiResponse.Fail(HttpStatusCode.NotFound, "User not found"));

        if (file == null)
            return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "No file uploaded"));

        // Delete old file if exists
        if (!string.IsNullOrEmpty(user.ProfileImagePath))
            await fileService.DeleteAsync(user.ProfileImagePath, ct);

        // Save new file
        var relativePath = await fileService.SaveAsync(file, "profile-images", ct);

        user.ProfileImagePath = relativePath;
        await db.SaveChangesAsync(ct);

        return Ok(ApiResponse.Ok(new
        {
            user.Id,
            ImageUrl = $"{Request.Scheme}://{Request.Host}/uploads/{relativePath}"
        }));
    }

}

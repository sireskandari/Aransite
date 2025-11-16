using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessing.Api.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/health")]
[ApiVersion("1.0")]
[AllowAnonymous] // you can secure later if needed
public sealed class HealthController : ControllerBase
{
    // same defaults you used in DetectTargetsController
    private static readonly string[] DefaultTargets = new[] { "person" };
    private static readonly Dictionary<string, string[]> CameraOverrides =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // optionally add overrides here
        };

    // same cameras as your CamerasController
    private static readonly List<object> Cameras = new()
    {
        new { id = "CAM1", location = "front gate",  rtsp = "rtsp://user:pass@192.168.1.50:554/h264" },
        new { id = "CAM2", location = "loading dock", rtsp = "rtsp://user:pass@192.168.1.51:554/h264" }
    };

    /// <summary>
    /// Returns combined config that Jetson uses (cameras + targets)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        var detectTargetsPayload = new
        {
            @default = DefaultTargets,
            cameras = CameraOverrides.Select(kvp => new { id = kvp.Key, targets = kvp.Value })
        };

        return Ok(new
        {
            timestamp_utc = DateTime.UtcNow,
            cameras = Cameras,
            detect_targets = detectTargetsPayload,
            status = "ok"
        });
    }
}

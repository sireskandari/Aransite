using Hangfire;
using ImageProcessing.Api.Models;
using ImageProcessing.Application.Timelapse;
using ImageProcessing.Application.Timelapses;
using ImageProcessing.Domain.Entities.Timelapse;
using Microsoft.Extensions.Logging;

namespace ImageProcessing.Api.Jobs
{
    public interface ITimelapseJobService
    {
        Task<Guid> EnqueueGenerateFromEdgeAsync(GenerateFromEdgeRequest req, CancellationToken ct);
        Task RunGenerateFromEdgeJobAsync(Guid timelapseId, GenerateFromEdgeRequest req, CancellationToken ct);
    }

    public class TimelapseJobService : ITimelapseJobService
    {
        private readonly ITimelapseFromEdgeEventsService _edgeSvc;
        private readonly ITimelapseService _timelapseService;
        private readonly IBackgroundJobClient _bg;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<TimelapseJobService> _logger;

        public TimelapseJobService(
            ITimelapseFromEdgeEventsService edgeSvc,
            ITimelapseService timelapseService,
            IBackgroundJobClient bg,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<TimelapseJobService> logger)
        {
            _edgeSvc = edgeSvc;
            _timelapseService = timelapseService;
            _bg = bg;
            _env = env;
            _config = config;
            _logger = logger;
        }

        public async Task<Guid> EnqueueGenerateFromEdgeAsync(GenerateFromEdgeRequest req, CancellationToken ct)
        {
            // 1) create Timelapse row as "Pending"
            var createReq = new CreateTimelapseRequest("", "mp4", "0", TimelapseStatus.Pending, "");
            var tl = await _timelapseService.CreateAsync(createReq, ct);

            // 2) enqueue background job
            _bg.Enqueue<ImageProcessing.Api.Jobs.TimelapseJobService>(svc => svc.RunGenerateFromEdgeJobAsync(tl.Id, req, CancellationToken.None));
            return tl.Id;
        }

        public async Task RunGenerateFromEdgeJobAsync(Guid timelapseId, GenerateFromEdgeRequest req, CancellationToken ct)
        {
            try
            {
                await _timelapseService.UpdateAsync(
                    timelapseId,
                    new UpdateTimelapseRequest("", "mp4", "0", TimelapseStatus.Processing, null),
                    ct);

                var ffmpegPath = _config["FFMPEG:FFMPEG_PATH"] ?? "C:\\tools\\ffmpeg\\bin\\ffmpeg.exe";
                var outputSubFolder = _config["FFMPEG:OUTPUT_SUBFOLDER"] ?? "uploads\\timelapses";

                var relativePath = await _edgeSvc.GenerateAsync(
                    search: req.Search,
                    cameraId: req.CameraId,
                    fromUtc: req.FromUtc,
                    toUtc: req.ToUtc,
                    fps: req.Fps,
                    width: req.Width,
                    maxFrames: req.MaxFrames,
                    ffmpegPath: ffmpegPath,
                    outputSubFolder: outputSubFolder,
                    crf: req.Crf,
                    preset: req.Preset,
                    ct: ct);

                var webRoot = _env.WebRootPath;
                if (string.IsNullOrWhiteSpace(webRoot))
                    webRoot = AppContext.BaseDirectory;

                var trimmed = relativePath.TrimStart('/', '\\')
                                          .Replace('/', Path.DirectorySeparatorChar);

                var physicalPath = Path.Combine(webRoot, trimmed);
                var fileInfo = new FileInfo(physicalPath);
                var sizeString = fileInfo.Exists ? fileInfo.Length.ToString() : "0";

                await _timelapseService.UpdateAsync(
                    timelapseId,
                    new UpdateTimelapseRequest(relativePath, "mp4", sizeString, TimelapseStatus.Completed, null),
                    ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Timelapse job {TimelapseId} failed", timelapseId);

                await _timelapseService.UpdateAsync(
                    timelapseId,
                    new UpdateTimelapseRequest("", "mp4", "0", TimelapseStatus.Failed, ex.Message),
                    ct);
            }
        }
    }
}

// Services/TimelapseFromEdgeEventsService.cs
using ImageProcessing.Application.EdgeEvents;
using System.Diagnostics;

public interface ITimelapseFromEdgeEventsService
{
    Task<string> GenerateAsync(
        string? search,
          DateTime? fromUtc,
        DateTime? toUtc,
        int fps,
        int width,          // 0 = keep native resolution (no scaling)
        int maxFrames,
        string ffmpegPath,
        string outputSubFolder,
        int crf,            // e.g., 16 for higher quality
        string preset,      // e.g., "slow" for better quality
        CancellationToken ct);
}

public sealed class TimelapseFromEdgeEventsService : ITimelapseFromEdgeEventsService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<TimelapseFromEdgeEventsService> _logger;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IEdgeEventsService _edgeEvents;

    public TimelapseFromEdgeEventsService(
        IWebHostEnvironment env,
        ILogger<TimelapseFromEdgeEventsService> logger,
        IHttpClientFactory httpFactory,
        IEdgeEventsService edgeEvents)
    {
        _env = env;
        _logger = logger;
        _httpFactory = httpFactory;
        _edgeEvents = edgeEvents;
    }
    private static readonly HashSet<string> ValidX264Presets =
    new(StringComparer.OrdinalIgnoreCase)
    {
        "ultrafast",
        "superfast",
        "veryfast",
        "faster",
        "fast",
        "medium",
        "slow",
        "slower",
        "veryslow",
        "placebo"
    };
    public async Task<string> GenerateAsync(
     string? search,
       DateTime? fromUtc,
        DateTime? toUtc,
     int fps,
     int width,
     int maxFrames,
     string ffmpegPath,
     string outputSubFolder,
     int crf,
     string preset,
     CancellationToken ct)
    {
        if (fps <= 0) fps = 20;
        if (maxFrames <= 0) maxFrames = 5000;
        if (crf <= 0) crf = 18;
        if (string.IsNullOrWhiteSpace(preset)) preset = "veryfast";

        if (!ValidX264Presets.Contains(preset))
        {
            preset = "veryfast";
        }

        // Validate ffmpeg path early
        if (string.IsNullOrWhiteSpace(ffmpegPath) || !File.Exists(ffmpegPath))
            throw new FileNotFoundException($"FFmpeg not found at: {ffmpegPath}");

        // 1) Pull frames from DB
        var events = await _edgeEvents.GetAll(search, fromUtc, toUtc, ct);
        var frames = events
            .Where(e => !string.IsNullOrWhiteSpace(e.FrameRawUrl))
            .OrderBy(e => e.CaptureTimestampUtc)
            .Select(e => e.FrameRawUrl!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(maxFrames)
            .ToList();

        if (frames.Count < 2)
            throw new InvalidOperationException("Need at least 2 frames to build a timelapse.");

        // 2) Prepare working folders under wwwroot/<outputSubFolder>/<id>
        var id = Guid.NewGuid().ToString("N");
        var webRoot = _env.WebRootPath ?? "wwwroot";
        var sub = (outputSubFolder ?? "uploads/timelapses").Trim().TrimStart('\\', '/');
        var workDir = Path.Combine(webRoot, sub, id);
        var framesDir = Path.Combine(workDir, "frames");
        Directory.CreateDirectory(framesDir);

        var listFile = Path.Combine(workDir, "list.txt");
        var output = Path.Combine(workDir, "video.mp4");

        var client = _httpFactory.CreateClient();
        var localPaths = new List<string>(frames.Count);

        try
        {
            // 3) Fetch frames → local files (if FrameRawUrl is a local path, skip download)
            int i = 0;
            foreach (var raw in frames)
            {
                ct.ThrowIfCancellationRequested();

                string local;
                if (File.Exists(raw))
                {
                    local = raw; // already local
                }
                else
                {
                    var name = $"frame_{i++:000000}.jpg";
                    local = Path.Combine(framesDir, name);

                    using var resp = await client.GetAsync(raw, ct);
                    resp.EnsureSuccessStatusCode();

                    await using var fs = new FileStream(local, FileMode.Create, FileAccess.Write, FileShare.None);
                    await resp.Content.CopyToAsync(fs, ct);
                }

                localPaths.Add(local);
            }

            if (localPaths.Count < 2)
                throw new InvalidOperationException("After fetching, there are fewer than 2 frames.");

            // 4) Build concat list for FFmpeg
            var duration = 1.0 / fps;
            await using (var sw = new StreamWriter(listFile))
            {
                foreach (var p in localPaths)
                {
                    sw.WriteLine($"file '{EscapeForFfmpeg(p)}'");
                    sw.WriteLine($"duration {duration:0.######}");
                }
                sw.WriteLine($"file '{EscapeForFfmpeg(localPaths[^1])}'");
            }

            // 5) Build FFmpeg args
            // If width == 0 => keep native (no scale). Otherwise scale to width, keep aspect.
            string vf = (width > 0)
                ? $"scale={width}:-2,format=yuv420p"
                : "format=yuv420p";

            // CRF lower = higher quality. Preset slower = better quality (same size) or smaller file (same quality).
            // Keep yuv420p for broad compatibility.
            var args =
                $"-y -f concat -safe 0 -i \"{listFile}\" " +
                $"-r {fps} -vf \"{vf}\" " +
                $"-c:v libx264 -pix_fmt yuv420p -crf {crf} -preset {preset} -profile:v high -level 5.2 " +
                "-movflags +faststart " +
                $"\"{output}\"";

            var (ok, stderr) = await RunProcessAsync(ffmpegPath, args, workDir, ct);
            if (!ok)
            {
                _logger.LogError("FFmpeg failed: {stderr}", stderr);
                throw new Exception("FFmpeg failed: " + stderr);
            }
        }
        finally
        {
            // 6) Cleanup temp frames + list.txt (keep video.mp4)
            SafeDeleteFile(listFile);
            SafeDeleteDirectory(framesDir);
        }

        // 7) Return the public URL (relative)
        var relative = "/" + Path.Combine(sub, id, "video.mp4").Replace('\\', '/');
        return relative;
    }

    private static string EscapeForFfmpeg(string p) => p.Replace("'", "\\'");

    private static async Task<(bool ok, string stderr)> RunProcessAsync(
        string file, string args, string workingDir, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            Arguments = args,
            WorkingDirectory = workingDir,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start ffmpeg process.");
        var stderrTask = proc.StandardError.ReadToEndAsync();
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();

        await Task.WhenAll(proc.WaitForExitAsync(ct), stderrTask, stdoutTask);
        return (proc.ExitCode == 0, await stderrTask);
    }

    private static void SafeDeleteFile(string? p)
    {
        try { if (!string.IsNullOrEmpty(p) && File.Exists(p)) File.Delete(p); } catch { }
    }

    private static void SafeDeleteDirectory(string? dir)
    {
        try { if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { }
    }
}

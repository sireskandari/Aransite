using System.Text.RegularExpressions;

namespace ImageProcessing.Api.Security;

public sealed class InputSanitizationOptions
{
    // Inspect these content types
    public HashSet<string> ContentTypesToInspect { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/json",
        "application/x-www-form-urlencoded",
        "text/plain",
        "text/json"
    };

    // Max bytes to read from body (avoid large allocations)
    public int MaxBodyBytesToInspect { get; set; } = 1_000_000; // 1 MB

    // Skip these path prefixes entirely
    public List<string> PathExclusions { get; } = new()
    {
        "/jobs",        // Hangfire
        "/swagger",     // Swagger UI
        "/uploads"      // static files
    };

    // Suspicious patterns (compiled) — tuned for XSS/SQLi-ish signatures
    public List<Regex> SuspiciousPatterns { get; } =
    [
        // HTML/JS vectors
        new(@"<\s*script\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bon\w+\s*=",  RegexOptions.IgnoreCase | RegexOptions.Compiled),      // onerror=, onclick=...
        new(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"data\s*:\s*text/html", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // SQL-ish
        new(@"\bunion\s+select\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bdrop\s+table\b",   RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"\bor\s+1\s*=\s*1\b", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        new(@"--",                 RegexOptions.Compiled),
        new(@"/\*.*\*/",           RegexOptions.Singleline | RegexOptions.Compiled),
        new(@"\bxp_",              RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // Null byte
        new(@"\x00", RegexOptions.Compiled)
    ];
}

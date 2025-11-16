using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace ImageProcessing.Api.Security;

public sealed class InputSanitizationMiddleware(RequestDelegate next, IOptions<InputSanitizationOptions> opt, ILogger<InputSanitizationMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly InputSanitizationOptions _opt = opt.Value;
    private readonly ILogger<InputSanitizationMiddleware> _logger = logger;

    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? string.Empty;

        // 0) Skip excluded paths fast
        if (_opt.PathExclusions.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(ctx);
            return;
        }

        // 1) Skip if endpoint explicitly opted out
        var endpoint = ctx.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<SkipSanitizationAttribute>() is not null)
        {
            await _next(ctx);
            return;
        }

        // 2) Always inspect query values (small + cheap)
        foreach (var kv in ctx.Request.Query)
        {
            if (IsSuspicious(kv.Key) || IsSuspicious(kv.Value))
            {
                Reject(ctx, $"Suspicious query parameter: '{kv.Key}'.");
                return;
            }
        }

        // 3) Inspect body depending on content type
        var ct = (ctx.Request.ContentType ?? string.Empty).Split(';')[0].Trim();

        // multipart: inspect only non-file fields to avoid reading huge files here
        if (ct.Equals("multipart/form-data", StringComparison.OrdinalIgnoreCase))
        {
            // Buffer form once, limit size via server config/RequestSizeLimit
            ctx.Request.EnableBuffering();
            try
            {
                var form = await ctx.Request.ReadFormAsync();
                foreach (var kv in form)
                {
                    if (IsSuspicious(kv.Key) || IsSuspicious(kv.Value))
                    {
                        Reject(ctx, $"Suspicious form field: '{kv.Key}'.");
                        return;
                    }
                }
                // don't touch files here; your upload endpoint validates them separately
            }
            catch
            {
                // if parsing fails, let the normal pipeline handle model binding errors
            }
            finally
            {
                ctx.Request.Body.Position = 0;
            }
        }
        else if (_opt.ContentTypesToInspect.Contains(ct))
        {
            // Read small bodies safely
            ctx.Request.EnableBuffering(_opt.MaxBodyBytesToInspect, _opt.MaxBodyBytesToInspect);
            using var reader = new StreamReader(ctx.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 8192, leaveOpen: true);
            var body = await reader.ReadToEndAsync();

            // restore stream position for model binding
            ctx.Request.Body.Position = 0;

            if (!string.IsNullOrEmpty(body) && IsSuspicious(body))
            {
                Reject(ctx, "Suspicious payload detected in request body.");
                return;
            }
        }
        // else: other types (e.g., octet-stream) — skip

        await _next(ctx);
    }

    private bool IsSuspicious(string? value)
        => !string.IsNullOrEmpty(value) && _opt.SuspiciousPatterns.Any(rx => rx.IsMatch(value));

    private bool IsSuspicious(Microsoft.Extensions.Primitives.StringValues values)
    {
        foreach (var v in values)
            if (IsSuspicious(v)) return true;
        return false;
    }

    private void Reject(HttpContext ctx, string detail)
    {
        _logger.LogWarning("InputSanitization blocked request {Path} : {Detail}", ctx.Request.Path, detail);
        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
        ctx.Response.ContentType = "application/problem+json";
        // simple ProblemDetails payload; your global middleware would also be fine
        ctx.Response.WriteAsync($$"""
        {
          "title": "Bad Request",
          "status": 400,
          "detail": "{{Escape(detail)}}",
          "traceId": "{{ctx.TraceIdentifier}}"
        }
        """);
    }

    private static string Escape(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

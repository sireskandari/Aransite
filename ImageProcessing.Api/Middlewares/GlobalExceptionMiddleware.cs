using System.Net;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ImageProcessing.Api.Middlewares;

public static class GlobalExceptionMiddleware
{
    public static void UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
                var ex = exceptionHandler?.Error;

                var status = ex switch
                {
                    FluentValidation.ValidationException => HttpStatusCode.BadRequest,
                    KeyNotFoundException => HttpStatusCode.NotFound,
                    UnauthorizedAccessException => HttpStatusCode.Unauthorized,
                    InvalidOperationException => HttpStatusCode.Conflict,
                    _ => HttpStatusCode.InternalServerError
                };

                var problem = new ProblemDetails
                {
                    Title = ex?.Message ?? "An unexpected error occurred.",
                    Status = (int)status,
                    Type = "https://tools.ietf.org/html/rfc7231",
                    Instance = context.Request.Path
                };

                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                                   .CreateLogger("GlobalException");
                logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);

                problem.Extensions["traceId"] = context.TraceIdentifier;
                if (context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                    problem.Extensions["exception"] = ex?.StackTrace;

                context.Response.StatusCode = (int)status;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
        });
    }
}

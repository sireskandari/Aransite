using Asp.Versioning;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.MySql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using ImageProcessing.Api.Middlewares;
using ImageProcessing.Api.Security;
using ImageProcessing.Application.Auth;
using ImageProcessing.Application.Users;
using ImageProcessing.Infrastructure;
using ImageProcessing.Infrastructure.Persistence;
using System.Text;
using System.Threading.RateLimiting;
using ImageProcessing.Application.EdgeEvents;
using ImageProcessing.Application.Cameras;
using ImageProcessing.Application.DetectTargets;
using MySql.Data.MySqlClient;

var builder = WebApplication.CreateBuilder(args);

// 1) Bootstrap Serilog from config
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.MySQL(
        connectionString: builder.Configuration.GetConnectionString("Default")!,
        tableName: "serilog_logs")
    .CreateLogger();

SelfLog.Enable(Console.Error);

builder.Host.UseSerilog();

// MVC Controllers
builder.Services.AddControllers();

// API Versioning (routes like /api/v1/users)
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Swagger (single doc)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "ImageProcessing API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddHttpClient();

// Validation + App services
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateUserValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateEdgeEventsValidator>();
builder.Services.AddScoped<IUsersService, UsersService>();
builder.Services.AddScoped<IEdgeEventsService, EdgeEventsService>();
builder.Services.AddScoped<ICamerasService, CamerasService>();
builder.Services.AddScoped<IDetectTargetsService, DetectTargetsService>();
builder.Services.AddScoped<ITimelapseFromEdgeEventsService, TimelapseFromEdgeEventsService>();




// Infrastructure (EF + MySQL)
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier;
        if (builder.Environment.IsDevelopment() && ctx.Exception is not null)
        {
            ctx.ProblemDetails.Extensions["exception"] = ctx.Exception.GetType().FullName;
            ctx.ProblemDetails.Extensions["stackTrace"] = ctx.Exception.StackTrace;
        }
    };
});


var cs = builder.Configuration.GetConnectionString("Default")
         ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

// Hangfire storage on MySQL
builder.Services.AddHangfire(cfg =>
{
    cfg.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
       .UseSimpleAssemblyNameTypeSerializer()
       .UseRecommendedSerializerSettings()
       .UseStorage(new MySqlStorage(cs, new MySqlStorageOptions
       {
           TablesPrefix = "hangfire",   // tables: hangfire* in your DB
           TransactionIsolationLevel = System.Transactions.IsolationLevel.ReadCommitted,
           QueuePollInterval = TimeSpan.FromSeconds(15)
       }));
});

// Background worker
builder.Services.AddHangfireServer();

// our maintenance job implementation (below)
builder.Services.AddScoped<ILogMaintenance, LogMaintenance>();

var key = builder.Configuration["Jwt:Key"]!;


builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy("RequireAdmin", p => p.RequireRole("Admin"));
});

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Nice 429 payload + headers
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.Headers.RetryAfter = "60"; // seconds
        ctx.HttpContext.Response.Headers.Append("X-RateLimit-Policy", ctx.Lease?.ToString() ?? "global");
        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            title = "Too many requests",
            status = 429,
            detail = "You have exceeded the allowed request rate. Please retry after a short delay.",
            traceId = ctx.HttpContext.TraceIdentifier
        }, cancellationToken: token);
    };

    // Helper: partition key = user id if authenticated, else client IP
    static string PartitionKey(HttpContext http)
        => http.User.FindFirst("sub")?.Value
           ?? http.Connection.RemoteIpAddress?.ToString()
           ?? "anonymous";

    // ① Global policy: 100 requests / minute per partition (split into 6 segments)
    options.AddPolicy("global", http =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: PartitionKey(http),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 6,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // ② Burst policy: token bucket, 60 tokens/min, burst up to 20
    options.AddPolicy("burst", http =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: PartitionKey(http),
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 20,                      // burst size
                QueueLimit = 0,
                ReplenishmentPeriod = TimeSpan.FromSeconds(1),
                TokensPerPeriod = 1,                  // ~60/min
                AutoReplenishment = true
            }));

    // ③ Uploads (later): allow 5 concurrent requests per partition, queue 50
    options.AddPolicy("uploads", http =>
        RateLimitPartition.GetConcurrencyLimiter(
            partitionKey: PartitionKey(http),
            factory: _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = 5,
                QueueLimit = 50,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});


// Authorization
builder.Services.AddAuthorization(options =>
{
    // Simple role-based policies
    options.AddPolicy(Policies.CanReadUsers, p => p.RequireRole(Roles.Admin, Roles.User));
    options.AddPolicy(Policies.CanCreateUser, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.CanDeleteUser, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.CanDelete, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.CanCreate, p => p.RequireRole(Roles.Admin));
    options.AddPolicy(Policies.CanRead, p => p.RequireRole(Roles.Admin));

    // Example of a claim-based rule you can use later:
    // options.AddPolicy("CanExport", p => p.RequireClaim("scope", "users.export"));
});


builder.Services.AddOutputCache(o =>
{
    // Policy for users list: cache 30s, vary by search/page params, tag = "users"
    o.AddPolicy("UsersListPolicy", p => p
        .Expire(TimeSpan.FromSeconds(30))
        .SetVaryByQuery("search", "pageNumber", "pageSize")
        .Tag("users"));

    // Policy for user by id: cache 60s, vary by route {id}, tag = "user-{id}"
    o.AddPolicy("UserByIdPolicy", p => p
        .Expire(TimeSpan.FromSeconds(60))
        .SetVaryByRouteValue("id")
        .Tag("user-{id}"));

    o.AddPolicy("CamerasListPolicy", b => b
     .Cache()
     .Expire(TimeSpan.FromMinutes(60))
     .Tag("Cameras") // list-level tag
     .SetVaryByQuery("search", "pageNumber", "pageSize"));

    o.AddPolicy("CamerasListPolicy", b => b.Cache().Expire(TimeSpan.FromMinutes(60))
     .Tag("Cameras").SetVaryByQuery("search", "pageNumber", "pageSize"));

    o.AddPolicy("CamerasGetAllPolicy", b => b.Cache().Expire(TimeSpan.FromMinutes(60))
        .Tag("Cameras").SetVaryByQuery("search"));

    o.AddPolicy("CameraByIdPolicy", b => b.Cache().Expire(TimeSpan.FromMinutes(60))
        .Tag("Cameras").SetVaryByRouteValue("id"));

    o.AddPolicy("DetectTargetsListPolicy", b => b.Cache().Expire(TimeSpan.FromMinutes(60))
   .Tag("DetectTargets").SetVaryByQuery("search", "pageNumber", "pageSize"));

    o.AddPolicy("DetectTargetsGetAllPolicy", b => b.Cache().Expire(TimeSpan.FromMinutes(60))
        .Tag("DetectTargets").SetVaryByQuery("search"));

    o.AddPolicy("CameraByIdPolicy", b => b.Cache().Expire(TimeSpan.FromMinutes(60))
        .Tag("DetectTargets").SetVaryByRouteValue("id"));

});

// Distributed cache via Redis (for application-level caching)
builder.Services.AddStackExchangeRedisCache(o =>
{
    o.Configuration = builder.Configuration["Redis:Configuration"];
    o.InstanceName = "ImageProcessing:";
});


builder.Services.Configure<InputSanitizationOptions>(o =>
{
    // o.MaxBodyBytesToInspect = 2_000_000;
    // o.PathExclusions.Add("/metrics");
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendDev", p =>
        p.WithOrigins(
             "http://localhost:5173",
             "http://127.0.0.1:5173")
         .AllowAnyMethod()
         .AllowAnyHeader()
         .WithExposedHeaders("X-Pagination")
    // don't use AllowCredentials unless you are sending cookies; you use Bearer tokens
    // .AllowCredentials()
    );
});


var app = builder.Build();



// Create / migrate the database at startup
// DB migrate + seed with basic retry
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    var db = services.GetRequiredService<AppDbContext>();

    const int maxRetries = 5;
    var attempt = 0;

    while (true)
    {
        try
        {
            // 1) Apply migrations (creates tables if missing)
            db.Database.Migrate();

            // 2) Seed users
            if (!db.Users.Any(u => u.Email == "admin@aransolution.com"))
            {
                db.Users.Add(new ImageProcessing.Domain.Entities.Users.User
                {
                    Email = "admin@aransolution.com",
                    Name = "Admin",
                    Role = Roles.Admin,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
                    CreatedUtc = DateTime.UtcNow
                });
            }

            if (!db.Users.Any(u => u.Email == "user@aransolution.com"))
            {
                db.Users.Add(new ImageProcessing.Domain.Entities.Users.User
                {
                    Email = "user@aransolution.com",
                    Name = "Regular User",
                    Role = Roles.User,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("P@ssw0rd!"),
                    CreatedUtc = DateTime.UtcNow
                });
            }

            db.SaveChanges();

            logger.LogInformation("Database migrated and seeded successfully.");
            break; // success
        }
        catch (MySqlException ex) when (attempt < maxRetries)
        {
            attempt++;
            logger.LogWarning(ex,
                "Database not ready yet (attempt {Attempt}/{Max}). Retrying in 5 seconds...",
                attempt, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fatal error during database migration/seed.");
            throw; // crash app if it's a real error
        }
    }
}


app.UseSerilogRequestLogging(opts =>
{
    opts.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    // Log 4xx/5xx at Warning, others at Information
    opts.GetLevel = (ctx, elapsed, ex) =>
        ex != null ? LogEventLevel.Error :
        ctx.Response.StatusCode >= 500 ? LogEventLevel.Error :
        ctx.Response.StatusCode >= 400 ? LogEventLevel.Warning :
        LogEventLevel.Information;

    // add a couple of useful properties
    opts.EnrichDiagnosticContext = (diag, http) =>
    {
        diag.Set("RequestHost", http.Request.Host.Value ?? "NULL");
        diag.Set("UserAgent", http.Request.Headers.UserAgent.ToString());
        diag.Set("QueryString", http.Request.QueryString.Value ?? "NULL");
    };
});



app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "ImageProcessing API v1");
});

var uploadPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), apiApp =>
{
    apiApp.UseMiddleware<InputSanitizationMiddleware>();

    apiApp.UseGlobalExceptionHandler();
    apiApp.UseOutputCache();
    apiApp.UseRateLimiter();
    apiApp.UseCors("FrontendDev");
});


app.MapControllers().RequireRateLimiting("global");

app.UseSerilogRequestLogging();

app.UseHangfireDashboard("/jobs");

var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Toronto");
RecurringJob.AddOrUpdate<ILogMaintenance>(
    recurringJobId: "logs-retain-90d-daily",
    methodCall: j => j.RetainAsync(90, CancellationToken.None),
    cronExpression: "30 2 * * *",
    options: new RecurringJobOptions { TimeZone = tz }
);

app.Run();

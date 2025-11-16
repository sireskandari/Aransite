using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using ImageProcessing.Application.Abstractions.Data;
using ImageProcessing.Application.Abstractions.Storage;
using ImageProcessing.Infrastructure.Persistence;
using ImageProcessing.Infrastructure.Storage;

namespace ImageProcessing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("Default")
                 ?? throw new InvalidOperationException("Missing ConnectionStrings:Default");

        services.AddDbContextPool<AppDbContext>(options =>
        {
            options.UseMySql(
                cs,
                ServerVersion.AutoDetect(cs),
                my => my
                    .MigrationsHistoryTable("__EFMigrationsHistory")
                    .SchemaBehavior(MySqlSchemaBehavior.Ignore)    
            );

            options.EnableDetailedErrors();
            // options.EnableSensitiveDataLogging(); // dev only
        });

        // Map EF context to your application port
        services.AddScoped<IAppDbContext, AppDbContext>();
        services.AddScoped<IFileService, FileService>();

        return services;
    }
}

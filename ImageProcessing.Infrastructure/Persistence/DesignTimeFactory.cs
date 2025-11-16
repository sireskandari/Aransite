using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace ImageProcessing.Infrastructure.Persistence;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Use env var if present, else local default
        var cs = Environment.GetEnvironmentVariable("MYAPP_MIGRATION_CS")
                 ?? "Server=127.0.0.1;Port=3307;Database=myapp;User=myapp;Password=myapppass;TreatTinyAsBoolean=false;DefaultCommandTimeout=60;SslMode=None;AllowPublicKeyRetrieval=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(cs, ServerVersion.AutoDetect(cs), my => my.MigrationsHistoryTable("__EFMigrationsHistory").SchemaBehavior(MySqlSchemaBehavior.Ignore))
            .Options;

        return new AppDbContext(options);
    }
}
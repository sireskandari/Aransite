using MySqlConnector;

public interface ILogMaintenance
{
    Task TruncateAsync(CancellationToken ct);
    Task RetainAsync(int days, CancellationToken ct);
}

public sealed class LogMaintenance : ILogMaintenance
{
    private readonly string _cs;
    public LogMaintenance(IConfiguration cfg)
        => _cs = cfg.GetConnectionString("Default")!;

    public async Task TruncateAsync(CancellationToken ct)
    {
        await using var conn = new MySqlConnection(_cs);
        await conn.OpenAsync(ct);
        await using var cmd = new MySqlCommand("TRUNCATE TABLE serilog_logs;", conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task RetainAsync(int days, CancellationToken ct)
    {
        // deletes old rows; then optimize to reclaim space
        await using var conn = new MySqlConnection(_cs);
        await conn.OpenAsync(ct);

        // 1) delete older than N days
        await using (var del = new MySqlCommand(
            "DELETE FROM serilog_logs WHERE TimeStamp < DATE_SUB(UTC_TIMESTAMP(), INTERVAL @days DAY);", conn))
        {
            del.Parameters.AddWithValue("@days", days);
            await del.ExecuteNonQueryAsync(ct);
        }

        // 2) optional: optimize (rebuilds/defrag table)
        await using (var opt = new MySqlCommand("OPTIMIZE TABLE serilog_logs;", conn))
        {
            await opt.ExecuteNonQueryAsync(ct);
        }
    }
}

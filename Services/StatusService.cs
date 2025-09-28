using ChatApp.Data;
using ChatApp.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Services;

public interface IStatusService
{
    Task<DbStatusInfo> GetDatabaseStatusAsync(CancellationToken ct = default);
}

public class StatusService : IStatusService
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<StatusService> _logger;
    private readonly IConfiguration _config;

    public StatusService(IDbContextFactory<AppDbContext> factory, ILogger<StatusService> logger, IConfiguration config)
    {
        _factory = factory; _logger = logger; _config = config;
    }

    public async Task<DbStatusInfo> GetDatabaseStatusAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var cs = _config.GetConnectionString("Default")!;
        var result = new DbStatusInfo { FilePath = GetFilePathFromConnectionString(cs) };

        // For SQLite we can query sqlite_master for table names
        await using var conn = new SqliteConnection(cs);
        await conn.OpenAsync(ct);

        var tables = new List<string>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'";
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                tables.Add(reader.GetString(0));
            }
        }
        result.TableCount = tables.Count;

        foreach (var tbl in tables)
        {
            await using var countCmd = conn.CreateCommand();
            countCmd.CommandText = $"SELECT COUNT(*) FROM '{tbl}'"; // table names controlled internally
            var count = (long?)await countCmd.ExecuteScalarAsync(ct) ?? 0;
            result.TableRowCounts[tbl] = (int)count;
            result.TotalRowCount += (int)count;
        }

        return result;
    }

    private static string GetFilePathFromConnectionString(string cs)
    {
        // typical format: Data Source=app.db
        var parts = cs.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var p in parts)
        {
            var kv = p.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals("Data Source", StringComparison.OrdinalIgnoreCase))
                return kv[1];
        }
        return cs;
    }
}

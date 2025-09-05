using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChatApp.Core.Services;

public record EfDiagnostics(string Provider, string? LatestMigration, int AppliedCount, string? SqliteVersion);

public interface IEFDiagnosticsProvider
{
    Task<EfDiagnostics> CollectAsync(CancellationToken ct = default);
}

public sealed class EFDiagnosticsProvider : IEFDiagnosticsProvider
{
    private readonly IServiceProvider _sp;
    private readonly ILogger<EFDiagnosticsProvider> _logger;
    public EFDiagnosticsProvider(IServiceProvider sp, ILogger<EFDiagnosticsProvider> logger)
    { _sp = sp; _logger = logger; }

    public async Task<EfDiagnostics> CollectAsync(CancellationToken ct = default)
    {
        string provider = string.Empty; string? latest = null; int count = 0; string? sqlite = null;
        try
        {
            using var scope = _sp.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Data.AppDbContext>>();
            await using var db = await factory.CreateDbContextAsync(ct);
            provider = db.Database.ProviderName ?? string.Empty;
            var migrations = (await db.Database.GetAppliedMigrationsAsync(ct)).ToList();
            count = migrations.Count;
            latest = migrations.LastOrDefault();

            if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var list = await db.Database.SqlQueryRaw<string>("select sqlite_version() as v").ToListAsync(ct);
                    sqlite = list.FirstOrDefault();
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to query sqlite_version()");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect EF diagnostics");
        }
        return new EfDiagnostics(provider, latest, count, sqlite);
    }
}

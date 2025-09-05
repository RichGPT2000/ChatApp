using ChatApp.Core.Data;
using ChatApp.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ChatApp.Tests;

public class EFDiagnosticsProviderTests
{
    [Fact]
    public async Task CollectAsync_with_sqlite_returns_sqlite_version()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"testdb_{Guid.NewGuid():N}.db");
        services.AddDbContextFactory<AppDbContext>(o => o.UseSqlite($"Data Source={dbPath}"));
        services.AddSingleton<IEFDiagnosticsProvider, EFDiagnosticsProvider>();

        using var sp = services.BuildServiceProvider();

        // ensure database created
        var factory = sp.GetRequiredService<IDbContextFactory<AppDbContext>>();
        await using (var db = await factory.CreateDbContextAsync())
        {
            await db.Database.EnsureCreatedAsync();
        }

        var prov = sp.GetRequiredService<IEFDiagnosticsProvider>();
        var diag = await prov.CollectAsync();

        Assert.Contains("Sqlite", diag.Provider, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(diag.SqliteVersion); // should have attempted the query
    }

    [Fact]
    public async Task CollectAsync_with_inmemory_has_no_sqlite_version()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextFactory<AppDbContext>(o => o.UseInMemoryDatabase($"mem-{Guid.NewGuid():N}"));
        services.AddSingleton<IEFDiagnosticsProvider, EFDiagnosticsProvider>();

        using var sp = services.BuildServiceProvider();
        var prov = sp.GetRequiredService<IEFDiagnosticsProvider>();
        var diag = await prov.CollectAsync();

        Assert.Contains("InMemory", diag.Provider, StringComparison.OrdinalIgnoreCase);
        Assert.Null(diag.SqliteVersion); // should not attempt sqlite_version
    }
}

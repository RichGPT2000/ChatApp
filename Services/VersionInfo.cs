using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

namespace ChatApp.Services;

public class VersionInfo
{
    public string AppVersion { get; set; } = "";               // AssemblyInformationalVersion
    public string FileVersion { get; set; } = "";              // AssemblyFileVersion
    public string? Commit { get; set; }                         // short git commit if provided

    public string RuntimeVersion { get; set; } = "";           // .NET runtime
    public string OSDescription { get; set; } = "";            // OS
    public string ProcessArchitecture { get; set; } = "";      // x64/arm64

    public string EnvironmentName { get; set; } = "";          // Development/Production

    public string EFProvider { get; set; } = "";               // e.g., Microsoft.EntityFrameworkCore.Sqlite
    public string? LatestMigration { get; set; }                // last applied migration id
    public int AppliedMigrations { get; set; }                  // count

    public string? SqliteVersion { get; set; }                  // SQLite engine version
}

public interface IVersionInfoProvider
{
    VersionInfo? Current { get; }
    Task InitializeAsync(CancellationToken ct = default);
}

public class VersionInfoProvider : IVersionInfoProvider
{
    private readonly IServiceProvider _sp;
    private readonly IConfiguration _cfg;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<VersionInfoProvider> _logger;

    public VersionInfoProvider(IServiceProvider sp, IConfiguration cfg, IWebHostEnvironment env, ILogger<VersionInfoProvider> logger)
    {
        _sp = sp; _cfg = cfg; _env = env; _logger = logger;
    }

    public VersionInfo? Current { get; private set; }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (Current is not null) return;

        var asm = typeof(Program).Assembly;
        var informational = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "";

        var commit = _cfg["Build:Commit"] ?? Environment.GetEnvironmentVariable("GIT_COMMIT");
        if (!string.IsNullOrEmpty(commit) && commit.Length > 7) commit = commit[..7];

        var runtime = Environment.Version.ToString();
        var os = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();

        string provider = ""; string? latestMigration = null; int appliedCount = 0; string? sqliteVer = null;
        try
        {
            using var scope = _sp.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<Data.AppDbContext>>();
            await using var db = await factory.CreateDbContextAsync(ct);
            provider = db.Database.ProviderName ?? "";
            var migrations = (await db.Database.GetAppliedMigrationsAsync(ct)).ToList();
            appliedCount = migrations.Count;
            latestMigration = migrations.LastOrDefault();

            try
            {
                var list = await db.Database.SqlQueryRaw<string>("select sqlite_version() as v").ToListAsync(ct);
                sqliteVer = list.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to query sqlite_version()");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect EF/SQLite diagnostics");
        }

        Current = new VersionInfo
        {
            AppVersion = informational,
            FileVersion = file,
            Commit = commit,
            RuntimeVersion = runtime,
            OSDescription = os,
            ProcessArchitecture = arch,
            EnvironmentName = _env.EnvironmentName,
            EFProvider = provider,
            LatestMigration = latestMigration,
            AppliedMigrations = appliedCount,
            SqliteVersion = sqliteVer
        };
    }
}

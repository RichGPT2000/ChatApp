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
    Task<VersionInfo> GetOrCreateAsync(CancellationToken ct = default);
    Task<VersionInfo> RefreshAsync(CancellationToken ct = default);
}

public class VersionInfoProvider : IVersionInfoProvider
{
    private readonly IAssemblyInfoProvider _asm;
    private readonly IRuntimeInfoProvider _runtime;
    private readonly IWebHostEnvironment _env;
    private readonly IEFDiagnosticsProvider _ef;
    private readonly ICommitProvider _commit;
    private readonly ILogger<VersionInfoProvider> _logger;

    public VersionInfoProvider(IAssemblyInfoProvider asm,
        IRuntimeInfoProvider runtime,
        IWebHostEnvironment env,
        IEFDiagnosticsProvider ef,
        ICommitProvider commit,
        ILogger<VersionInfoProvider> logger)
    {
        _asm = asm; _runtime = runtime; _env = env; _ef = ef; _commit = commit; _logger = logger;
    }

    public VersionInfo? Current { get; private set; }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        // Backwards compatibility
        await GetOrCreateAsync(ct);
    }

    public async Task<VersionInfo> GetOrCreateAsync(CancellationToken ct = default)
    {
        if (Current is not null) return Current;
        return Current = await BuildAsync(ct);
    }

    public async Task<VersionInfo> RefreshAsync(CancellationToken ct = default)
    {
        Current = await BuildAsync(ct);
        return Current;
    }

    private async Task<VersionInfo> BuildAsync(CancellationToken ct)
    {
        EfDiagnostics efdiag = await _ef.CollectAsync(ct);
        var vi = new VersionInfo
        {
            AppVersion = _asm.GetInformationalVersion(),
            FileVersion = _asm.GetFileVersion(),
            Commit = _commit.GetShortCommit(),
            RuntimeVersion = _runtime.GetRuntimeVersion(),
            OSDescription = _runtime.GetOSDescription(),
            ProcessArchitecture = _runtime.GetProcessArchitecture(),
            EnvironmentName = _env.EnvironmentName,
            EFProvider = efdiag.Provider,
            LatestMigration = efdiag.LatestMigration,
            AppliedMigrations = efdiag.AppliedCount,
            SqliteVersion = efdiag.SqliteVersion
        };
        _logger.LogDebug("VersionInfo built: {@VersionInfo}", vi);
        return vi;
    }
}

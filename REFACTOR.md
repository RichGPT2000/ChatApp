# VersionInfo Refactor Plan

Goal: Improve testability, separation of concerns, and extensibility of VersionInfoProvider while keeping existing behavior for consumers.

## Overview of Issues
Current VersionInfoProvider directly accesses:
- Assembly metadata via typeof(Program).Assembly (hard to fake)
- Environment & runtime static APIs (Environment.Version, RuntimeInformation.*, env vars)
- EF Core diagnostics & raw SQL (mixed responsibility)
- Caches result in mutable property `Current` with one-shot init method

This hampers unit testing and future extension (e.g., adding refresh, alternative DB providers, additional runtime facts).

## Incremental Commit Plan

### Commit 1: Introduce system abstractions
Add interfaces:
- IAssemblyInfoProvider (InformationalVersion, FileVersion)
- IRuntimeInfoProvider (RuntimeVersion, OSDescription, ProcessArchitecture)
- IEnvironmentReader (Get(string key))
Register concrete implementations in DI.

### Commit 2: Extract EF diagnostics
Add IEFDiagnosticsProvider with:
```
Task<EfDiagnostics> CollectAsync(CancellationToken ct);
record EfDiagnostics(string Provider, string? LatestMigration, int AppliedCount, string? SqliteVersion);
```
Move EF querying logic there. Conditional sqlite_version() query only when provider contains "Sqlite".

### Commit 3: Introduce commit provider
ICommitProvider resolves and shortens commit:
Order: IConfiguration["Build:Commit"] -> env var GIT_COMMIT -> null. Shorten to 7 chars if longer.

### Commit 4: Reshape VersionInfoProvider
Inject new abstractions and compose them. Remove direct static calls. Keep InitializeAsync for backward compatibility but internally call new GetOrCreateAsync.

### Commit 5: Add refresh capability
Add:
```
Task<VersionInfo> GetOrCreateAsync(CancellationToken ct = default);
Task<VersionInfo> RefreshAsync(CancellationToken ct = default);
```
InitializeAsync becomes wrapper of GetOrCreateAsync.

### Commit 6: Add VersionInfoFactory
Pure function/service that builds VersionInfo from all collected parts (assembly, commit, runtime, env, EF). Simplifies testing to verifying output with deterministic inputs.

### Commit 7: Unit tests
Add test project if absent.
Tests:
- CommitProvider shortening & precedence
- AssemblyInfoProvider (can fake with test double)
- EF diagnostics provider using in-memory DbContext & a fake provider name for sqlite, verifying conditional query
- VersionInfoProvider end-to-end with all fakes
- RefreshAsync produces new instance

### Commit 8: Blazor integration adjustments
Add DI extension method `services.AddVersionInfo()` that registers all pieces as singletons. Optionally pre-warm in Program.cs (await provider.GetOrCreateAsync()). Provide a component or page consumption example (maybe version footer).

### Commit 9: Logging improvements
Structured log messages with event IDs for EF diagnostics failures and sqlite version query fallback (Debug level). Keep warnings only for total EF diagnostics failure.

### Commit 10: Documentation
Update README & this REFACTOR.md with usage examples, test instructions, and extension points.

### Commit 11: Cleanup
Remove obsolete comments/usages. Possibly mark InitializeAsync as [Obsolete("Use GetOrCreateAsync")] in a later pass.

## Data Structures
```
public record EfDiagnostics(string Provider, string? LatestMigration, int AppliedCount, string? SqliteVersion);
```

## DI Lifetime Decisions
- Assembly/runtime/env/commit providers: singleton (pure, stateless)
- EF diagnostics provider: scoped usage but registered as singleton using factory scopes internally (like current pattern)
- VersionInfoProvider: singleton (cached value; Refresh creates new internal value)

## Testing Strategy
Use fakes rather than heavy mocks where possible (small hand-written classes). For EF: use InMemory provider to simulate applied migrations; add extra migration assembly if needed or manually seed IMigrationsAssembly.

## Risks & Mitigations
- Over-refactor: keep public contract stable initially.
- Breaking API: do not remove InitializeAsync until consumers migrated.
- Performance: minimal impact; added indirection negligible.

## Future Enhancements (Not in scope of main refactor)
- Endpoint /version or /health/version returning VersionInfo JSON
- Periodic refresh with IHostedService + configurable interval
- Expose metrics (OpenTelemetry) for initialization duration

## Example Usage After Refactor
```
var vi = await versionInfoProvider.GetOrCreateAsync();
Console.WriteLine($"App {vi.AppVersion} ({vi.Commit}) running on {vi.OSDescription} {vi.ProcessArchitecture}");
```

---
Status: Pending implementation.

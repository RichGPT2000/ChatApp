using ChatApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ChatApp.Tests;

public class FakeAssemblyInfoProvider : IAssemblyInfoProvider
{
    public string GetInformationalVersion() => "1.2.3-test+meta";
    public string GetFileVersion() => "1.2.3.0";
}
public class FakeRuntimeInfoProvider : IRuntimeInfoProvider
{
    public string GetRuntimeVersion() => "9.9.9";
    public string GetOSDescription() => "UnitTestOS 1.0";
    public string GetProcessArchitecture() => "xTEST";
}
public class FakeEnvironmentReader : IEnvironmentReader
{
    private readonly Dictionary<string,string?> _vals = new();
    public void Set(string k, string? v) => _vals[k] = v;
    public string? Get(string key) => _vals.TryGetValue(key, out var v) ? v : null;
}

public class VersionInfoTests
{
    [Fact]
    public void CommitProvider_precedence_and_shortening()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{ {"Build:Commit", "123456789abcdef"} }).Build();
        var env = new FakeEnvironmentReader();
        env.Set("GIT_COMMIT", "ignoredenv");
        var cp = new CommitProvider(cfg, env);
        Assert.Equal("1234567", cp.GetShortCommit());
    }

    [Fact]
    public void CommitProvider_falls_back_to_env()
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>()).Build();
        var env = new FakeEnvironmentReader();
        env.Set("GIT_COMMIT", "abcdef123456");
        var cp = new CommitProvider(cfg, env);
        Assert.Equal("abcdef1", cp.GetShortCommit());
    }

    [Fact]
    public async Task VersionInfoProvider_builds_expected_values()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAssemblyInfoProvider, FakeAssemblyInfoProvider>();
        services.AddSingleton<IRuntimeInfoProvider, FakeRuntimeInfoProvider>();
        var envReader = new FakeEnvironmentReader();
        services.AddSingleton<IEnvironmentReader>(envReader);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>()).Build();
        services.AddSingleton<IConfiguration>(cfg);
        services.AddLogging();
        services.AddSingleton<ICommitProvider, CommitProvider>();
        // EF diagnostics fake
        services.AddSingleton<IEFDiagnosticsProvider>(new FakeEfDiag());
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(new FakeHostEnv());
        services.AddSingleton<IVersionInfoProvider, VersionInfoProvider>();

        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IVersionInfoProvider>();
        var vi = await provider.GetOrCreateAsync();

        Assert.Equal("1.2.3-test+meta", vi.AppVersion);
        Assert.Equal("1.2.3.0", vi.FileVersion);
        Assert.Equal("UnitTestOS 1.0", vi.OSDescription);
        Assert.Equal("xTEST", vi.ProcessArchitecture);
        Assert.Equal("TestEnv", vi.EnvironmentName);
        Assert.Equal("Fake.Provider", vi.EFProvider);
        Assert.Equal("202401010101_AddX", vi.LatestMigration);
        Assert.Equal(5, vi.AppliedMigrations);
        Assert.Equal("3.42.0", vi.SqliteVersion);
    }

    [Fact]
    public async Task Refresh_updates_instance()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IAssemblyInfoProvider, FakeAssemblyInfoProvider>();
        services.AddSingleton<IRuntimeInfoProvider, FakeRuntimeInfoProvider>();
        var envReader = new FakeEnvironmentReader();
        services.AddSingleton<IEnvironmentReader>(envReader);
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>()).Build();
        services.AddSingleton<IConfiguration>(cfg);
        services.AddLogging();
        services.AddSingleton<ICommitProvider, CommitProvider>();
        services.AddSingleton<IEFDiagnosticsProvider>(new FakeEfDiag());
        services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(new FakeHostEnv());
        services.AddSingleton<IVersionInfoProvider, VersionInfoProvider>();

        var sp = services.BuildServiceProvider();
        var provider = sp.GetRequiredService<IVersionInfoProvider>();
        var first = await provider.GetOrCreateAsync();
        var second = await provider.RefreshAsync();
        Assert.NotSame(first, second);
    }

    private class FakeEfDiag : IEFDiagnosticsProvider
    {
        public Task<EfDiagnostics> CollectAsync(CancellationToken ct = default) =>
            Task.FromResult(new EfDiagnostics("Fake.Provider", "202401010101_AddX", 5, "3.42.0"));
    }

    private class FakeHostEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "TestApp";
        public IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "TestEnv";
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}

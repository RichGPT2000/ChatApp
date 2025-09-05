using Microsoft.Extensions.Configuration;

namespace ChatApp.Core.Services;

public interface ICommitProvider
{
    string? GetShortCommit();
}

public sealed class CommitProvider : ICommitProvider
{
    private readonly IConfiguration _cfg;
    private readonly IEnvironmentReader _env;
    public CommitProvider(IConfiguration cfg, IEnvironmentReader env) { _cfg = cfg; _env = env; }

    public string? GetShortCommit()
    {
        var raw = _cfg["Build:Commit"] ?? _env.Get("GIT_COMMIT");
        if (string.IsNullOrWhiteSpace(raw)) return null;
        raw = raw.Trim();
        return raw.Length > 7 ? raw[..7] : raw;
    }
}

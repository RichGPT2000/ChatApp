using System.Reflection;

namespace ChatApp.Services;

public interface IAssemblyInfoProvider
{
    string GetInformationalVersion();
    string GetFileVersion();
}

public sealed class AssemblyInfoProvider : IAssemblyInfoProvider
{
    private readonly Assembly _assembly;
    public AssemblyInfoProvider() : this(typeof(Program).Assembly) { }
    public AssemblyInfoProvider(Assembly assembly) => _assembly = assembly;

    public string GetInformationalVersion() => _assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
    public string GetFileVersion() => _assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? string.Empty;
}

public interface IRuntimeInfoProvider
{
    string GetRuntimeVersion();
    string GetOSDescription();
    string GetProcessArchitecture();
}

public sealed class RuntimeInfoProvider : IRuntimeInfoProvider
{
    public string GetRuntimeVersion() => Environment.Version.ToString();
    public string GetOSDescription() => System.Runtime.InteropServices.RuntimeInformation.OSDescription;
    public string GetProcessArchitecture() => System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString();
}

public interface IEnvironmentReader
{
    string? Get(string key);
}

public sealed class EnvironmentReader : IEnvironmentReader
{
    public string? Get(string key) => Environment.GetEnvironmentVariable(key);
}

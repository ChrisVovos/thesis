using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace ItemAuthoring.Integration.Tests.Infrastructure;

/// <summary>
/// A fact that is skipped, rather than failed, when no container runtime is reachable.
/// </summary>
/// <remarks>
/// The integration suite is written against a real SQL Server because an in-memory provider would
/// not exercise the very things these tests exist to prove — value converters, table-per-hierarchy
/// discriminators, unique indexes and query translation. A developer without Docker should still be
/// able to run <c>dotnet test</c> and get a meaningful result from the other suites, so these tests
/// report as skipped instead of red. Continuous integration always has a runtime, so nothing is
/// silently lost there.
/// </remarks>
public sealed class RequiresDockerFactAttribute : FactAttribute
{
    /// <summary>Initializes a new instance of the <see cref="RequiresDockerFactAttribute"/> class.</summary>
    public RequiresDockerFactAttribute()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Skip = "No container runtime is reachable; the integration suite requires Docker.";
        }
    }
}

/// <summary>
/// A theory that is skipped, rather than failed, when no container runtime is reachable.
/// </summary>
public sealed class RequiresDockerTheoryAttribute : TheoryAttribute
{
    /// <summary>Initializes a new instance of the <see cref="RequiresDockerTheoryAttribute"/> class.</summary>
    public RequiresDockerTheoryAttribute()
    {
        if (!ContainerRuntime.IsAvailable)
        {
            Skip = "No container runtime is reachable; the integration suite requires Docker.";
        }
    }
}

/// <summary>Probes for a reachable container runtime exactly once per test run.</summary>
public static class ContainerRuntime
{
    private static readonly Lazy<bool> Probe = new(Detect, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>Gets a value indicating whether a container runtime responded.</summary>
    public static bool IsAvailable => Probe.Value;

    private static bool Detect()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return File.Exists(@"\\.\pipe\docker_engine")
                    || Directory.EnumerateFiles(@"\\.\pipe\").Any(pipe =>
                        pipe.Contains("docker", StringComparison.OrdinalIgnoreCase));
            }

            using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            socket.Connect(new UnixDomainSocketEndPoint("/var/run/docker.sock"));
            return socket.Connected;
        }
        catch (Exception exception) when (exception is IOException or SocketException
            or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}

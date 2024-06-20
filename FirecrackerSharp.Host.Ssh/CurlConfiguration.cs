namespace FirecrackerSharp.Host.Ssh;

/// <summary>
/// The configuration options for communicating with Management API UDS sockets through the curl tool.
/// </summary>
/// <param name="Timeout">A <see cref="TimeSpan"/> since the send-out of a request, after which that request is
/// terminated and considered "timed out"</param>
public sealed record CurlConfiguration(
    TimeSpan Timeout)
{
    /// <summary>
    /// A "sensible default" <see cref="CurlConfiguration"/> that fits most usage scenarios, with a 30-second timeout
    /// and a 0.1-second poll frequency.
    /// </summary>
    public static readonly CurlConfiguration Default = new(
        Timeout: TimeSpan.FromSeconds(30));
}
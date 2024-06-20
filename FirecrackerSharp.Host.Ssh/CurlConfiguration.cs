namespace FirecrackerSharp.Host.Ssh;

/// <summary>
/// The configuration options for communicating with Management API UDS sockets through the curl tool.
/// </summary>
/// <param name="PollFrequency">The frequency at which SSH should be polled to see if the command has gone through</param>
/// <param name="RetryAmount">The --retry curl argument</param>
/// <param name="RetryDelaySeconds">The --retry-delay curl argument</param>
/// <param name="RetryMaxTimeSeconds">The --retry-max-time-seconds curl argument</param>
public sealed record CurlConfiguration(
    TimeSpan PollFrequency,
    int RetryAmount,
    int RetryDelaySeconds,
    int RetryMaxTimeSeconds)
{
    /// <summary>
    /// A "sensible default" <see cref="CurlConfiguration"/> that fits most usage scenarios.
    /// </summary>
    public static readonly CurlConfiguration Default = new(TimeSpan.FromMilliseconds(5), 1, 0, 20);
}
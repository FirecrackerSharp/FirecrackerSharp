namespace FirecrackerSharp.Host.Ssh;

public record CurlConfiguration(
    TimeSpan Timeout,
    TimeSpan PollFrequency)
{
    public static readonly CurlConfiguration Default = new(
        Timeout: TimeSpan.FromSeconds(30),
        PollFrequency: TimeSpan.FromMilliseconds(100));
}
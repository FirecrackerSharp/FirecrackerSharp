namespace FirecrackerSharp.Host.Ssh;

public record CurlConfiguration(
    TimeSpan Timeout,
    TimeSpan PollFrequency);
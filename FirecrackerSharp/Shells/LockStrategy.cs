namespace FirecrackerSharp.Shells;

public record LockStrategy(
    LockStrategyType Type,
    TimeSpan PollFrequency,
    TimeSpan? Timeout = null)
{
    public static readonly LockStrategy Default = new(
        LockStrategyType.WaitWithTimeout,
        TimeSpan.FromMilliseconds(10),
        TimeSpan.FromSeconds(10));
}
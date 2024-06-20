namespace FirecrackerSharp.Core;

/// <summary>
/// The options for various delays and timeouts that are optional/necessary in the microVM lifecycle.
/// </summary>
/// <param name="DelayForBoot">The optional but highly recommended delay for the boot to finish</param>
/// <param name="DelayBeforeBootApiRequests">The optional delay before issuing boot API requests</param>
/// <param name="TimeoutForBootApiRequests">The total timeout for all boot API requests</param>
/// <param name="TimeoutForShutdown">The timeout for shutdown operations</param>
public sealed record WaitOptions(
    TimeSpan? DelayForBoot,
    TimeSpan? DelayBeforeBootApiRequests,
    TimeSpan TimeoutForBootApiRequests,
    TimeSpan TimeoutForShutdown)
{
    public static readonly WaitOptions Default = new(
        DelayForBoot: TimeSpan.FromSeconds(2),
        DelayBeforeBootApiRequests: TimeSpan.FromMilliseconds(150),
        TimeoutForBootApiRequests: TimeSpan.FromSeconds(10),
        TimeoutForShutdown: TimeSpan.FromSeconds(30));
}
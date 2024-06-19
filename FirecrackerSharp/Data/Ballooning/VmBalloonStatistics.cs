namespace FirecrackerSharp.Data.Ballooning;

public sealed record VmBalloonStatistics(
    int TargetPages,
    int ActualPages,
    int TargetMib,
    int ActualMib,
    long? SwapIn = null,
    long? SwapOut = null,
    long? MajorFaults = null,
    long? MinorFaults = null,
    long? FreeMemory = null,
    long? TotalMemory = null,
    long? AvailableMemory = null,
    long? DiskCaches = null,
    long? HugetlbAllocations = null,
    long? HugetlbFailures = null);

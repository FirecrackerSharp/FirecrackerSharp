namespace FirecrackerSharp.Core;

public static class VmShutdownResultExtensions
{
    public static bool IsSuccessful(this VmShutdownResult shutdownResult)
        => shutdownResult == VmShutdownResult.Successful;

    public static bool IsSoftFailure(this VmShutdownResult shutdownResult)
        => shutdownResult == VmShutdownResult.SoftFailedDuringCleanup;
    
    public static bool IsFailure(this VmShutdownResult shutdownResult)
        => shutdownResult != VmShutdownResult.Successful;
}
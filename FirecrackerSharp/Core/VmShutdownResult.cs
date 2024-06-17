namespace FirecrackerSharp.Core;

public enum VmShutdownResult
{
    Successful,
    SoftFailedDuringCleanup,
    FailedDueToBrokenPipe,
    FailedDueToHangingProcess,
    FailedDueToTtyNotResponding
}
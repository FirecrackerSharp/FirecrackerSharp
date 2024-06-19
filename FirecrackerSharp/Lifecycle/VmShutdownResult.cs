namespace FirecrackerSharp.Lifecycle;

public enum VmShutdownResult
{
    Successful,
    SoftFailedDuringCleanup,
    FailedDueToBrokenPipe,
    FailedDueToHangingProcess,
    FailedDueToTtyNotResponding
}
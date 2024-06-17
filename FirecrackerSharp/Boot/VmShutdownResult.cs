namespace FirecrackerSharp.Boot;

public enum VmShutdownResult
{
    Successful,
    SoftFailedDuringCleanup,
    FailedDueToBrokenPipe,
    FailedDueToHangingProcess,
    FailedDueToTtyNotResponding
}
namespace FirecrackerSharp.Data;

public record VmNetworkInterface(
    string HostDevName,
    string IfaceId,
    string? GuestMac = null,
    VmRateLimiter? RxRateLimiter = null,
    VmRateLimiter? TxRateLimiter = null);

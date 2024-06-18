namespace FirecrackerSharp.Data;

public sealed record VmNetworkInterfaceUpdate(
    string IfaceId,
    VmRateLimiter RxRateLimiter,
    VmRateLimiter TxRateLimiter);

namespace FirecrackerSharp.Data;

public sealed record VmRateLimiter(
    VmTokenBucket Bandwidth,
    VmTokenBucket Ops);

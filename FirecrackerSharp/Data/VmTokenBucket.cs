namespace FirecrackerSharp.Data;

public sealed record VmTokenBucket(
    int RefillTime,
    int Size,
    int? OneTimeBurst = null);
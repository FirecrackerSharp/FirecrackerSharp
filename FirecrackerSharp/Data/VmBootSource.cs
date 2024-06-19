namespace FirecrackerSharp.Data;

public sealed record VmBootSource(
    string KernelImagePath,
    string? BootArgs = null,
    string? InitrdPath = null);
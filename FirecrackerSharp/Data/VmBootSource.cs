namespace FirecrackerSharp.Data;

public record VmBootSource(
    string KernelImagePath,
    string? BootArgs = null,
    string? InitrdPath = null);
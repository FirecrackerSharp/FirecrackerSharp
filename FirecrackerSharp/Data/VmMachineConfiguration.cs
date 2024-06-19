namespace FirecrackerSharp.Data;

public sealed record VmMachineConfiguration(
    int MemSizeMib,
    int VcpuCount,
    bool Smt = false,
    bool TrackDirtyPages = false,
    string HugePages = "None");
namespace FirecrackerSharp.Data;

public record VmMachineConfiguration(
    int MemSizeMib,
    int VcpuCount,
    bool Smt = false,
    bool TrackDirtyPages = false,
    string HugePages = "None");
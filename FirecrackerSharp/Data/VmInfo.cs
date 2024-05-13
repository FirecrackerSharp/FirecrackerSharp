namespace FirecrackerSharp.Data;

public record VmInfo(
    string AppName,
    string Id,
    VmState State,
    string VmmVersion);
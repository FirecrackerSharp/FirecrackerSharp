namespace FirecrackerSharp.Data.State;

public record VmInfo(
    string AppName,
    string Id,
    VmState State,
    string VmmVersion);

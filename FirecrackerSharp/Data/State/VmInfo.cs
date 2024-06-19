namespace FirecrackerSharp.Data.State;

public sealed record VmInfo(
    string AppName,
    string Id,
    VmState State,
    string VmmVersion);

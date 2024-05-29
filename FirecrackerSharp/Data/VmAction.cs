namespace FirecrackerSharp.Data;

public record VmAction(
    VmActionType ActionType);

public enum VmActionType
{
    FlushMetrics,
    SendCtrlAltDel
}
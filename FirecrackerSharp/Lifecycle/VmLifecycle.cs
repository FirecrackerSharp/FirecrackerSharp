using FirecrackerSharp.Core;

namespace FirecrackerSharp.Lifecycle;

public class VmLifecycle
{
    public ILogTarget BootLogTarget { internal get; set; } = ILogTarget.Null;
    public ILogTarget ActiveLogTarget { internal get; set; } = ILogTarget.Null;
    public ILogTarget ShutdownLogTarget { internal get; set; } = ILogTarget.Null;
    public VmLifecyclePhase CurrentPhase { get; internal set; } = VmLifecyclePhase.Boot;

    public bool IsNotActive => CurrentPhase != VmLifecyclePhase.Active;

    public void SetAll(ILogTarget logTarget)
    {
        BootLogTarget = logTarget;
        ActiveLogTarget = logTarget;
        ShutdownLogTarget = logTarget;
    }
}
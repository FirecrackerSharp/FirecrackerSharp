namespace FirecrackerSharp.Core;

public class VmLifecycle
{
    public ILogTarget BootLogTarget { internal get; set; } = ILogTarget.Null;
    public ILogTarget ActiveLogTarget { internal get; set; } = ILogTarget.Null;
    public ILogTarget ShutdownLogTarget { internal get; set; } = ILogTarget.Null;
    public VmLifecyclePhase CurrentPhase { get; internal set; } = VmLifecyclePhase.Boot;

    public void SetAll(ILogTarget logTarget)
    {
        BootLogTarget = logTarget;
        ActiveLogTarget = logTarget;
        ShutdownLogTarget = logTarget;
    }
}
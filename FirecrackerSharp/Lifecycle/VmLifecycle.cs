namespace FirecrackerSharp.Lifecycle;

public class VmLifecycle
{
    private VmLifecyclePhase _currentPhase = VmLifecyclePhase.PreBoot;
    
    internal ILogTarget BootLogTarget = ILogTarget.Null;
    internal ILogTarget ActiveLogTarget = ILogTarget.Null;
    internal ILogTarget ShutdownLogTarget = ILogTarget.Null;
    
    public VmLifecyclePhase CurrentPhase
    {
        get => _currentPhase;
        internal set
        {
            PhaseFinished?.Invoke(sender: this, _currentPhase);
            _currentPhase = value;
            PhaseStarted?.Invoke(sender: this, _currentPhase);
        }
    }

    public event EventHandler<VmLifecyclePhase>? PhaseStarted;
    public event EventHandler<VmLifecyclePhase>? PhaseFinished;

    public bool IsNotActive => CurrentPhase != VmLifecyclePhase.Active;

    internal VmLifecycle()
    {
        CurrentPhase = VmLifecyclePhase.PreBoot;
    }

    public void AttachLogTarget(VmLifecyclePhase lifecyclePhase, ILogTarget logTarget)
    {
        switch (lifecyclePhase)
        {
            case VmLifecyclePhase.PreBoot:
                throw new NotAccessibleDueToLifecycleException("Cannot attach a log target to the pre-boot phase");
            case VmLifecyclePhase.Boot:
                BootLogTarget = logTarget;
                break;
            case VmLifecyclePhase.Active:
                ActiveLogTarget = logTarget;
                break;
            case VmLifecyclePhase.Shutdown:
                ShutdownLogTarget = logTarget;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifecyclePhase), lifecyclePhase, null);
        }
    }

    public void AttachAllLogTargets(ILogTarget bootLogTarget, ILogTarget activeLogTarget, ILogTarget shutdownLogTarget)
    {
        BootLogTarget = bootLogTarget;
        ActiveLogTarget = activeLogTarget;
        ShutdownLogTarget = shutdownLogTarget;
    }

    public void AttachAllLogTargetsToSingle(ILogTarget logTarget)
    {
        BootLogTarget = logTarget;
        ActiveLogTarget = logTarget;
        ShutdownLogTarget = logTarget;
    }

    public void DetachLogTarget(VmLifecyclePhase lifecyclePhase)
    {
        switch (lifecyclePhase)
        {
            case VmLifecyclePhase.PreBoot:
                throw new NotAccessibleDueToLifecycleException("Cannot detach a log target from the pre-boot phase");
            case VmLifecyclePhase.Boot:
                BootLogTarget = ILogTarget.Null;
                break;
            case VmLifecyclePhase.Active:
                ActiveLogTarget = ILogTarget.Null;
                break;
            case VmLifecyclePhase.Shutdown:
                ShutdownLogTarget = ILogTarget.Null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lifecyclePhase), lifecyclePhase, null);
        }
    }

    public void DetachAllLogTargets()
    {
        BootLogTarget = ILogTarget.Null;
        ActiveLogTarget = ILogTarget.Null;
        ShutdownLogTarget = ILogTarget.Null;
    }

    internal void FinishLastPhase()
    {
        PhaseFinished?.Invoke(sender: this, _currentPhase);
    }
}
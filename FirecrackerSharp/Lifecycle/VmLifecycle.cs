namespace FirecrackerSharp.Lifecycle;

public sealed class VmLifecycle
{
    private VmLifecyclePhase _currentPhase = VmLifecyclePhase.NotBooted;
    
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

    internal bool IsNotActive => CurrentPhase != VmLifecyclePhase.Active;

    internal VmLifecycle()
    {
        CurrentPhase = VmLifecyclePhase.NotBooted;
    }

    public void AttachLogTarget(LoggedVmLifecyclePhase lifecyclePhase, ILogTarget logTarget)
    {
        switch (lifecyclePhase)
        {
            case LoggedVmLifecyclePhase.Boot:
                BootLogTarget = logTarget;
                break;
            case LoggedVmLifecyclePhase.Active:
                ActiveLogTarget = logTarget;
                break;
            case LoggedVmLifecyclePhase.Shutdown:
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

    public bool IsLogTargetAttached(LoggedVmLifecyclePhase lifecyclePhase)
    {
        return lifecyclePhase switch
        {
            LoggedVmLifecyclePhase.Boot => BootLogTarget != ILogTarget.Null,
            LoggedVmLifecyclePhase.Active => ActiveLogTarget != ILogTarget.Null,
            LoggedVmLifecyclePhase.Shutdown => ShutdownLogTarget != ILogTarget.Null,
            _ => throw new ArgumentOutOfRangeException(nameof(lifecyclePhase), lifecyclePhase, null)
        };
    }

    public void DetachLogTarget(LoggedVmLifecyclePhase lifecyclePhase)
    {
        switch (lifecyclePhase)
        {
            case LoggedVmLifecyclePhase.Boot:
                BootLogTarget = ILogTarget.Null;
                break;
            case LoggedVmLifecyclePhase.Active:
                ActiveLogTarget = ILogTarget.Null;
                break;
            case LoggedVmLifecyclePhase.Shutdown:
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
}
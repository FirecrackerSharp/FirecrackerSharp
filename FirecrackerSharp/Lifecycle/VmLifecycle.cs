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

    public void AttachLogTarget(VmLifecyclePhase lifecyclePhase, ILogTarget logTarget)
    {
        switch (lifecyclePhase)
        {
            case VmLifecyclePhase.NotBooted:
                throw new NotAccessibleDueToLifecycleException("The preparing phase is not logged");
            case VmLifecyclePhase.Booting:
                BootLogTarget = logTarget;
                break;
            case VmLifecyclePhase.Active:
                ActiveLogTarget = logTarget;
                break;
            case VmLifecyclePhase.ShuttingDown:
                ShutdownLogTarget = logTarget;
                break;
            case VmLifecyclePhase.PoweredOff:
                throw new NotAccessibleDueToLifecycleException("The powered-off phase is not logged");
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
            case VmLifecyclePhase.NotBooted:
                throw new NotAccessibleDueToLifecycleException("The preparing phase is not logged");
            case VmLifecyclePhase.Booting:
                BootLogTarget = ILogTarget.Null;
                break;
            case VmLifecyclePhase.Active:
                ActiveLogTarget = ILogTarget.Null;
                break;
            case VmLifecyclePhase.ShuttingDown:
                ShutdownLogTarget = ILogTarget.Null;
                break;
            case VmLifecyclePhase.PoweredOff:
                throw new NotAccessibleDueToLifecycleException("The powered-off phase is not logged");
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
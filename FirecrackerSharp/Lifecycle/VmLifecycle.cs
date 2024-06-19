namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// An interface to accessing the microVM's lifecycle, registering listeners to lifecycle events, and setting up
/// lifecycle logging to various <see cref="ILogTarget"/>s.
/// </summary>
public sealed class VmLifecycle
{
    private VmLifecyclePhase _currentPhase = VmLifecyclePhase.NotBooted;
    
    internal ILogTarget BootLogTarget = ILogTarget.Null;
    internal ILogTarget ActiveLogTarget = ILogTarget.Null;
    internal ILogTarget ShutdownLogTarget = ILogTarget.Null;
    
    /// <summary>
    /// The current <see cref="VmLifecyclePhase"/> of this microVM. Refer to <see cref="VmLifecyclePhase"/> documentation
    /// on the differences between them.
    /// </summary>
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

    /// <summary>
    /// An event that is triggered by <see cref="VmLifecycle"/> when the <see cref="VmLifecyclePhase"/> has been changed.
    /// This event will receive the <see cref="VmLifecyclePhase"/> that has now started as its argument.
    /// </summary>
    public event EventHandler<VmLifecyclePhase>? PhaseStarted;
    /// <summary>
    /// An event that is triggered by <see cref="VmLifecycle"/> when the <see cref="VmLifecyclePhase"/> has been changed.
    /// This event will receive the former <see cref="VmLifecyclePhase"/> as its argument.
    /// </summary>
    public event EventHandler<VmLifecyclePhase>? PhaseFinished;

    internal bool IsNotActive => CurrentPhase != VmLifecyclePhase.Active;

    internal VmLifecycle()
    {
        CurrentPhase = VmLifecyclePhase.NotBooted;
    }

    /// <summary>
    /// Attach a <see cref="ILogTarget"/> to a <see cref="LoggedVmLifecyclePhase"/> of this microVM.
    /// </summary>
    /// <param name="lifecyclePhase">The <see cref="LoggedVmLifecyclePhase"/> during which this <see cref="ILogTarget"/>
    /// should receive log messages</param>
    /// <param name="logTarget">The <see cref="ILogTarget"/> to receive the log</param>
    /// <exception cref="ArgumentOutOfRangeException">If the <see cref="ILogTarget"/> was out of range of possible
    /// values</exception>
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

    /// <summary>
    /// Attach three given <see cref="ILogTarget"/> to all <see cref="LoggedVmLifecyclePhase"/>s of this microVM in
    /// the fixed order of: boot, active, shutdown.
    /// </summary>
    /// <param name="bootLogTarget">The <see cref="ILogTarget"/> to receive boot logs</param>
    /// <param name="activeLogTarget">The <see cref="ILogTarget"/> to receive active logs</param>
    /// <param name="shutdownLogTarget">The <see cref="ILogTarget"/> to receive shutdown logs</param>
    public void AttachAllLogTargets(ILogTarget bootLogTarget, ILogTarget activeLogTarget, ILogTarget shutdownLogTarget)
    {
        BootLogTarget = bootLogTarget;
        ActiveLogTarget = activeLogTarget;
        ShutdownLogTarget = shutdownLogTarget;
    }

    /// <summary>
    /// Attach the given <see cref="ILogTarget"/> to all <see cref="LoggedVmLifecyclePhase"/>s, meaning it will receive
    /// the sequential combination of all the separate logs.
    /// </summary>
    /// <param name="logTarget">The <see cref="ILogTarget"/> to receive all logs</param>
    public void AttachAllLogTargetsToSingle(ILogTarget logTarget)
    {
        BootLogTarget = logTarget;
        ActiveLogTarget = logTarget;
        ShutdownLogTarget = logTarget;
    }

    /// <summary>
    /// Determines whether any <see cref="ILogTarget"/> is attached to the given <see cref="LoggedVmLifecyclePhase"/>
    /// currently.
    /// </summary>
    /// <param name="lifecyclePhase">The <see cref="LoggedVmLifecyclePhase"/> to be checked against</param>
    /// <returns>Whether the attachment exists</returns>
    /// <exception cref="ArgumentOutOfRangeException">If the given <see cref="LoggedVmLifecyclePhase"/> is out of the
    /// range of all possible values</exception>
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

    /// <summary>
    /// Detach the <see cref="ILogTarget"/> that is currently attached to the given <see cref="LoggedVmLifecyclePhase"/>,
    /// or does nothing if none has been attached.
    /// </summary>
    /// <param name="lifecyclePhase">The <see cref="LoggedVmLifecyclePhase"/> to detach from</param>
    /// <exception cref="ArgumentOutOfRangeException">If the given <see cref="LoggedVmLifecyclePhase"/> is out of the
    /// range of possible values</exception>
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

    /// <summary>
    /// Detaches all currently attached <see cref="ILogTarget"/>s from all <see cref="LoggedVmLifecyclePhase"/>s.
    /// </summary>
    public void DetachAllLogTargets()
    {
        BootLogTarget = ILogTarget.Null;
        ActiveLogTarget = ILogTarget.Null;
        ShutdownLogTarget = ILogTarget.Null;
    }
}
namespace FirecrackerSharp.Lifecycle;

public class VmLifecycle
{
    private VmLifecyclePhase _currentPhase = VmLifecyclePhase.PreBoot;
    
    public ILogTarget BootLogTarget { internal get; set; } = ILogTarget.Null;
    public ILogTarget ActiveLogTarget { internal get; set; } = ILogTarget.Null;
    public ILogTarget ShutdownLogTarget { internal get; set; } = ILogTarget.Null;

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

    internal void FinishLastPhase()
    {
        PhaseFinished?.Invoke(sender: this, _currentPhase);
    }
}
namespace FirecrackerSharp.Shells;

public class VmShell
{
    public Guid Id { get; }
    
    private readonly VmShellManager _shellManager;
    
    internal VmShell(VmShellManager shellManager)
    {
        _shellManager = shellManager;
        Id = Guid.NewGuid();
    }
    
    
}
namespace FirecrackerSharp.Tty.Multiplexing;

public sealed class VmTtyMultiplexer
{
    private readonly VmTtyClient _ttyClient;

    internal VmTtyMultiplexer(VmTtyClient ttyClient)
    {
        _ttyClient = ttyClient;
    }
}
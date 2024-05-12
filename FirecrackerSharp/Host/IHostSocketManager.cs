namespace FirecrackerSharp.Host;

public interface IHostSocketManager
{
    public static IHostSocketManager Current { get; set; } = null!;

    public IHostSocket Connect(string socketAddress, string baseAddress);
}
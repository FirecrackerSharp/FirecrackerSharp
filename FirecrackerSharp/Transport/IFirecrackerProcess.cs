namespace FirecrackerSharp.Transport;

public interface IFirecrackerProcess
{
    public StreamReader StandardOutput { get; set; }
    public StreamReader StandardError { get; set; }
    public StreamWriter StandardInput { get; set; }

    public void Kill();
}
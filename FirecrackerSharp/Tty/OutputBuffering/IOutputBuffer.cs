namespace FirecrackerSharp.Tty.OutputBuffering;

public interface IOutputBuffer
{
    void Open();
    
    void Receive(string line);

    void Commit();
}
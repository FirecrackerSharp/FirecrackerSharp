namespace FirecrackerSharp.Tty;

public interface IOutputBuffer
{
    void Open();
    
    void Receive(string line);

    void Commit();
}
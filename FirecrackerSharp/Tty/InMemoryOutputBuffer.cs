using System.Text;

namespace FirecrackerSharp.Tty;

public class InMemoryOutputBuffer : IOutputBuffer
{
    private readonly StringBuilder _stringBuilder = new();
    
    public string Content => _stringBuilder.ToString();

    public event EventHandler<(string, string)>? DataReceived;
    public event EventHandler<string>? Filled;

    public void Open() { }

    public void Receive(string line)
    {
        _stringBuilder.Append(line);
        DataReceived?.Invoke(sender: this, (line, Content));
    }

    public void Commit()
    {
        var data = _stringBuilder.ToString();
        Filled?.Invoke(sender: this, data);
    }
}
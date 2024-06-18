using System.Text;

namespace FirecrackerSharp.Tty;

public class MemoryOutputBuffer : IOutputBuffer
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly List<string> _commits = [];
    
    public string PartialState => _stringBuilder.ToString();
    public IReadOnlyList<string> Commits => _commits;
    public string? LastCommit => _commits.LastOrDefault();

    public event EventHandler<(string, string)>? CommitUpdated;
    public event EventHandler<string>? CommitFinished;

    public void Open() { }

    public void Receive(string line)
    {
        _stringBuilder.Append(line);
        CommitUpdated?.Invoke(sender: this, (line, PartialState));
    }

    public void Commit()
    {
        var commit = _stringBuilder.ToString();
        _commits.Add(commit);
        _stringBuilder.Clear();
        CommitFinished?.Invoke(sender: this, commit);
    }
}
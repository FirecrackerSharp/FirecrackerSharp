using System.Text;

namespace FirecrackerSharp.Tty.OutputBuffering;

public sealed class MemoryOutputBuffer : IOutputBuffer
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly List<string> _commits = [];
    
    public string FutureCommitState => _stringBuilder.ToString();
    public IReadOnlyList<string> Commits => _commits;
    public string? LastCommit => _commits.LastOrDefault();

    public event EventHandler<(string, string)>? CommitUpdated;
    public event EventHandler<string>? CommitFinished;

    public void Open() { }

    public void Receive(string line)
    {
        _stringBuilder.Append(line);
        CommitUpdated?.Invoke(sender: this, (line, FutureCommitState));
    }

    public void Commit()
    {
        var commit = _stringBuilder.ToString();
        _commits.Add(commit);
        _stringBuilder.Clear();
        CommitFinished?.Invoke(sender: this, commit);
    }

    public void FlushCommits()
    {
        _commits.Clear();
    }
}
using System.Text;

namespace FirecrackerSharp.Tty.OutputBuffering;

/// <summary>
/// An <see cref="IOutputBuffer"/> that stores its data in-memory. This may be a sensible default for scenarios with
/// negligible amounts of data. The history of commits is also stored in-memory, but can be cleared/flushed via
/// <see cref="FlushCommits"/>.
/// </summary>
public sealed class MemoryOutputBuffer : IOutputBuffer
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly List<string> _commits = [];
    
    /// <summary>
    /// The currently bubbled-up receives that will become the next commit.
    /// </summary>
    public string FutureCommitState => _stringBuilder.ToString();
    /// <summary>
    /// The <see cref="IReadOnlyList{T}"/> representing the current sequential history of commits.
    /// </summary>
    public IReadOnlyList<string> Commits => _commits;
    /// <summary>
    /// The data stored in the last commit or null if no commits have been made.
    /// </summary>
    public string? LastCommit => _commits.LastOrDefault();

    /// <summary>
    /// An event that is triggered every time the future commit is updated, receiving the newly added line as its
    /// argument.
    /// </summary>
    public event EventHandler<string>? FutureCommitUpdated;
    /// <summary>
    /// An event that is triggered every time a commit is made, receiving the full content of the newly made commit
    /// as its argument.
    /// </summary>
    public event EventHandler<string>? CommitFinished;

    public void Open() { }

    public void Receive(string line)
    {
        _stringBuilder.Append(line);
        FutureCommitUpdated?.Invoke(sender: this, line);
    }

    public void Commit()
    {
        var commit = _stringBuilder.ToString();
        _commits.Add(commit);
        _stringBuilder.Clear();
        CommitFinished?.Invoke(sender: this, commit);
    }

    /// <summary>
    /// Clear the commit history, which is mainly useful in scenarios where RAM becomes a bottleneck but using another
    /// output buffer is infeasible.
    /// </summary>
    public void FlushCommits()
    {
        _commits.Clear();
    }
}
using FirecrackerSharp.Tty.OutputBuffering;
using FluentAssertions;

namespace FirecrackerSharp.Tests.OutputBuffering;

public class MemoryOutputBufferTests
{
    private readonly MemoryOutputBuffer _buffer = new();

    [Fact]
    public void Receive_ShouldTriggerEvent()
    {
        var eventTriggered = false;
        _buffer.CommitUpdated += (_, line) =>
        {
            line.Should().Be("test");
            _buffer.FutureCommitState.Should().Be("test");
            eventTriggered = true;
        };
        
        _buffer.Receive("test");
        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public void Commit_ShouldTriggerEvent()
    {
        var eventTriggered = false;
        _buffer.CommitFinished += (_, commit) =>
        {
            eventTriggered = true;
            commit.Should().Be("ab");
            _buffer.Commits.Last().Should().Be("ab");
            _buffer.FutureCommitState.Should().BeEmpty();
            _buffer.LastCommit.Should().Be("ab");
        };
        
        _buffer.Receive("a");
        _buffer.Receive("b");
        _buffer.Commit();

        eventTriggered.Should().BeTrue();
    }

    [Fact]
    public void FlushCommits_ShouldClearCommitList()
    {
        _buffer.Receive("a");
        _buffer.Commit();
        _buffer.Receive("b");
        _buffer.Commit();

        _buffer.Commits.Count.Should().Be(2);
        _buffer.FlushCommits();
        _buffer.LastCommit.Should().BeNull();
        _buffer.Commits.Should().BeEmpty();
    }
}
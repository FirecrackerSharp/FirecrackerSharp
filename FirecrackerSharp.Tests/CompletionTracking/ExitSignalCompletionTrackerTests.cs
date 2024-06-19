using FirecrackerSharp.Tty.CompletionTracking;
using FluentAssertions;

namespace FirecrackerSharp.Tests.CompletionTracking;

public class ExitSignalCompletionTrackerTests
{
    private readonly ExitSignalCompletionTracker _completionTracker = new(() => "exit")
    {
        Context = new CompletionTrackerContext(null!, DateTimeOffset.UtcNow, "cat --help")
    };

    [Fact]
    public void TransformInput_ShouldAppendEcho()
    {
        var result = _completionTracker.TransformInput("cat --help");
        result.Should().Be("cat --help;echo exit");
    }

    [Fact]
    public void ShouldCapture_ShouldRejectInputText()
    {
        _completionTracker.ShouldCapture("cat --help").Should().BeFalse();
    }

    [Fact]
    public void ShouldCapture_ShouldRejectExitSignal()
    {
        _completionTracker.TransformInput("");
        _completionTracker.ShouldCapture("exit").Should().BeFalse();
    }

    [Fact]
    public void ReactiveCheck_ShouldTriggerOnExitSignal()
    {
        _completionTracker.TransformInput("");
        _completionTracker.Check("exit").Should().BeTrue();
        _completionTracker.Check("not_exit").Should().BeFalse();
    }
}
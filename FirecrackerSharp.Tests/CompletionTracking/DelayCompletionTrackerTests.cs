using FirecrackerSharp.Tty.CompletionTracking;
using FluentAssertions;

namespace FirecrackerSharp.Tests.CompletionTracking;

public class DelayCompletionTrackerTests
{
    private readonly CompletionTrackerContext _trackerContext = new(null!, DateTimeOffset.UtcNow, "sample");
    
    [Fact]
    public void ShouldNoOp_OnUnusedMethods()
    {
        var tracker = new DelayCompletionTracker(TimeSpan.Zero);
        tracker.CheckReactively("any").Should().BeFalse();
        tracker.TransformInput("any").Should().Be("any");
    }
    
    [Fact]
    public void ShouldCapture_ShouldRejectContainingCommand()
    {
        var rejectTracker = new DelayCompletionTracker(TimeSpan.Zero) { Context = _trackerContext };
        var acceptTracker = new DelayCompletionTracker(TimeSpan.Zero, excludeContainingCommand: false) { Context = _trackerContext };
        rejectTracker.ShouldCapture("q_sample_q").Should().BeFalse();
        acceptTracker.ShouldCapture("q_sample_q").Should().BeTrue();
    }

    [Fact]
    public async Task CheckPassively_ShouldAwaitTimeSpan()
    {
        var tracker = new DelayCompletionTracker(TimeSpan.FromSeconds(1));
        var initialTime = DateTimeOffset.UtcNow;
        var success = await tracker.CheckPassively();
        success.Should().BeTrue();
        var elapsedTime = DateTimeOffset.UtcNow - initialTime;
        elapsedTime.Should().BeCloseTo(TimeSpan.FromSeconds(1), precision: TimeSpan.FromMilliseconds(20));
    }
}
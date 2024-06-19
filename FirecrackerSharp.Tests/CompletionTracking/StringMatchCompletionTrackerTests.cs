using FirecrackerSharp.Tty.CompletionTracking;
using FluentAssertions;

namespace FirecrackerSharp.Tests.CompletionTracking;

public class StringMatchCompletionTrackerTests
{
    private readonly StringMatchCompletionTracker _defaultTracker = new(StringMatchMode.Contains, "test");
    private readonly CompletionTrackerContext _trackerContext = new(null!, DateTimeOffset.UtcNow, "sample");

    [Fact]
    public void ShouldPerformContains()
    {
        var caseSensitiveTracker = new StringMatchCompletionTracker(StringMatchMode.Contains, "test")
            { Context = _trackerContext };
        var caseInsensitiveTracker = new StringMatchCompletionTracker(StringMatchMode.Contains, "test", StringComparison.OrdinalIgnoreCase)
            { Context = _trackerContext };
        caseSensitiveTracker.CheckReactively("q_test_q").Should().BeTrue();
        caseSensitiveTracker.CheckReactively("q_Test_q").Should().BeFalse();
        caseInsensitiveTracker.CheckReactively("q_test_q").Should().BeTrue();
        caseInsensitiveTracker.CheckReactively("q_Test_q").Should().BeTrue();
    }

    [Fact]
    public void ShouldPerformStartsWith()
    {
        var caseSensitiveTracker = new StringMatchCompletionTracker(StringMatchMode.StartsWith, "test")
            { Context = _trackerContext };
        var caseInsensitiveTracker = new StringMatchCompletionTracker(StringMatchMode.StartsWith, "test", StringComparison.OrdinalIgnoreCase)
            { Context = _trackerContext };
        caseSensitiveTracker.CheckReactively("test_q").Should().BeTrue();
        caseSensitiveTracker.CheckReactively("Test_q").Should().BeFalse();
        caseSensitiveTracker.CheckReactively("q_test_q").Should().BeFalse();
        caseInsensitiveTracker.CheckReactively("test_q").Should().BeTrue();
        caseInsensitiveTracker.CheckReactively("Test_q").Should().BeTrue();
        caseInsensitiveTracker.CheckReactively("q_test_q").Should().BeFalse();
    }

    [Fact]
    public void ShouldPerformEndsWith()
    {
        var caseSensitiveTracker = new StringMatchCompletionTracker(StringMatchMode.EndsWith, "test")
            { Context = _trackerContext };
        var caseInsensitiveTracker = new StringMatchCompletionTracker(StringMatchMode.EndsWith, "test", StringComparison.OrdinalIgnoreCase)
            { Context = _trackerContext };
        caseSensitiveTracker.CheckReactively("q_test").Should().BeTrue();
        caseSensitiveTracker.CheckReactively("q_Test").Should().BeFalse();
        caseSensitiveTracker.CheckReactively("q_test_q").Should().BeFalse();
        caseInsensitiveTracker.CheckReactively("q_test").Should().BeTrue();
        caseInsensitiveTracker.CheckReactively("q_Test").Should().BeTrue();
        caseInsensitiveTracker.CheckReactively("q_test_q").Should().BeFalse();
    }
    
    [Fact]
    public void PassiveCheck_ShouldNotBeSupported()
    {
        _defaultTracker.CheckPassively().Should().BeNull();
    }

    [Fact]
    public void ShouldNotTransformInput()
    {
        _defaultTracker.TransformInput("q").Should().Be("q");
    }

    [Fact]
    public void ShouldCapture_ShouldRejectContainingCommand()
    {
        var rejectTracker = new StringMatchCompletionTracker(StringMatchMode.Contains, "", excludeContainingCommand: true)
            { Context = _trackerContext };
        var acceptTracker = new StringMatchCompletionTracker(StringMatchMode.Contains, "", excludeContainingCommand: false)
            { Context = _trackerContext };
        rejectTracker.ShouldCapture("q_sample_q").Should().BeFalse();
        acceptTracker.ShouldCapture("q_sample_q").Should().BeTrue();
    }
}
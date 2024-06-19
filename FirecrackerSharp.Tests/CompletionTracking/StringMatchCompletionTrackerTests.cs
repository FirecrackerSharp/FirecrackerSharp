using FirecrackerSharp.Tty.CompletionTracking;
using FluentAssertions;

namespace FirecrackerSharp.Tests.CompletionTracking;

public class StringMatchCompletionTrackerTests
{
    private readonly StringMatchCompletionTracker _defaultTracker = new(StringMatchOperation.Contains, "test");
    private readonly CompletionTrackerContext _trackerContext = new(null!, DateTimeOffset.UtcNow, "sample");

    [Fact]
    public void ShouldPerformContains()
    {
        var caseSensitiveTracker = new StringMatchCompletionTracker(StringMatchOperation.Contains, "test")
            { Context = _trackerContext };
        var caseInsensitiveTracker = new StringMatchCompletionTracker(StringMatchOperation.Contains, "test", StringComparison.OrdinalIgnoreCase)
            { Context = _trackerContext };
        caseSensitiveTracker.Check("q_test_q").Should().BeTrue();
        caseSensitiveTracker.Check("q_Test_q").Should().BeFalse();
        caseInsensitiveTracker.Check("q_test_q").Should().BeTrue();
        caseInsensitiveTracker.Check("q_Test_q").Should().BeTrue();
    }

    [Fact]
    public void ShouldPerformStartsWith()
    {
        var caseSensitiveTracker = new StringMatchCompletionTracker(StringMatchOperation.StartsWith, "test")
            { Context = _trackerContext };
        var caseInsensitiveTracker = new StringMatchCompletionTracker(StringMatchOperation.StartsWith, "test", StringComparison.OrdinalIgnoreCase)
            { Context = _trackerContext };
        caseSensitiveTracker.Check("test_q").Should().BeTrue();
        caseSensitiveTracker.Check("Test_q").Should().BeFalse();
        caseSensitiveTracker.Check("q_test_q").Should().BeFalse();
        caseInsensitiveTracker.Check("test_q").Should().BeTrue();
        caseInsensitiveTracker.Check("Test_q").Should().BeTrue();
        caseInsensitiveTracker.Check("q_test_q").Should().BeFalse();
    }

    [Fact]
    public void ShouldPerformEndsWith()
    {
        var caseSensitiveTracker = new StringMatchCompletionTracker(StringMatchOperation.EndsWith, "test")
            { Context = _trackerContext };
        var caseInsensitiveTracker = new StringMatchCompletionTracker(StringMatchOperation.EndsWith, "test", StringComparison.OrdinalIgnoreCase)
            { Context = _trackerContext };
        caseSensitiveTracker.Check("q_test").Should().BeTrue();
        caseSensitiveTracker.Check("q_Test").Should().BeFalse();
        caseSensitiveTracker.Check("q_test_q").Should().BeFalse();
        caseInsensitiveTracker.Check("q_test").Should().BeTrue();
        caseInsensitiveTracker.Check("q_Test").Should().BeTrue();
        caseInsensitiveTracker.Check("q_test_q").Should().BeFalse();
    }

    [Fact]
    public void ShouldPerformEquals()
    {
        var caseSensitiveTracker = new StringMatchCompletionTracker(StringMatchOperation.Equals, "test")
            { Context = _trackerContext };
        var caseInsensitiveTracker = new StringMatchCompletionTracker(StringMatchOperation.Equals, "test", StringComparison.OrdinalIgnoreCase)
            { Context = _trackerContext };
        caseSensitiveTracker.Check("test").Should().BeTrue();
        caseSensitiveTracker.Check("Test").Should().BeFalse();
        caseSensitiveTracker.Check("testq").Should().BeFalse();
        caseInsensitiveTracker.Check("test").Should().BeTrue();
        caseInsensitiveTracker.Check("Test").Should().BeTrue();
        caseInsensitiveTracker.Check("testq").Should().BeFalse();
    }

    [Fact]
    public void ShouldNotTransformInput()
    {
        _defaultTracker.TransformInput("q").Should().Be("q");
    }

    [Fact]
    public void ShouldCapture_ShouldRejectContainingCommand()
    {
        var rejectTracker = new StringMatchCompletionTracker(StringMatchOperation.Contains, "", excludeContainingCommand: true)
            { Context = _trackerContext };
        var acceptTracker = new StringMatchCompletionTracker(StringMatchOperation.Contains, "", excludeContainingCommand: false)
            { Context = _trackerContext };
        rejectTracker.ShouldCapture("q_sample_q").Should().BeFalse();
        acceptTracker.ShouldCapture("q_sample_q").Should().BeTrue();
    }
}
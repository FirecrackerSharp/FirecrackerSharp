using FirecrackerSharp.Tty.CompletionTracking;
using FluentAssertions;

namespace FirecrackerSharp.Tests.CompletionTracking;

public class AggregateCompletionTrackerTests
{
    [Fact]
    public void TransformInput_ShouldSequentiallyAggregate()
    {
        var ct1 = new MockCompletionTracker("b");
        var ct2 = new MockCompletionTracker("c");
        var ct = new AggregateCompletionTracker([ct1, ct2]);
        ct.TransformInput("a").Should().Be("abc");
    }

    [Fact]
    public void ShouldCapture_ShouldUseNegotiationPolicy()
    {
        var ct1 = new MockCompletionTracker();
        var ct2 = new MockCompletionTracker();
        var ct3 = new MockCompletionTracker();
        
        ct1.CaptureVote = true;
        new AggregateCompletionTracker([ct1, ct2, ct3], captureNegotiationPolicy: NegotiationPolicy.Any)
            .ShouldCapture("").Should().BeTrue();
        ct1.CaptureVote = false;
        new AggregateCompletionTracker([ct1, ct2, ct3], captureNegotiationPolicy: NegotiationPolicy.Any)
            .ShouldCapture("").Should().BeFalse();

        ct1.CaptureVote = true;
        ct2.CaptureVote = true;
        new AggregateCompletionTracker([ct1, ct2, ct3], captureNegotiationPolicy: NegotiationPolicy.Majority)
            .ShouldCapture("").Should().BeTrue();
        ct2.CaptureVote = false;
        new AggregateCompletionTracker([ct1, ct2, ct3], captureNegotiationPolicy: NegotiationPolicy.Majority)
            .ShouldCapture("").Should().BeFalse();

        ct1.CaptureVote = true;
        ct2.CaptureVote = true;
        ct3.CaptureVote = true;
        new AggregateCompletionTracker([ct1, ct2, ct3], captureNegotiationPolicy: NegotiationPolicy.All)
            .ShouldCapture("").Should().BeTrue();
        ct3.CaptureVote = false;
        new AggregateCompletionTracker([ct1, ct2, ct3], captureNegotiationPolicy: NegotiationPolicy.All)
            .ShouldCapture("").Should().BeFalse();
    }
    
    [Fact]
    public void Check_ShouldUseNegotiationPolicy()
    {
        var ct1 = new MockCompletionTracker();
        var ct2 = new MockCompletionTracker();
        var ct3 = new MockCompletionTracker();
        
        ct1.CheckVote = true;
        new AggregateCompletionTracker([ct1, ct2, ct3], checkNegotiationPolicy: NegotiationPolicy.Any)
            .Check("").Should().BeTrue();
        ct1.CheckVote = false;
        new AggregateCompletionTracker([ct1, ct2, ct3], checkNegotiationPolicy: NegotiationPolicy.Any)
            .Check("").Should().BeFalse();

        ct1.CheckVote = true;
        ct2.CheckVote = true;
        new AggregateCompletionTracker([ct1, ct2, ct3], checkNegotiationPolicy: NegotiationPolicy.Majority)
            .Check("").Should().BeTrue();
        ct2.CheckVote = false;
        new AggregateCompletionTracker([ct1, ct2, ct3], checkNegotiationPolicy: NegotiationPolicy.Majority)
            .Check("").Should().BeFalse();

        ct1.CheckVote = true;
        ct2.CheckVote = true;
        ct3.CheckVote = true;
        new AggregateCompletionTracker([ct1, ct2, ct3], checkNegotiationPolicy: NegotiationPolicy.All)
            .Check("").Should().BeTrue();
        ct3.CheckVote = false;
        new AggregateCompletionTracker([ct1, ct2, ct3], checkNegotiationPolicy: NegotiationPolicy.All)
            .Check("").Should().BeFalse();
    }
    
    private class MockCompletionTracker(string suffix = "a") : ICompletionTracker
    {
        public bool CaptureVote;
        public bool CheckVote;
        
        public CompletionTrackerContext? Context { get; set; }
        public string TransformInput(string inputText) => inputText + suffix;
        public bool ShouldCapture(string line) => CaptureVote;
        public bool Check(string line) => CheckVote;
    }
}
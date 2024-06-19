using FirecrackerSharp.Tty.OutputBuffering;
using FluentAssertions;

namespace FirecrackerSharp.Tests.OutputBuffering;

public class AggregateOutputBufferTests
{
    private static event EventHandler<int>? Trigger;
    private readonly AggregateOutputBuffer _buffer = new([new OutputBuffer1(), new OutputBuffer2()]);

    [Fact]
    public void Open_ShouldForward()
    {
        var signal1 = false;
        var signal2 = false;
        
        Trigger += (_, signal) =>
        {
            if (signal == 1) signal1 = true;
            if (signal == 4) signal2 = true;
        };

        _buffer.Open();
        signal1.Should().Be(signal2).And.BeTrue();
    }

    [Fact]
    public void Receive_ShouldForward()
    {
        var signal1 = false;
        var signal2 = false;

        Trigger += (_, signal) =>
        {
            if (signal == 2) signal1 = true;
            if (signal == 5) signal2 = true;
        };
        
        _buffer.Receive("test");
        signal1.Should().Be(signal2).And.BeTrue();
    }

    [Fact]
    public void Commit_ShouldForward()
    {
        var signal1 = false;
        var signal2 = false;

        Trigger += (_, signal) =>
        {
            if (signal == 3) signal1 = true;
            if (signal == 6) signal2 = true;
        };
        
        _buffer.Commit();
        signal1.Should().Be(signal2).And.BeTrue();
    }
    
    private class OutputBuffer1 : IOutputBuffer
    {
        public void Open() => Trigger?.Invoke(this, 1);
        public void Receive(string line) => Trigger?.Invoke(this, 2);
        public void Commit() => Trigger?.Invoke(this, 3);
    }

    private class OutputBuffer2 : IOutputBuffer
    {
        public void Open() => Trigger?.Invoke(this, 4);
        public void Receive(string line) => Trigger?.Invoke(this, 5);
        public void Commit() => Trigger?.Invoke(this, 6);
    }
}
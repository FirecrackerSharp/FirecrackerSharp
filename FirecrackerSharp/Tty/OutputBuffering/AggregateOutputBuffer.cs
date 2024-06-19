namespace FirecrackerSharp.Tty.OutputBuffering;

public sealed class AggregateOutputBuffer(IEnumerable<IOutputBuffer> aggregatedOutputBuffers) : IOutputBuffer
{
    public void Open()
    {
        foreach (var outputBuffer in aggregatedOutputBuffers)
        {
            outputBuffer.Open();
        }
    }

    public void Receive(string line)
    {
        foreach (var outputBuffer in aggregatedOutputBuffers)
        {
            outputBuffer.Receive(line);
        }
    }

    public void Commit()
    {
        foreach (var outputBuffer in aggregatedOutputBuffers)
        {
            outputBuffer.Commit();
        }
    }
}
namespace FirecrackerSharp.Tty.OutputBuffering;

/// <summary>
/// An <see cref="IOutputBuffer"/> that represents a combination of multiple <see cref="IOutputBuffer"/>s. It simply
/// sequentially forwards the received open, receive and commit calls to each encapsulated <see cref="IOutputBuffer"/>.
/// </summary>
/// <param name="aggregatedOutputBuffers">The <see cref="IEnumerable{IOutputBuffer}"/> to be aggregated</param>
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
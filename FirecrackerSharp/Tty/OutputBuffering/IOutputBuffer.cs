namespace FirecrackerSharp.Tty.OutputBuffering;

/// <summary>
/// An output buffer is a mechanism of the TTY client used for persisting the output of primary write operations
/// (not intermittent, since those, by definition, have an output which is a subset of primary output).
///
/// Always persisting the output in-memory can be a pitfall since, for more sophisticated scenarios, it would exhaust
/// the RAM of the process. However, this is implemented in the <see cref="MemoryOutputBuffer"/> which also supports
/// flushing its commits, so consider that to be a sane default implementation of an output buffer.
/// </summary>
public interface IOutputBuffer
{
    /// <summary>
    /// Open this output buffer's resources. This is called when the output buffer is loaded into the TTY client.
    ///
    /// For example, the <see cref="FileOutputBuffer"/> uses the open in order to ensure that the file exists.
    /// </summary>
    void Open();
    
    /// <summary>
    /// Persist the streamed-in line into this output buffer. This is a "partial" persist, meaning that not the entire
    /// result of the write operation, but rather a single small chunk (line) is being written.
    /// </summary>
    /// <param name="line">The chunk/line of data to be persisted into the buffer</param>
    void Receive(string line);

    /// <summary>
    /// Commit this write operation into the buffer. This is a "full" persist, meaning that it is called after the
    /// write operation is entirely completed. A buffer can choose to, for example, persist to its target resource
    /// on every <see cref="Receive"/> call, or defer the persistence operation by bubbling up the commit (=operation)'s
    /// data in-memory and then persist in on the <see cref="Commit"/> call.
    /// </summary>
    void Commit();
}
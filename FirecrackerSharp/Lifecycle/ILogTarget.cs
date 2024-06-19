using System.Text;
using FirecrackerSharp.Host;

namespace FirecrackerSharp.Lifecycle;

/// <summary>
/// A log target is anything that can receive logs emitted by the firecracker/jailer process, which are sent line-by-line
/// in a reactive (observer) manner. An example would be an in-memory <see cref="StringBuilder"/>, a file and so forth.
/// </summary>
public interface ILogTarget
{
    /// <summary>
    /// Receive the streamed log line and persist it to this target. This operation is synchronous, meaning that only
    /// synchronous I/O operations can be used when needed (except for firing up an asynchronous task on a background
    /// thread).
    /// </summary>
    /// <param name="line">The streamed line</param>
    public void Receive(string line);

    internal static readonly ILogTarget Null = new NullLogTarget();
    
    /// <summary>
    /// Create a new log target that logs to the given in-memory <see cref="StringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/> that should receive the logs</param>
    /// <returns>The created log target</returns>
    public static ILogTarget ToStringBuilder(StringBuilder stringBuilder)
        => new StringBuilderLogTarget(stringBuilder);

    /// <summary>
    /// Create a new log target that logs to the given stream.
    /// </summary>
    /// <param name="stream">The <see cref="Stream"/> to be written to</param>
    /// <returns>The created log target</returns>
    public static ILogTarget ToStream(Stream stream)
        => new StreamLogTarget(stream);

    /// <summary>
    /// Create a new log target that logs to the given file, plus create that file on the app or VM host if it doesn't
    /// already exist.
    /// </summary>
    /// <param name="filePath">The absolute path to the file</param>
    /// <param name="onAppHost">Whether the file should be on the application host or the VM host. If these are the
    /// same machine, this doesn't have any influence</param>
    /// <returns>The created log target</returns>
    public static ILogTarget ToFile(string filePath, bool onAppHost = true)
    {
        switch (onAppHost)
        {
            case true when !File.Exists(filePath):
                File.CreateText(filePath).Close();
                break;
            case false when !IHostFilesystem.Current.FileOrDirectoryExists(filePath):
                IHostFilesystem.Current.CreateTextFile(filePath);
                break;
        }
        return new FileLogTarget(filePath, onAppHost);
    }

    /// <summary>
    /// Create a new log target that logs to a combination of given log targets. This is internally implemented by simply
    /// looping through the given targets and sequentially invoking their 
    /// </summary>
    /// <param name="aggregatedTargets">The <see cref="IEnumerable{T}"/> of aggregated log targets</param>
    /// <returns>The created log target</returns>
    public static ILogTarget ToAggregate(IEnumerable<ILogTarget> aggregatedTargets)
        => new AggregateLogTarget(aggregatedTargets);

    private class NullLogTarget : ILogTarget
    {
        public void Receive(string line)
        {
            // no-op
        }
    }

    private class StringBuilderLogTarget(StringBuilder stringBuilder) : ILogTarget
    {
        public void Receive(string line)
        {
            stringBuilder.Append(line);
        }
    }

    private class FileLogTarget(string filePath, bool onAppHost) : ILogTarget
    {
        public void Receive(string line)
        {
            if (onAppHost)
            {
                File.AppendAllText(filePath, line);
            }
            else
            {
                IHostFilesystem.Current.AppendTextFile(filePath, line);
            }
        }
    }

    private class StreamLogTarget(Stream stream) : ILogTarget
    {
        public void Receive(string line)
        {
            stream.Write(Encoding.Default.GetBytes(line));
        }
    }

    private class AggregateLogTarget(IEnumerable<ILogTarget> aggregatedTargets) : ILogTarget
    {
        public void Receive(string line)
        {
            foreach (var target in aggregatedTargets)
            {
                target.Receive(line);
            }
        }
    }
}
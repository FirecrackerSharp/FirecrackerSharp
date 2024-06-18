using System.Text;
using FirecrackerSharp.Host;

namespace FirecrackerSharp.Lifecycle;

public interface ILogTarget
{
    public void Receive(string line);

    public static readonly ILogTarget Null = new NullLogTarget();
    
    public static ILogTarget ToStringBuilder(StringBuilder stringBuilder)
        => new StringBuilderLogTarget(stringBuilder);

    public static ILogTarget ToStream(Stream stream)
        => new StreamLogTarget(stream);

    public static ILogTarget ToFile(string filePath, bool onAppHost = true)
        => new FileLogTarget(filePath, onAppHost);

    public static ILogTarget ToCombination(IEnumerable<ILogTarget> aggregatedTargets)
        => new CombinedLogTarget(aggregatedTargets);

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

    private class StreamLogTarget : ILogTarget
    {
        private readonly StreamWriter _streamWriter;

        internal StreamLogTarget(Stream stream)
        {
            _streamWriter = new StreamWriter(stream);
        }

        public void Receive(string line)
        {
            _streamWriter.Write(line);
        }
    }

    private class CombinedLogTarget(IEnumerable<ILogTarget> aggregatedTargets) : ILogTarget
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
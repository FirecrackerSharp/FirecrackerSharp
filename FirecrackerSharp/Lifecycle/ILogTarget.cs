using System.Text;
using FirecrackerSharp.Host;

namespace FirecrackerSharp.Lifecycle;

public interface ILogTarget
{
    public void Receive(string line);

    internal static readonly ILogTarget Null = new NullLogTarget();
    
    public static ILogTarget ToStringBuilder(StringBuilder stringBuilder)
        => new StringBuilderLogTarget(stringBuilder);

    public static ILogTarget ToStream(Stream stream)
        => new StreamLogTarget(stream);

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
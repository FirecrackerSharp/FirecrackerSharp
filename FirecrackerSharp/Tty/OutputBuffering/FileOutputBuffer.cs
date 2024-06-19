using System.Text;
using FirecrackerSharp.Host;

namespace FirecrackerSharp.Tty.OutputBuffering;

/// <summary>
/// An <see cref="IOutputBuffer"/> that writes to a single file by appending to it. The file will be created if it doesn't
/// already exist when the buffer is opened.
/// </summary>
/// <param name="filePath">The path to the single file</param>
/// <param name="onAppHost">Whether the file is located on the application host, or the VM host</param>
/// <param name="deferWriteUntilCommit">When true, the commit data will be bubbled up into a temporary buffer stored
/// in-memory and only persisted to the file upon commit. When false, each receive will persist to the file</param>
public sealed class FileOutputBuffer(
    string filePath,
    bool onAppHost = true,
    bool deferWriteUntilCommit = false) : IOutputBuffer
{
    private readonly StringBuilder _deferredBuffer = new();
    
    public void Open()
    {
        if (onAppHost)
        {
            if (!File.Exists(filePath))
            {
                File.CreateText(filePath).Close();
            }
        }
        else
        {
            if (!IHostFilesystem.Current.FileOrDirectoryExists(filePath))
            {
                IHostFilesystem.Current.CreateTextFile(filePath);
            }
        }
    }

    public void Receive(string line)
    {
        if (deferWriteUntilCommit)
        {
            _deferredBuffer.Append(line);
        }
        else
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

    public void Commit()
    {
        if (!deferWriteUntilCommit) return;
        
        if (onAppHost)
        {
            File.AppendAllText(filePath, _deferredBuffer.ToString());
        }
        else
        {
            IHostFilesystem.Current.AppendTextFile(filePath, _deferredBuffer.ToString());
        }

        _deferredBuffer.Clear();
    }
}
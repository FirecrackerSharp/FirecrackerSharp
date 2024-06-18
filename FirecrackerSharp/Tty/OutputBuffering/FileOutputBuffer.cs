using System.Text;
using FirecrackerSharp.Host;

namespace FirecrackerSharp.Tty.OutputBuffering;

public class FileOutputBuffer(
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
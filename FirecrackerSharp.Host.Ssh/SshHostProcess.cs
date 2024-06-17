using System.Text;
using Renci.SshNet;

namespace FirecrackerSharp.Host.Ssh;

internal class SshHostProcess : IHostProcess, IAsyncDisposable
{
    private StreamWriter? _stdinWriter;
    private readonly ShellStream _shellStream;
    private readonly IBaseClient _sshClient;
    private readonly ShellConfiguration _shellConfiguration;

    public StreamWriter StdinWriter
    {
        get
        {
            _stdinWriter ??= new StreamWriter(_shellStream);
            return _stdinWriter;
        }
    }
    
    public string CurrentOutput { get; private set; } = string.Empty;
    
    public event EventHandler<string>? OutputReceived;

    internal SshHostProcess(ShellStream shellStream, IBaseClient sshClient, ShellConfiguration shellConfiguration)
    {
        _shellStream = shellStream;
        _sshClient = sshClient;
        _shellConfiguration = shellConfiguration;

        _shellStream.DataReceived += (_, args) =>
        {
            var line = Encoding.UTF8.GetString(args.Data);
            if (!string.IsNullOrEmpty(line))
            {
                OutputReceived?.Invoke(sender: this, line);
            }
        };

        OutputReceived += (_, line) =>
        {
            CurrentOutput += line;
        };
    }
    
    public async Task KillAsync()
    {
        _shellStream.WriteLine("^C");
        await DisposeAsync();
    }

    public async Task<bool> WaitForGracefulExitAsync(TimeSpan timeout)
    {
        var result = _shellStream.Expect(_shellConfiguration.ExpectedShellEnding, timeout);
        await DisposeAsync();
        return result is not null;
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_stdinWriter != null) await _stdinWriter.DisposeAsync();
        await _shellStream.DisposeAsync();
        _sshClient.Disconnect();
        _sshClient.Dispose();
    }
}
using System.Text;
using Renci.SshNet;

namespace FirecrackerSharp.Host.Ssh;

internal sealed class SshHostProcess : IHostProcess, IAsyncDisposable
{
    private readonly ShellStream _shellStream;
    private readonly IBaseClient _sshClient;
    private readonly ShellConfiguration _shellConfiguration;
    private readonly StringBuilder _buffer = new();
    
    public string CurrentOutput { get; set; } = string.Empty;

    public bool SupportsExpect => true;
    public event EventHandler<string>? OutputReceived;

    internal SshHostProcess(ShellStream shellStream, IBaseClient sshClient, ShellConfiguration shellConfiguration)
    {
        _shellStream = shellStream;
        _sshClient = sshClient;
        _shellConfiguration = shellConfiguration;

        _shellStream.DataReceived += (_, args) =>
        {
            var line = Encoding.UTF8.GetString(args.Data);
            if (line != "")
            {
                _buffer.Append(line);
                var content = _buffer.ToString();
                if (content.EndsWith('\n'))
                {
                    OutputReceived?.Invoke(sender: this, content);
                    _buffer.Clear();
                }
            }
        };

        OutputReceived += (_, line) =>
        {
            CurrentOutput += line;
        };
    }

    public bool Expect(string text, TimeSpan timeout)
    {
        var result = _shellStream.Expect(text, timeout);
        return result is not null;
    }

    public Task WriteAsync(string text, CancellationToken cancellationToken)
    {
        _shellStream.Write(text);
        return Task.CompletedTask;
    }

    public Task WriteLineAsync(string text, CancellationToken cancellationToken)
    {
        _shellStream.WriteLine(text);
        _shellStream.Expect("exit_code");
        return Task.CompletedTask;
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
        await _shellStream.DisposeAsync();
        _sshClient.Disconnect();
        _sshClient.Dispose();
    }
}
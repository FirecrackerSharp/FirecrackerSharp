using System.Text;
using Renci.SshNet;

namespace FirecrackerSharp.Host.Ssh;

internal sealed class SshHostProcess : IHostProcess, IAsyncDisposable
{
    private readonly ShellStream _shellStream;
    private readonly IBaseClient _sshClient;
    private readonly StringBuilder _buffer = new();
    
    public string CurrentOutput { get; set; } = string.Empty;

    public event EventHandler<string>? OutputReceived;

    internal SshHostProcess(ShellStream shellStream, IBaseClient sshClient)
    {
        _shellStream = shellStream;
        _sshClient = sshClient;

        _shellStream.DataReceived += (_, args) =>
        {
            var line = Encoding.UTF8.GetString(args.Data);
            if (line == "") return;
            
            _buffer.Append(line);
            var content = _buffer.ToString();
            if (!content.EndsWith('\n')) return;
            
            OutputReceived?.Invoke(sender: this, content);
            _buffer.Clear();
        };

        OutputReceived += (_, line) =>
        {
            CurrentOutput += line;
        };
    }

    public Task WriteAsync(string text, CancellationToken cancellationToken)
    {
        _shellStream.Write(text);
        return Task.CompletedTask;
    }

    public Task WriteLineAsync(string text, CancellationToken cancellationToken)
    {
        _shellStream.WriteLine(text);
        return Task.CompletedTask;
    }

    public async Task KillAsync()
    {
        await DisposeAsync();
    }

    public async Task<bool> WaitForExitAsync(TimeSpan timeout, string? expectation)
    {
        if (expectation is null) return false;
        var result = _shellStream.Expect(expectation, timeout);
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
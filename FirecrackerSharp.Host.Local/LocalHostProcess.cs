using System.Diagnostics;
using System.Text;

namespace FirecrackerSharp.Host.Local;

internal sealed class LocalHostProcess : IHostProcess
{
    private readonly Process _osProcess;
    
    public string CurrentOutput { get; set; } = string.Empty;
    public event EventHandler<string>? OutputReceived;

    internal LocalHostProcess(Process osProcess)
    {
        _osProcess = osProcess;
        _osProcess.Start();
        _osProcess.BeginOutputReadLine();
        _osProcess.BeginErrorReadLine();

        _osProcess.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                OutputReceived?.Invoke(sender: this, args.Data + "\n");
            }
        };

        _osProcess.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
            {
                OutputReceived?.Invoke(sender: this, args.Data + "\n");
            }
        };

        OutputReceived += (_, line) =>
        {
            CurrentOutput += line;
        };
    }

    public Task WriteAsync(string text, CancellationToken cancellationToken)
    {
        return _osProcess.StandardInput.WriteAsync(new StringBuilder(text), cancellationToken);
    }

    public Task WriteLineAsync(string text, CancellationToken cancellationToken)
    {
        return _osProcess.StandardInput.WriteLineAsync(new StringBuilder(text), cancellationToken);
    }

    public Task KillAsync()
    {
        _osProcess.Kill();
        return Task.CompletedTask;
    }

    public async Task<bool> WaitForExitAsync(TimeSpan timeout, string? expectation)
    {
        var cancellationToken = new CancellationTokenSource(timeout).Token;
        try
        {
            await _osProcess.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        return true;
    }
}
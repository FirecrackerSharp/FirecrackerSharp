using FirecrackerSharp.Tests.Helpers;
using FirecrackerSharp.Tty.CompletionTracking;
using FirecrackerSharp.Tty.OutputBuffering;
using FluentAssertions;

namespace FirecrackerSharp.Tests.Vmm;

public class VmTtyClientTests : SingleVmFixture
{
    private const string HelpOutput =
    """
    Usage: cat [OPTION]... [FILE]...
    Concatenate FILE(s) to standard output.
    
    With no FILE, or when FILE is -, read standard input.
    
      -A, --show-all           equivalent to -vET
      -b, --number-nonblank    number nonempty output lines, overrides -n
      -e                       equivalent to -vE
      -E, --show-ends          display $ at end of each line
      -n, --number             number all output lines
      -s, --squeeze-blank      suppress repeated empty output lines
      -t                       equivalent to -vT
      -T, --show-tabs          display TAB characters as ^I
      -u                       (ignored)
      -v, --show-nonprinting   use ^ and M- notation, except for LFD and TAB
          --help     display this help and exit
          --version  output version information and exit
    
    Examples:
      cat f - g  Output f's contents, then standard input, then g's contents.
      cat        Copy standard input to standard output.
    
    GNU coreutils online help: <https://www.gnu.org/software/coreutils/>
    Report any translation bugs to <https://translationproject.org/team/>
    Full documentation <https://www.gnu.org/software/coreutils/cat>
    or available locally via: info '(coreutils) cat invocation'
    """;

    [Fact]
    public async Task WritePrimaryAsync_ShouldPerformNonTrackedWrite()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
                Vm.TtyClient.OutputBuffer = new MemoryOutputBuffer();
                await Vm.TtyClient.WritePrimaryAsync("cat --help", cancellationToken: token);
                await Task.Delay(TimeSpan.FromMilliseconds(300), token);
                AssertHelpOutput();
            })
            .Should().NotThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WritePrimaryAsync_ShouldPerformTrackedWrite()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
                Vm.TtyClient.OutputBuffer = new MemoryOutputBuffer();
                await Vm.TtyClient.WritePrimaryAsync(
                    "cat --help", completionTracker: new ExitSignalCompletionTracker(), cancellationToken: token);
                await Vm.TtyClient.WaitForPrimaryAvailabilityAsync(cancellationToken: token);
                AssertHelpOutput();
            })
            .Should().NotThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WriteIntermittentAsync_ShouldPerformNonTrackedWrite()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
                await Vm.TtyClient.StartBufferedCommandAsync("read n && echo $n", cancellationToken: token);
                await Vm.TtyClient.WriteIntermittentAsync("sample_input", cancellationToken: token);
                
                var output = await Vm.TtyClient.WaitForBufferedCommandAsync(cancellationToken: token);
                output.Should().NotBeNull();
                output!.Trim().Should().Be("sample_input\nsample_input");
            })
            .Should().NotThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WriteIntermittentAsync_ShouldPerformTrackedWrite()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                var token = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
                await Vm.TtyClient.StartBufferedCommandAsync("read n && echo q$n", cancellationToken: token);
                await Vm.TtyClient.WriteIntermittentAsync("test",
                    completionTracker: new StringMatchCompletionTracker(StringMatchMode.Contains, "qtest"),
                    cancellationToken: token);
                await Vm.TtyClient.WaitForIntermittentAvailabilityAsync(cancellationToken: token);
                await Vm.TtyClient.WaitForPrimaryAvailabilityAsync(cancellationToken: token);

                var output = await Vm.TtyClient.WaitForBufferedCommandAsync(cancellationToken: token);
                output.Should().NotBeNull();
                output.Should().Be("test\nqtest");
            })
            .Should().NotThrowAsync<OperationCanceledException>();
    }
    
    [Fact]
    public async Task RunBufferedCommandAsync_ShouldReturnCorrectOutput()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                var output = await Vm.TtyClient.RunBufferedCommandAsync("cat --help",
                    cancellationToken: new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token);
                output.Should().NotBeNull();
                output!.Trim().Should().Be(HelpOutput);
            })
            .Should().NotThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task StartBufferedCommandAsync_ShouldQueueCommand()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                var token = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token;
                
                await Vm.TtyClient.StartBufferedCommandAsync("cat --help", cancellationToken: token);
                Vm.TtyClient.IsAvailableForPrimaryWrite.Should().BeFalse();
                await Vm.TtyClient.WaitForPrimaryAvailabilityAsync(cancellationToken: token);
                AssertHelpOutput();
            })
            .Should().NotThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task WaitForBufferedCommandAsync_ShouldReturnCorrectOutput()
    {
        await FluentActions
            .Awaiting(async () =>
            {
                var token = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token;
                await Vm.TtyClient.WritePrimaryAsync(
                    "cat --help", completionTracker: new ExitSignalCompletionTracker(), cancellationToken: token);
                
                var output = await Vm.TtyClient.WaitForBufferedCommandAsync(cancellationToken: token);
                output.Should().NotBeNull();
                output!.Trim().Should().Be(HelpOutput);
            })
            .Should().NotThrowAsync<OperationCanceledException>();
    }

    private void AssertHelpOutput()
    {
        var output = ((MemoryOutputBuffer)Vm.TtyClient.OutputBuffer!).LastCommit;
        output.Should().NotBeNull();
        output!.Trim().Should().Be(HelpOutput);
    }
}
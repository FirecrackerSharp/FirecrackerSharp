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
        var token = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
        Vm.TtyClient.OutputBuffer = new MemoryOutputBuffer();
        await Vm.TtyClient.WritePrimaryAsync("cat --help", cancellationToken: token);
        await Task.Delay(TimeSpan.FromMilliseconds(500), token);
        Vm.TtyClient.CompletePrimaryWrite();
        AssertHelpOutput();
    }

    [Fact]
    public async Task WritePrimaryAsync_ShouldPerformTrackedWrite()
    {
        var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
        Vm.TtyClient.OutputBuffer = new MemoryOutputBuffer();
        await Vm.TtyClient.WritePrimaryAsync(
            "cat --help", completionTracker: new ExitSignalCompletionTracker(), cancellationToken: token);
        await Vm.TtyClient.WaitForPrimaryAvailabilityAsync(cancellationToken: token);
        AssertHelpOutput();
    }

    [Fact]
    public async Task WriteIntermittentAsync_ShouldPerformNonTrackedWrite()
    {
        var token = new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token;
        await Vm.TtyClient.StartBufferedCommandAsync("read n && echo $n", cancellationToken: token);
        await Vm.TtyClient.WriteIntermittentAsync("sample_input", cancellationToken: token);
        Vm.TtyClient.CompleteIntermittentWrite();
        
        var output = await Vm.TtyClient.WaitForBufferedCommandAsync(cancellationToken: token);
        output.Should().NotBeNull();
        output!.Trim().Should().Be("sample_input\nsample_input");
    }

    [Fact]
    public async Task WriteIntermittentAsync_ShouldPerformTrackedWrite()
    {
        var token = new CancellationTokenSource(TimeSpan.FromSeconds(2)).Token;
        await Vm.TtyClient.StartBufferedCommandAsync("read n && echo q$n", cancellationToken: token);
        await Task.Delay(100, token);
        await Vm.TtyClient.WriteIntermittentAsync("test",
            completionTracker: new StringMatchCompletionTracker(StringMatchMode.Contains, "qtest"),
            cancellationToken: token);
        await Vm.TtyClient.WaitForIntermittentAvailabilityAsync(cancellationToken: token);
        await Vm.TtyClient.WaitForPrimaryAvailabilityAsync(cancellationToken: token);

        var output = await Vm.TtyClient.WaitForBufferedCommandAsync(cancellationToken: token);
        output.Should().NotBeNull();
        output!.Trim().Should().Be("test\nqtest");
    }
    
    [Fact]
    public async Task RunBufferedCommandAsync_ShouldReturnCorrectOutput()
    {
        var output = await Vm.TtyClient.RunBufferedCommandAsync("cat --help",
            cancellationToken: new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token);
        output.Should().NotBeNull();
        output!.Trim().Should().Be(HelpOutput);
    }

    [Fact]
    public async Task StartBufferedCommandAsync_ShouldQueueCommand()
    {
        var token = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token;
        await Vm.TtyClient.StartBufferedCommandAsync("cat --help", cancellationToken: token);
        await Vm.TtyClient.WaitForPrimaryAvailabilityAsync(cancellationToken: token);
        AssertHelpOutput();
    }

    [Fact]
    public async Task WaitForBufferedCommandAsync_ShouldReturnCorrectOutput()
    {
        var token = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)).Token;
        Vm.TtyClient.OutputBuffer = new MemoryOutputBuffer();
        await Vm.TtyClient.WritePrimaryAsync(
            "cat --help", completionTracker: new ExitSignalCompletionTracker(), cancellationToken: token);
        
        var output = await Vm.TtyClient.WaitForBufferedCommandAsync(cancellationToken: token);
        output.Should().NotBeNull();
        output!.Trim().Should().Be(HelpOutput);
    }

    [Fact]
    public async Task TryGetMemoryBufferState_ShouldReturnInProgressAndFinalizedBuffers()
    {
        var token = new CancellationTokenSource(TimeSpan.FromSeconds(3)).Token;
        Vm.TtyClient.OutputBuffer = new MemoryOutputBuffer();
        await Vm.TtyClient.WritePrimaryAsync("cat --help", completionTracker: new ExitSignalCompletionTracker(),
            cancellationToken: token);
    
        var partialBuffer = Vm.TtyClient.TryGetMemoryBufferState();
        partialBuffer.Should().NotBeNull();
        partialBuffer.Should().BeEmpty();
                
        await Vm.TtyClient.WaitForPrimaryAvailabilityAsync(cancellationToken: token);

        var fullBuffer = Vm.TtyClient.TryGetMemoryBufferState();
        fullBuffer.Should().NotBeNull();
        fullBuffer!.Trim().Should().Be(HelpOutput);
    }

    private void AssertHelpOutput()
    {
        var output = ((MemoryOutputBuffer)Vm.TtyClient.OutputBuffer!).LastCommit;
        output.Should().NotBeNull();
        output!.Trim().Should().EndWith(HelpOutput);
    }
}
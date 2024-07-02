using FirecrackerSharp.Tests.Helpers;
using FirecrackerSharp.Tty.Multiplexing;

namespace FirecrackerSharp.Tests.Vmm;

public class VmTtyMultiplexerTests : SingleVmFixture
{
    [Fact]
    public async Task Stub()
    {
        var cmd = await Vm.TtyMultiplexer.StartCommandAsync("cat --help", CaptureMode.StandardOutputAndError);
        await Task.Delay(10);
        await cmd.SendCtrlCAsync();
        var output = await cmd.CaptureOutputIntoMemoryAsync();
        Console.WriteLine(output);
    }
}
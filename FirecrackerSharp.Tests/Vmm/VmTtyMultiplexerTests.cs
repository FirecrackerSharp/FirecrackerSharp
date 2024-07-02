using FirecrackerSharp.Tests.Helpers;
using FirecrackerSharp.Tty.Multiplexing;

namespace FirecrackerSharp.Tests.Vmm;

public class VmTtyMultiplexerTests : SingleVmFixture
{
    [Fact]
    public async Task Stub()
    {
        var cmd = await Vm.TtyMultiplexer.StartCommandAsync("cat --help", CaptureMode.StandardOutputAndError);
        await cmd.SendCtrlCAsync();
        var ocmd = Vm.TtyMultiplexer.GetCommandById(1);
        
        var output = await cmd.CaptureOutputIntoMemoryAsync();
        Console.WriteLine(output);
    }
}
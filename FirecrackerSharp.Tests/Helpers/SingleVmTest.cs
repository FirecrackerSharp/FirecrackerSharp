using FirecrackerSharp.Boot;

namespace FirecrackerSharp.Tests.Helpers;

public class SingleVmTest : IClassFixture<SingleVmTestFactory>
{
    protected Vm Vm;
    
    protected SingleVmTest(SingleVmTestFactory testFactory)
    {
        Vm = testFactory.Vm;
    }
}
using AutoFixture.Xunit2;

namespace FirecrackerSharp.Host.Local.Tests;

public class SampleTest : SshServerFixture
{
    [Theory, AutoData]
    public void SampleTheory(Q q)
    {
        Console.WriteLine("alr");
    }
}
using AutoFixture.Xunit2;
using FirecrackerSharp.Installation;
using FirecrackerSharp.Tests.Fixtures;
using FluentAssertions;
using Octokit;

namespace FirecrackerSharp.Tests;

public class FirecrackerInstallationTests() : FirecrackerInstallationFixture("/tmp/firecracker-installation")
{
     [Fact]
     public async Task FirecrackerInstaller_ShouldSucceed()
     {
          var install = await Installer.InstallAsync();
          await AssertInstallCorrectness(install);
     }

     [Fact]
     public async Task FirecrackerInstaller_ShouldDetectUpdates()
     {
          var updatesAvailable = await Installer.CheckForUpdatesAsync("v1.0.0");
          updatesAvailable.Should().BeTrue();
     }

     [Fact]
     public async Task FirecrackerInstaller_ShouldNotDetectUpdates_WhenThereAreNone()
     {
          var install = await Installer.InstallAsync();
          var updatesAvailable = await Installer.CheckForUpdatesAsync(install.Version);
          updatesAvailable.Should().BeFalse();
     }

     [Fact]
     public async Task FirecrackerInstallManager_ShouldForwardInstallation()
     {
          var install = await InstallManager.InstallAsync(githubCredentials: GithubCredentials);
          await AssertInstallCorrectness(install);
     }

     [Theory, AutoData]
     public async Task AddToIndexAsync_Works(FirecrackerInstall install)
     {
          await InstallManager.AddToIndexAsync(install);

          var returnedInstalls = await InstallManager.GetAllFromIndexAsync();
          returnedInstalls.Should().Contain(install);
     }

     [Theory, AutoData]
     public async Task AddAllToIndexAsync_Works(List<FirecrackerInstall> installs)
     {
          await InstallManager.AddAllToIndexAsync(installs);

          var returnedInstalls = await InstallManager.GetAllFromIndexAsync();
          returnedInstalls.Should().BeEquivalentTo(installs);
     }

     [Theory, AutoData]
     public async Task RemoveFromIndexAsync_ShouldRemove(FirecrackerInstall install)
     {
          await InstallManager.AddToIndexAsync(install);
          await InstallManager.RemoveFromIndexAsync(install.Version);

          var returnedInstalls = await InstallManager.GetAllFromIndexAsync();
          returnedInstalls.Should().BeEmpty();
     }

     [Theory, AutoData]
     public async Task RemoveAllFromIndexAsync_ShouldRemove(List<FirecrackerInstall> installs)
     {
          await InstallManager.AddAllToIndexAsync(installs);
          await InstallManager.RemoveAllFromIndexAsync();

          var returnedInstalls = await InstallManager.GetAllFromIndexAsync();
          returnedInstalls.Should().BeEmpty();
     }

     [Theory, AutoData]
     public async Task GetFromIndexAsync_ShouldPerformLookup(FirecrackerInstall install)
     {
          await InstallManager.AddToIndexAsync(install);
          var returnedInstall = await InstallManager.GetFromIndexAsync(install.Version);
          
          returnedInstall.Should().NotBeNull();
          returnedInstall.Should().Be(install);
     }

     [Fact]
     public async Task GetLatestFromIndexAsync_ShouldReturnLatest()
     {
          await InstallManager.AddToIndexAsync(new FirecrackerInstall("v1.0.0", "", ""));
          await InstallManager.AddToIndexAsync(new FirecrackerInstall("v1.1.0", "", ""));
          
          var latestInstall = await InstallManager.GetLatestFromIndexAsync();
          
          latestInstall.Should().NotBeNull();
          latestInstall!.Version.Should().Be("v1.1.0");
     }

     private static async Task AssertInstallCorrectness(FirecrackerInstall install)
     {
          var githubClient = new GitHubClient(new ProductHeaderValue("FirecrackerSharp-Tests"))
          {
               Credentials = GithubCredentials
          };
          var latestRelease = await githubClient.Repository.Release.GetLatest("firecracker-microvm", "firecracker");
          
          install.Version.Should().Be(latestRelease.TagName);
          File.Exists(install.FirecrackerBinary).Should().BeTrue();
          File.Exists(install.JailerBinary).Should().BeTrue();
#pragma warning disable CA1416
          File.GetUnixFileMode(install.FirecrackerBinary).Should()
               .HaveFlag(UnixFileMode.UserRead).And.HaveFlag(UnixFileMode.UserExecute);
          File.GetUnixFileMode(install.JailerBinary).Should()
               .HaveFlag(UnixFileMode.UserRead).And.HaveFlag(UnixFileMode.UserExecute);
#pragma warning restore CA1416
     }
}
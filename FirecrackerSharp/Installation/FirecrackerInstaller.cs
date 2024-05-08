using System.Security.Cryptography;
using System.Text;
using Octokit;
using Serilog;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace FirecrackerSharp.Installation;

public class FirecrackerInstaller(
    string installRoot,
    string releaseTag = "latest",
    string repoOwner = "firecracker-microvm",
    string repoName = "firecracker")
{
    private static readonly HttpClient HttpClient = new();
    private static readonly ILogger Logger = Log.ForContext(typeof(FirecrackerInstaller));
    
    public async Task<FirecrackerInstall> InstallAsync()
    {
        var (archiveAsset, archiveChecksumAsset, release) = await FetchAssetsFromApiAsync();
        var fetchedReleaseTag = release.TagName;
        var installDirectory = Path.Join(installRoot, fetchedReleaseTag, Guid.NewGuid().ToString());
        Directory.CreateDirectory(installDirectory);
        var archivePath = await DownloadAssetsAndVerifyAsync(archiveAsset, archiveChecksumAsset);
        var firecracker = await ExtractToInstallRootAsync(archivePath, installDirectory, fetchedReleaseTag);

        Logger.Information("Installed Firecracker {tag} to: {installDirectory}", fetchedReleaseTag, installDirectory);
        return firecracker;
    }

    private async Task<(ReleaseAsset, ReleaseAsset, Release)> FetchAssetsFromApiAsync()
    {
        var githubClient = new GitHubClient(new ProductHeaderValue("FirecrackerSharp"));
        var repository = await githubClient.Repository.Get(repoOwner, repoName);
        if (repository is null)
        {
            throw new FirecrackerInstallationException(
                $"Could not the Firecracker GitHub repository owned by {repoOwner} and named {repoName}");
        }
        Logger.Debug("Fetched GitHub repository from API");

        var releases = await githubClient.Repository.Release.GetAll(repository.Id);
        if (releases is null)
        {
            throw new FirecrackerInstallationException("Could not query Firecracker releases on the specified repository");
        }
        Logger.Debug("Fetched GitHub releases from API");

        var release = releaseTag == "latest"
            ? releases.MaxBy(x => x.CreatedAt)
            : releases.FirstOrDefault(x => x.TagName == releaseTag);
        if (release is null)
        {
            throw new FirecrackerInstallationException(
                releaseTag == "latest" ? "Could not find the latest release" : $"Could not find the release with the tag {releaseTag}");
        }

        var archiveAsset = release.Assets.FirstOrDefault(x => x.Name.EndsWith("x86_64.tgz"));
        var archiveChecksumAsset = release.Assets.FirstOrDefault(x => x.Name.EndsWith("x86_64.tgz.sha256.txt"));
        if (archiveAsset is null || archiveChecksumAsset is null)
        {
            throw new FirecrackerInstallationException("Could not fetch the necessary x86_64 archive and checksum");
        }

        return (archiveAsset, archiveChecksumAsset, release);
    }

    private async Task<FirecrackerInstall> ExtractToInstallRootAsync(string archivePath, string installDirectory,
        string fetchedReleaseTag)
    {
        var temporaryDirectory = Directory.CreateTempSubdirectory().FullName;

        await using (Stream stream = File.OpenRead(archivePath))
        {            
            var reader = ReaderFactory.Open(stream);
            while (reader.MoveToNextEntry())
            {
                if (!reader.Entry.IsDirectory)
                    reader.WriteEntryToDirectory(temporaryDirectory,
                        new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
            }
        }
        Logger.Debug("Extracted Firecracker binaries archive");

        var subdirectoryPath = Directory.GetDirectories(temporaryDirectory).FirstOrDefault();
        if (subdirectoryPath is null)
        {
            throw new FirecrackerInstallationException("The extracted Firecracker directory doesn't contain the" +
                                                  "expected subdirectory");
        }

        var files = Directory.GetFiles(subdirectoryPath);
        var firecrackerBinaryPath = files.First(x => x.Contains("firecracker") && !x.Contains("debug"));
        var jailerBinaryPath = files.First(x => x.Contains("jailer") && !x.Contains("debug"));
        var newFirecrackerBinaryPath = Path.Join(installDirectory, "firecracker");
        var newJailerBinaryPath = Path.Join(installDirectory, "jailer");
        File.Copy(firecrackerBinaryPath, newFirecrackerBinaryPath);
        File.Copy(jailerBinaryPath, newJailerBinaryPath);
#pragma warning disable CA1416
        File.SetUnixFileMode(newFirecrackerBinaryPath, UnixFileMode.UserExecute);
        File.SetUnixFileMode(newJailerBinaryPath, UnixFileMode.UserExecute);
#pragma warning restore CA1416
        
        Directory.Delete(temporaryDirectory, recursive: true);
        File.Delete(archivePath);
        
        return new FirecrackerInstall(fetchedReleaseTag, newFirecrackerBinaryPath, newJailerBinaryPath);
    }

    private static async Task<string> DownloadAssetsAndVerifyAsync(ReleaseAsset archiveAsset, ReleaseAsset archiveChecksumAsset)
    {
        var expectedChecksum = await HttpClient.GetStringAsync(archiveChecksumAsset.BrowserDownloadUrl);
        expectedChecksum = expectedChecksum.Split(" ").First();
        
        var archivePath = Path.GetTempFileName() + ".tar.gz";
        var archiveBytes = await HttpClient.GetByteArrayAsync(archiveAsset.BrowserDownloadUrl);
        Logger.Debug("Downloaded Firecracker binaries archive");

        var actualChecksum = ToHex(SHA256.HashData(archiveBytes));
        if (expectedChecksum != actualChecksum)
        {
            throw new FirecrackerInstallationException("The actual checksum doesn't match the expected one from GitHub");
        }
        Logger.Debug("Verified checksums successfully");
        
        await File.WriteAllBytesAsync(archivePath, archiveBytes);

        return archivePath;
    }
    
    private static string ToHex(IReadOnlyCollection<byte> bytes)
    {
        var result = new StringBuilder(bytes.Count * 2);
        foreach (var t in bytes)
            result.Append(t.ToString("x2"));

        return result.ToString();
    }
}
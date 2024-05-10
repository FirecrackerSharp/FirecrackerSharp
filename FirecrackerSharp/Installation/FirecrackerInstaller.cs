using System.Security.Cryptography;
using System.Text;
using FirecrackerSharp.Transport;
using Octokit;
using Serilog;

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
        IFirecrackerTransport.Current.CreateDirectory(installDirectory);
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

    private static async Task<FirecrackerInstall> ExtractToInstallRootAsync(string archivePath, string installDirectory,
        string fetchedReleaseTag)
    {
        var temporaryDirectory = IFirecrackerTransport.Current.CreateTemporaryDirectory();
        await IFirecrackerTransport.Current.ExtractGzipAsync(archivePath, temporaryDirectory);
        Logger.Debug("Extracted Firecracker binaries archive");

        var subdirectoryPath = IFirecrackerTransport.Current
            .GetSubdirectories(temporaryDirectory)
            .FirstOrDefault();
        if (subdirectoryPath is null)
        {
            throw new FirecrackerInstallationException("The extracted Firecracker directory doesn't contain the" +
                                                  "expected subdirectory");
        }

        var files = IFirecrackerTransport.Current
            .GetFiles(subdirectoryPath)
            .ToList();
        var firecrackerBinaryPath = files.First(x => x.Contains("firecracker") && !x.Contains("debug"));
        var jailerBinaryPath = files.First(x => x.Contains("jailer") && !x.Contains("debug"));
        var newFirecrackerBinaryPath = Path.Join(installDirectory, "firecracker");
        var newJailerBinaryPath = Path.Join(installDirectory, "jailer");
        
        await Task.WhenAll([
            IFirecrackerTransport.Current.CopyFileAsync(firecrackerBinaryPath, newFirecrackerBinaryPath),
            IFirecrackerTransport.Current.CopyFileAsync(jailerBinaryPath, newJailerBinaryPath)
        ]);
        
        IFirecrackerTransport.Current.MakeFileExecutable(newFirecrackerBinaryPath);
        IFirecrackerTransport.Current.MakeFileExecutable(newJailerBinaryPath);
        
        IFirecrackerTransport.Current.DeleteDirectoryRecursively(temporaryDirectory);
        IFirecrackerTransport.Current.DeleteFile(archivePath);
        
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

        await IFirecrackerTransport.Current.WriteBinaryFileAsync(archivePath, archiveBytes);

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
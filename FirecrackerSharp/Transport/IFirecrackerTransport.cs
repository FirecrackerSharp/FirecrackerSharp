namespace FirecrackerSharp.Transport;

public interface IFirecrackerTransport
{
    public static IFirecrackerTransport Current { internal get; set; } = null!;

    internal Task WriteTextFileAsync(string path, string content);

    internal Task WriteBinaryFileAsync(string path, byte[] content);

    internal Task<string> ReadTextFileAsync(string path);

    internal Task CopyFileAsync(string sourcePath, string destinationPath);

    internal void CreateDirectory(string path);

    internal IEnumerable<string> GetSubdirectories(string path);

    internal IEnumerable<string> GetFiles(string path);

    internal Task ExtractGzipAsync(string archivePath, string destinationPath);

    internal void MakeFileExecutable(string path);

    internal void DeleteFile(string path);

    internal void DeleteDirectoryRecursively(string path);

    internal string JoinPaths(params string[] paths);

    internal string CreateTemporaryDirectory();
}
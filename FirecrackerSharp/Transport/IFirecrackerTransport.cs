namespace FirecrackerSharp.Transport;

public interface IFirecrackerTransport
{
    public static IFirecrackerTransport Current { internal get; set; } = null!;

    public Task WriteTextFileAsync(string path, string content);

    public Task WriteBinaryFileAsync(string path, byte[] content);

    public Task<string> ReadTextFileAsync(string path);

    public Task CopyFileAsync(string sourcePath, string destinationPath);

    public string GetTemporaryFilename();

    public void CreateDirectory(string path);

    public IEnumerable<string> GetSubdirectories(string path);

    public IEnumerable<string> GetFiles(string path);

    public Task ExtractGzipAsync(string archivePath, string destinationPath);

    public void MakeFileExecutable(string path);

    public void DeleteFile(string path);

    public void DeleteDirectoryRecursively(string path);

    public string CreateTemporaryDirectory();
}
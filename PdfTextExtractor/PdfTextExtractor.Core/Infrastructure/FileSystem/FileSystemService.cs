namespace PdfTextExtractor.Core.Infrastructure.FileSystem;

/// <summary>
/// Implementation of file system operations.
/// </summary>
public class FileSystemService : IFileSystemService
{
    public string[] GetPdfFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"PDF folder not found: {folderPath}");

        return Directory.GetFiles(folderPath, "*.pdf", SearchOption.TopDirectoryOnly);
    }

    public string[] GetTextFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            return Array.Empty<string>();

        return Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly);
    }

    public void EnsureDirectoryExists(string folderPath)
    {
        Directory.CreateDirectory(folderPath);
    }

    public bool FileExists(string filePath) => File.Exists(filePath);

    public async Task<string> ReadTextFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Text file not found: {filePath}", filePath);

        return await File.ReadAllTextAsync(filePath, cancellationToken);
    }
}

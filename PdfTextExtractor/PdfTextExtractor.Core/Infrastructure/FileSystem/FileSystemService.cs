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
}

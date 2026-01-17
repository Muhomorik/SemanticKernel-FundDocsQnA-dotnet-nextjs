namespace PdfTextExtractor.Core.Infrastructure.FileSystem;

/// <summary>
/// Service for file system operations.
/// </summary>
public interface IFileSystemService
{
    string[] GetPdfFiles(string folderPath);
    string[] GetTextFiles(string folderPath);
    void EnsureDirectoryExists(string folderPath);
}

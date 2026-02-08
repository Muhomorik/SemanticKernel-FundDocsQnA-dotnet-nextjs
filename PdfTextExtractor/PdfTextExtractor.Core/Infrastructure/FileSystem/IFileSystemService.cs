namespace PdfTextExtractor.Core.Infrastructure.FileSystem;

/// <summary>
/// Service for file system operations.
/// </summary>
public interface IFileSystemService
{
    string[] GetPdfFiles(string folderPath);
    string[] GetTextFiles(string folderPath);
    void EnsureDirectoryExists(string folderPath);

    /// <summary>
    /// Checks if a file exists at the specified path.
    /// </summary>
    bool FileExists(string filePath);

    /// <summary>
    /// Reads the entire contents of a text file asynchronously.
    /// </summary>
    /// <exception cref="FileNotFoundException">If file does not exist</exception>
    /// <exception cref="IOException">If file cannot be read</exception>
    Task<string> ReadTextFileAsync(string filePath, CancellationToken cancellationToken = default);
}

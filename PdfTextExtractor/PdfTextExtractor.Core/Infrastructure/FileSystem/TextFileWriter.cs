using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Infrastructure.FileSystem;

/// <summary>
/// Implementation of text file writing operations.
/// </summary>
public class TextFileWriter : ITextFileWriter
{
    public async Task WriteTextFileAsync(string filePath, string content, CancellationToken cancellationToken = default)
    {
        await File.WriteAllTextAsync(filePath, content, cancellationToken);
    }

    public async Task WritePagesAsync(string outputFolderPath, string pdfFileName, IEnumerable<DocumentPage> pages, CancellationToken cancellationToken = default)
    {
        foreach (var page in pages)
        {
            var fileName = $"{Path.GetFileNameWithoutExtension(pdfFileName)}_page_{page.PageNumber}.txt";
            var filePath = Path.Combine(outputFolderPath, fileName);
            await WriteTextFileAsync(filePath, page.PageText, cancellationToken);
        }
    }
}

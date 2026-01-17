namespace PdfTextExtractor.Core.Configuration;

/// <summary>
/// Parameters for Ollama OCR-based text extraction (planned for future implementation).
/// </summary>
public class OllamaParameters
{
    public required string PdfFolderPath { get; init; }
    public required string OutputFolderPath { get; init; }
    public string OllamaUrl { get; init; } = "http://localhost:11434";
    public required string VisionModelName { get; init; }
    public int RasterizationDpi { get; init; } = 300;
    public int ChunkSize { get; init; } = 1000;
}

namespace PdfTextExtractor.Core.Domain.ValueObjects;

/// <summary>
/// Supported text extraction methods (value object wrapper).
/// </summary>
public sealed record ExtractorType
{
    public string Value { get; }

    private ExtractorType(string value)
    {
        Value = value;
    }

    public static readonly ExtractorType PdfPig = new("PdfPig");
    public static readonly ExtractorType LMStudio = new("LMStudio");
    public static readonly ExtractorType Ollama = new("Ollama");

    public static ExtractorType FromString(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "pdfpig" => PdfPig,
            "lmstudio" => LMStudio,
            "ollama" => Ollama,
            _ => throw new ArgumentException($"Unknown extractor type: {value}", nameof(value))
        };
    }
}

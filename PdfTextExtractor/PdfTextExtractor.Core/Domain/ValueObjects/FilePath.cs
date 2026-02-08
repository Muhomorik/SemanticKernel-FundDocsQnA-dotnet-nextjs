namespace PdfTextExtractor.Core.Domain.ValueObjects;

/// <summary>
/// Immutable, validated file path value object.
/// </summary>
public sealed record FilePath
{
    public string Value { get; }

    private FilePath(string value)
    {
        Value = value;
    }

    public static FilePath Create(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("File path cannot be empty.", nameof(path));

        if (path.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new ArgumentException($"File path contains invalid characters: {path}", nameof(path));

        return new FilePath(Path.GetFullPath(path));
    }

    public string FileName => Path.GetFileName(Value);
    public string Directory => Path.GetDirectoryName(Value) ?? string.Empty;
    public string Extension => Path.GetExtension(Value);
}

using Backend.API.Domain.Models;
using Backend.API.Infrastructure.Search.Models;

namespace Backend.API.Infrastructure.Search.Mapping;

/// <summary>
/// Maps between domain DocumentChunk and infrastructure DocumentChunkRecord.
/// </summary>
public static class DocumentChunkMapper
{
    /// <summary>
    /// Converts a domain DocumentChunk to an infrastructure record for VectorStore.
    /// </summary>
    public static DocumentChunkRecord ToRecord(DocumentChunk chunk)
    {
        return new DocumentChunkRecord
        {
            Id = chunk.Id,
            Text = chunk.Text,
            Source = chunk.Metadata.Source,
            Page = chunk.Metadata.Page,
            Vector = new ReadOnlyMemory<float>(chunk.Vector.Values)
        };
    }

    /// <summary>
    /// Converts an infrastructure record back to a domain DocumentChunk.
    /// </summary>
    public static DocumentChunk ToDomain(DocumentChunkRecord record)
    {
        return DocumentChunk.Create(
            record.Id,
            record.Text,
            record.Vector.ToArray(),
            record.Source,
            record.Page);
    }
}

using AutoFixture.Kernel;
using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class DocumentChunkBuilder : ISpecimenBuilder
{
    private static readonly Random _random = new();

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(DocumentChunk))
        {
            return new DocumentChunk
            {
                SourceFile = $"test-document-{Guid.NewGuid():N}.pdf",
                PageNumber = _random.Next(1, 100),
                ChunkIndex = _random.Next(0, 50),
                Content = $"Test chunk content with meaningful text. {Guid.NewGuid()}"
            };
        }

        return new NoSpecimen();
    }
}

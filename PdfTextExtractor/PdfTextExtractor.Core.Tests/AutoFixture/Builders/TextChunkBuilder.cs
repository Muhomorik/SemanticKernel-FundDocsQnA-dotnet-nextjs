using AutoFixture;
using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.Entities;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class TextChunkBuilder : ISpecimenBuilder
{
    private static readonly Random _random = new();

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(TextChunk))
        {
            var pageNumber = SpecimenFactory.Create<PageNumber>(context);
            var chunkIndex = _random.Next(0, 100);
            var content = SpecimenFactory.Create<ChunkContent>(context);

            return TextChunk.Create(pageNumber, chunkIndex, content);
        }

        return new NoSpecimen();
    }
}

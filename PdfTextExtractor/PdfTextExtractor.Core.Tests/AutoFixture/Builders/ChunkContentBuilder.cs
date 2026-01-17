using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class ChunkContentBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(ChunkContent))
        {
            var content = $"Sample chunk content with meaningful text. {Guid.NewGuid()}";
            return ChunkContent.Create(content);
        }

        return new NoSpecimen();
    }
}

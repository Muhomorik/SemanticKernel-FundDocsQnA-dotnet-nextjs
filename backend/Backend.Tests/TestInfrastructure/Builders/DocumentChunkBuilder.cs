using AutoFixture.Kernel;
using Backend.API.Domain.Models;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for DocumentChunk.
/// Uses the factory method DocumentChunk.Create() to ensure proper validation.
/// </summary>
public class DocumentChunkBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(DocumentChunk))
        {
            var id = Guid.NewGuid().ToString();
            var text = $"Sample text content {Guid.NewGuid()}";
            var embedding = Enumerable.Range(0, 1536)
                .Select(_ => (float)(Random.Shared.NextDouble() * 2 - 1))
                .ToArray();
            var source = $"document-{Guid.NewGuid()}.pdf";
            var page = Random.Shared.Next(1, 100);

            return DocumentChunk.Create(id, text, embedding, source, page);
        }

        return new NoSpecimen();
    }
}

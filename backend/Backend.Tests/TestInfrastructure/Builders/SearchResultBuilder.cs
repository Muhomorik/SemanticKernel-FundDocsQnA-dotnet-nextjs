using AutoFixture.Kernel;
using Backend.API.Domain.Models;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for SearchResult.
/// Creates search results with valid similarity scores in range [0, 1].
/// </summary>
public class SearchResultBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SearchResult))
        {
            var chunk = (DocumentChunk)context.Resolve(typeof(DocumentChunk));
            var score = (float)Random.Shared.NextDouble(); // 0.0 to 1.0

            return new SearchResult(chunk, score);
        }

        return new NoSpecimen();
    }
}

using AutoFixture.Kernel;
using Backend.API.Domain.ValueObjects;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for EmbeddingVector.
/// Creates valid vectors with realistic dimensions (1536 for OpenAI embeddings).
/// </summary>
public class EmbeddingVectorBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(EmbeddingVector))
        {
            // OpenAI text-embedding-3-small uses 1536 dimensions
            // Generate random normalized vector values in range [-1, 1]
            var values = Enumerable.Range(0, 1536)
                .Select(_ => (float)(Random.Shared.NextDouble() * 2 - 1))
                .ToArray();

            return new EmbeddingVector(values);
        }

        return new NoSpecimen();
    }
}

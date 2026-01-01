using AutoFixture.Kernel;
using Backend.API.Domain.ValueObjects;

namespace Backend.Tests.TestInfrastructure.Builders;

/// <summary>
/// AutoFixture specimen builder for DocumentMetadata.
/// Creates metadata with valid source files and page numbers.
/// </summary>
public class DocumentMetadataBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(DocumentMetadata))
        {
            var source = $"test-document-{Guid.NewGuid()}.pdf";
            var page = Random.Shared.Next(1, 100); // Pages start at 1

            return new DocumentMetadata(source, page);
        }

        return new NoSpecimen();
    }
}

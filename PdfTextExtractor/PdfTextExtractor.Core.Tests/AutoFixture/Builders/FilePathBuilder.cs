using AutoFixture.Kernel;
using PdfTextExtractor.Core.Domain.ValueObjects;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class FilePathBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(FilePath))
        {
            var fileName = $"test-document-{Guid.NewGuid():N}.pdf";
            var directory = Path.Combine(Path.GetTempPath(), "PdfTextExtractor.Tests");
            var fullPath = Path.Combine(directory, fileName);
            return FilePath.Create(fullPath);
        }

        return new NoSpecimen();
    }
}

using AutoFixture.Kernel;
using PdfTextExtractor.Core.Configuration;

namespace PdfTextExtractor.Core.Tests.AutoFixture.Builders;

public class OpenAIParametersBuilder : ISpecimenBuilder
{
    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(OpenAIParameters))
        {
            return new OpenAIParameters
            {
                PdfFolderPath = Path.Combine(Path.GetTempPath(), "PdfTextExtractor.Tests", "pdfs"),
                OutputFolderPath = Path.Combine(Path.GetTempPath(), "PdfTextExtractor.Tests", "output"),
                ApiKey = "test-api-key",
                VisionModelName = "gpt-4o",
                RasterizationDpi = 300,
                MaxTokens = 2000,
                DetailLevel = "high",
                ExtractionPrompt = "TEST: Extract text from image"
            };
        }

        return new NoSpecimen();
    }
}

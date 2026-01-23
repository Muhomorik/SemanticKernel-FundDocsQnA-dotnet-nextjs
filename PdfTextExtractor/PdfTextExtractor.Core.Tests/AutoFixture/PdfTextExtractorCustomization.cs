using AutoFixture;
using Moq;
using PdfTextExtractor.Core.Domain.Events;
using PdfTextExtractor.Core.Infrastructure.EventBus;
using PdfTextExtractor.Core.Infrastructure.FileSystem;
using PdfTextExtractor.Core.Infrastructure.LMStudio;
using PdfTextExtractor.Core.Infrastructure.OpenAI;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Tests.AutoFixture.Builders;

namespace PdfTextExtractor.Core.Tests.AutoFixture;

/// <summary>
/// Custom AutoFixture customization for PdfTextExtractor.Core test suite.
/// Registers all custom builders and mock configurations.
/// </summary>
public class PdfTextExtractorCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        // Add custom specimen builders for value objects
        fixture.Customizations.Add(new FilePathBuilder());
        fixture.Customizations.Add(new PageNumberBuilder());
        fixture.Customizations.Add(new PageContentBuilder());
        fixture.Customizations.Add(new SessionIdBuilder());
        fixture.Customizations.Add(new CorrelationIdBuilder());
        fixture.Customizations.Add(new ExtractorTypeBuilder());

        // Add custom specimen builders for entities
        fixture.Customizations.Add(new PageBuilder());
        fixture.Customizations.Add(new DocumentBuilder());
        fixture.Customizations.Add(new ExtractionSessionBuilder());

        // Add custom specimen builders for models and events
        fixture.Customizations.Add(new DocumentPageBuilder());
        fixture.Customizations.Add(new PdfExtractionEventBuilder());
        fixture.Customizations.Add(new LMStudioParametersBuilder());
        fixture.Customizations.Add(new OpenAIParametersBuilder());

        // Configure mock registrations for interfaces
        fixture.Register<IEventPublisher>(() =>
            fixture.Freeze<Mock<IEventPublisher>>().Object);

        fixture.Register<IFileSystemService>(() =>
            fixture.Freeze<Mock<IFileSystemService>>().Object);

        fixture.Register<ITextFileWriter>(() =>
            fixture.Freeze<Mock<ITextFileWriter>>().Object);

        fixture.Register<IRasterizationService>(() =>
            fixture.Freeze<Mock<IRasterizationService>>().Object);

        fixture.Register<ILMStudioVisionClient>(() =>
            fixture.Freeze<Mock<ILMStudioVisionClient>>().Object);

        fixture.Register<IOpenAIVisionClient>(() =>
            fixture.Freeze<Mock<IOpenAIVisionClient>>().Object);
    }
}

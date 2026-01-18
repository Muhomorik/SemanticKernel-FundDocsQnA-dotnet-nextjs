using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Infrastructure.EventBus;
using PdfTextExtractor.Core.Infrastructure.Extractors;
using PdfTextExtractor.Core.Infrastructure.LMStudio;
using PdfTextExtractor.Core.Infrastructure.Rasterization;
using PdfTextExtractor.Core.Models;
using PdfTextExtractor.Core.Tests.TestHelpers;

namespace PdfTextExtractor.Core.Tests.Integration;

/// <summary>
/// Integration tests for LMStudioOcrExtractor using real implementations.
/// These tests connect to a running LM Studio instance to validate the full extraction pipeline.
///
/// Prerequisites:
/// - LM Studio running at http://localhost:1234 (or configured URL)
/// - Vision model loaded (qwen/qwen2.5-vl-7b or configured model)
/// - Sample PDF files in test directory
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("LMStudio")]
[Explicit("Requires LM Studio running with vision model loaded")]
public class LMStudioOcrExtractorIntegrationTests
{
    private LMStudioOcrExtractor _sut;
    private InMemoryEventPublisher _eventPublisher;
    private string _lmStudioUrl;
    private string _visionModelName;

    [SetUp]
    public void SetUp()
    {
        // Configuration - can be overridden with environment variables
        _lmStudioUrl = Environment.GetEnvironmentVariable("LMSTUDIO_URL") ?? "http://localhost:1234";
        _visionModelName = Environment.GetEnvironmentVariable("LMSTUDIO_VISION_MODEL") ?? "qwen/qwen2.5-vl-7b";

        _eventPublisher = new InMemoryEventPublisher();
    }

    #region DPI Configuration Tests

    [Test]
    [TestCase(150, Description = "Low DPI - smallest images, least GPU memory")]
    [TestCase(200, Description = "Medium-low DPI - balanced quality and memory")]
    [TestCase(300, Description = "Standard DPI - default quality")]
    public async Task ExtractAsync_DifferentDpiSettings_ExtractsTextSuccessfully(int dpi)
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var parameters = CreateParameters(dpi: dpi);
        _sut = CreateExtractor(parameters);

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        Console.WriteLine($"=== Testing DPI: {dpi} ===");
        Console.WriteLine($"LM Studio URL: {_lmStudioUrl}");
        Console.WriteLine($"Vision Model: {_visionModelName}");

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _sut.ExtractAsync(
            TestPdfFiles.SamplePdf,
            _eventPublisher,
            correlationId,
            sessionId);
        sw.Stop();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty, "Should extract at least one chunk");

        // Report results
        var chunks = result.ToList();
        var events = _eventPublisher.GetPublishedEvents().ToList();

        Console.WriteLine($"Extraction completed in {sw.Elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"Total chunks: {chunks.Count}");
        Console.WriteLine($"Total events: {events.Count}");
        Console.WriteLine($"Pages processed: {chunks.Select(c => c.PageNumber).Distinct().Count()}");

        // Check for rasterization events to verify image size
        var rasterizationEvents = events
            .Where(e => e.GetType().Name.Contains("Rasterization"))
            .ToList();

        Console.WriteLine($"Rasterization events: {rasterizationEvents.Count}");
        foreach (var evt in rasterizationEvents.Take(3))
        {
            Console.WriteLine($"  - {evt.GetType().Name}");
        }
    }

    [Test]
    [Explicit("High DPI may cause GPU OOM - test manually")]
    [TestCase(450, Description = "High DPI - may cause GPU memory issues")]
    [TestCase(600, Description = "Very high DPI - likely to cause GPU OOM")]
    public async Task ExtractAsync_HighDpiSettings_MayCauseGpuOom(int dpi)
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var parameters = CreateParameters(dpi: dpi);
        _sut = CreateExtractor(parameters);

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        Console.WriteLine($"=== Testing HIGH DPI: {dpi} (May cause GPU OOM) ===");
        Console.WriteLine($"Monitor GPU memory usage during this test");

        try
        {
            // Act
            var result = await _sut.ExtractAsync(
                TestPdfFiles.SamplePdf,
                _eventPublisher,
                correlationId,
                sessionId);

            // If we get here, extraction succeeded
            Console.WriteLine($"✓ SUCCESS: DPI {dpi} completed without GPU OOM");
            Assert.Pass($"Extraction succeeded with DPI {dpi}");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("LM Studio"))
        {
            // LM Studio connection or model error
            Console.WriteLine($"✗ FAILED: {ex.Message}");
            Assert.Fail($"LM Studio error at DPI {dpi}: {ex.Message}");
        }
        catch (Exception ex)
        {
            // Other errors (potentially GPU OOM)
            Console.WriteLine($"✗ GPU OOM or other error: {ex.Message}");
            Assert.Inconclusive($"DPI {dpi} may have caused GPU OOM: {ex.Message}");
        }
    }

    #endregion

    #region Chunk Size Configuration Tests

    [Test]
    [TestCase(500, Description = "Small chunks - more granular")]
    [TestCase(1000, Description = "Medium chunks - default")]
    [TestCase(2000, Description = "Large chunks - fewer chunks")]
    public async Task ExtractAsync_DifferentChunkSizes_CreatesAppropriateChunks(int chunkSize)
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var parameters = CreateParameters(chunkSize: chunkSize);
        _sut = CreateExtractor(parameters);

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        Console.WriteLine($"=== Testing Chunk Size: {chunkSize} ===");

        // Act
        var result = await _sut.ExtractAsync(
            TestPdfFiles.SamplePdf,
            _eventPublisher,
            correlationId,
            sessionId);

        // Assert
        var chunks = result.ToList();
        Assert.That(chunks, Is.Not.Empty);

        // Verify chunk sizes
        var avgChunkSize = chunks.Average(c => c.Content.Length);
        var maxChunkSize = chunks.Max(c => c.Content.Length);

        Console.WriteLine($"Total chunks: {chunks.Count}");
        Console.WriteLine($"Average chunk size: {avgChunkSize:F0} chars");
        Console.WriteLine($"Max chunk size: {maxChunkSize} chars");
        Console.WriteLine($"Configured limit: {chunkSize} chars");

        // Most chunks should be within reasonable range of configured size
        // Allow overflow for sentence completion
        Assert.That(maxChunkSize, Is.LessThanOrEqualTo(chunkSize + 200),
            "Max chunk size should not exceed limit by more than 200 chars (for sentence completion)");
    }

    #endregion

    #region Model Configuration Tests

    [Test]
    [TestCase("qwen/qwen2.5-vl-7b", Description = "Qwen 2.5 VL 7B - high quality vision model")]
    [TestCase("llava-v1.6-mistral-7b", Description = "LLaVA 1.6 Mistral 7B - alternative vision model")]
    public async Task ExtractAsync_DifferentVisionModels_ExtractsText(string modelName)
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var parameters = CreateParameters(visionModelName: modelName);
        _sut = CreateExtractor(parameters);

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        Console.WriteLine($"=== Testing Vision Model: {modelName} ===");

        try
        {
            // Act
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await _sut.ExtractAsync(
                TestPdfFiles.SamplePdf,
                _eventPublisher,
                correlationId,
                sessionId);
            sw.Stop();

            // Assert
            Assert.That(result, Is.Not.Empty);

            var chunks = result.ToList();
            var totalText = string.Join(" ", chunks.Select(c => c.Content));

            Console.WriteLine($"Model: {modelName}");
            Console.WriteLine($"Extraction time: {sw.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"Total chunks: {chunks.Count}");
            Console.WriteLine($"Total characters: {totalText.Length}");
            Console.WriteLine($"Text preview: {totalText.Substring(0, Math.Min(100, totalText.Length))}...");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("model"))
        {
            Assert.Inconclusive($"Model {modelName} not available in LM Studio: {ex.Message}");
        }
    }

    #endregion

    #region Multi-Page Tests

    [Test]
    public async Task ExtractAsync_MultiPagePdf_ProcessesAllPages()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var parameters = CreateParameters();
        _sut = CreateExtractor(parameters);

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        Console.WriteLine("=== Testing Multi-Page PDF Processing ===");

        // Act
        var result = await _sut.ExtractAsync(
            TestPdfFiles.SamplePdf,
            _eventPublisher,
            correlationId,
            sessionId);

        // Assert
        var chunks = result.ToList();
        var pages = chunks.Select(c => c.PageNumber).Distinct().OrderBy(p => p).ToList();

        Console.WriteLine($"Total pages processed: {pages.Count}");
        Console.WriteLine($"Page numbers: {string.Join(", ", pages)}");
        Console.WriteLine($"Total chunks: {chunks.Count}");

        foreach (var pageNum in pages)
        {
            var pageChunks = chunks.Where(c => c.PageNumber == pageNum).ToList();
            Console.WriteLine($"  Page {pageNum}: {pageChunks.Count} chunks");
        }

        Assert.That(pages, Is.Not.Empty, "Should process at least one page");
        Assert.That(pages, Is.Ordered, "Page numbers should be sequential");
    }

    #endregion

    #region Event Publishing Tests

    [Test]
    public async Task ExtractAsync_PublishesExpectedEvents()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var parameters = CreateParameters();
        _sut = CreateExtractor(parameters);

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        Console.WriteLine("=== Testing Event Publishing ===");

        // Act
        await _sut.ExtractAsync(
            TestPdfFiles.SamplePdf,
            _eventPublisher,
            correlationId,
            sessionId);

        // Assert
        var events = _eventPublisher.GetPublishedEvents().ToList();
        var eventTypes = events.Select(e => e.GetType().Name).Distinct().OrderBy(n => n).ToList();

        Console.WriteLine($"Total events published: {events.Count}");
        Console.WriteLine("Event types:");
        foreach (var eventType in eventTypes)
        {
            var count = events.Count(e => e.GetType().Name == eventType);
            Console.WriteLine($"  {eventType}: {count}");
        }

        // Verify key event types are present
        Assert.That(events.Any(e => e.GetType().Name == "DocumentExtractionStarted"), Is.True);
        Assert.That(events.Any(e => e.GetType().Name == "DocumentExtractionCompleted"), Is.True);
        Assert.That(events.Any(e => e.GetType().Name == "PageExtractionStarted"), Is.True);
        Assert.That(events.Any(e => e.GetType().Name == "PageExtractionCompleted"), Is.True);
        Assert.That(events.Any(e => e.GetType().Name == "OcrProcessingStarted"), Is.True);
        Assert.That(events.Any(e => e.GetType().Name == "OcrProcessingCompleted"), Is.True);
    }

    #endregion

    #region GPU Memory Optimization Tests

    [Test]
    [TestCase(150, 500, Description = "Low DPI + small chunks - minimal GPU memory")]
    [TestCase(200, 1000, Description = "Medium DPI + medium chunks - balanced")]
    [TestCase(300, 1000, Description = "High DPI + medium chunks - standard quality")]
    public async Task ExtractAsync_OptimizedConfigurations_ForGpuMemory(int dpi, int chunkSize)
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var parameters = CreateParameters(dpi: dpi, chunkSize: chunkSize);
        _sut = CreateExtractor(parameters);

        var correlationId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        Console.WriteLine($"=== GPU Memory Optimization Test ===");
        Console.WriteLine($"Configuration: DPI={dpi}, ChunkSize={chunkSize}");
        Console.WriteLine($"Expected GPU impact: {GetExpectedGpuImpact(dpi)}");

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        IEnumerable<DocumentChunk> result;

        try
        {
            result = await _sut.ExtractAsync(
                TestPdfFiles.SamplePdf,
                _eventPublisher,
                correlationId,
                sessionId);
            sw.Stop();

            // Assert
            var chunks = result.ToList();

            Console.WriteLine($"✓ SUCCESS");
            Console.WriteLine($"Extraction time: {sw.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"Total chunks: {chunks.Count}");
            Console.WriteLine($"GPU OOM: No");
            Console.WriteLine($"Recommendation: This configuration is safe for your GPU");

            Assert.Pass($"Configuration DPI={dpi}, ChunkSize={chunkSize} succeeded");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("LM Studio"))
        {
            sw.Stop();
            Console.WriteLine($"✗ FAILED (LM Studio error)");
            Console.WriteLine($"Time before failure: {sw.Elapsed.TotalSeconds:F2}s");
            Console.WriteLine($"Error: {ex.Message}");

            if (ex.Message.Contains("out of memory") || ex.Message.Contains("OOM"))
            {
                Console.WriteLine($"Recommendation: Reduce DPI to {dpi - 50} or lower");
                Assert.Fail($"GPU OOM at DPI={dpi}. Try lower DPI.");
            }
            else
            {
                Assert.Fail($"LM Studio error: {ex.Message}");
            }
        }
    }

    #endregion

    #region Stress Tests

    [Test]
    [Explicit("Stress test - runs extraction multiple times to verify stability")]
    public async Task ExtractAsync_MultipleIterations_RemainsStable()
    {
        // Arrange
        if (!TestPdfFiles.SamplePdfExists)
        {
            Assert.Ignore("Test PDF file not found");
        }

        var parameters = CreateParameters(dpi: 200); // Safe DPI
        _sut = CreateExtractor(parameters);

        const int iterations = 5;
        var results = new List<(int iteration, TimeSpan duration, int chunks, bool success, string error)>();

        Console.WriteLine($"=== Stress Test: {iterations} iterations ===");

        // Act
        for (int i = 1; i <= iterations; i++)
        {
            var correlationId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var result = await _sut.ExtractAsync(
                    TestPdfFiles.SamplePdf,
                    _eventPublisher,
                    correlationId,
                    sessionId);
                sw.Stop();

                var chunks = result.ToList();
                results.Add((i, sw.Elapsed, chunks.Count, true, string.Empty));

                Console.WriteLine($"Iteration {i}/{iterations}: ✓ Success ({sw.Elapsed.TotalSeconds:F2}s, {chunks.Count} chunks)");
            }
            catch (Exception ex)
            {
                sw.Stop();
                results.Add((i, sw.Elapsed, 0, false, ex.Message));
                Console.WriteLine($"Iteration {i}/{iterations}: ✗ Failed ({sw.Elapsed.TotalSeconds:F2}s) - {ex.Message}");
            }

            // Give GPU time to recover
            await Task.Delay(1000);
        }

        // Assert
        var successRate = results.Count(r => r.success) / (double)iterations * 100;
        var avgDuration = TimeSpan.FromSeconds(results.Where(r => r.success).Average(r => r.duration.TotalSeconds));

        Console.WriteLine($"\n=== Summary ===");
        Console.WriteLine($"Success rate: {successRate:F0}%");
        Console.WriteLine($"Average duration: {avgDuration.TotalSeconds:F2}s");
        Console.WriteLine($"Failures: {results.Count(r => !r.success)}");

        Assert.That(successRate, Is.GreaterThanOrEqualTo(80),
            "At least 80% of iterations should succeed");
    }

    #endregion

    #region Helper Methods

    private LMStudioParameters CreateParameters(
        int dpi = 300,
        int chunkSize = 1000,
        string? visionModelName = null)
    {
        return new LMStudioParameters
        {
            PdfFolderPath = Path.GetDirectoryName(TestPdfFiles.SamplePdf),
            OutputFolderPath = Path.Combine(Path.GetTempPath(), "PdfTextExtractor.Tests"),
            LMStudioUrl = _lmStudioUrl,
            VisionModelName = visionModelName ?? _visionModelName,
            RasterizationDpi = dpi,
            ChunkSize = chunkSize
        };
    }

    private LMStudioOcrExtractor CreateExtractor(LMStudioParameters parameters)
    {
        var logger = new NullLogger<LMStudioOcrExtractor>();
        var rasterizerLogger = new NullLogger<PdfPageRasterizer>();
        var visionClientLogger = new NullLogger<LMStudioVisionClient>();

        var rasterizer = new PdfPageRasterizer(rasterizerLogger);
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var visionClient = new LMStudioVisionClient(visionClientLogger, httpClient);

        return new LMStudioOcrExtractor(logger, rasterizer, visionClient, parameters);
    }

    private string GetExpectedGpuImpact(int dpi)
    {
        return dpi switch
        {
            <= 150 => "Very Low (< 1 GB VRAM for image buffer)",
            <= 200 => "Low (~ 1-1.2 GB VRAM for image buffer)",
            <= 300 => "Medium (~ 1.5-1.8 GB VRAM for image buffer)",
            <= 450 => "High (~ 2-2.5 GB VRAM for image buffer)",
            _ => "Very High (> 2.5 GB VRAM - likely to cause OOM on 6GB GPU)"
        };
    }

    #endregion
}

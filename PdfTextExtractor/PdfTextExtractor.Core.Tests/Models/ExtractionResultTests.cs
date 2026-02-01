using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoMoq;
using NUnit.Framework;
using PdfTextExtractor.Core.Configuration;
using PdfTextExtractor.Core.Models;

namespace PdfTextExtractor.Core.Tests.Models;

/// <summary>
/// Tests for the ExtractionResult model.
/// </summary>
[TestFixture]
public class ExtractionResultTests
{
    private IFixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
    }

    [Test]
    public void PageTextFiles_WithValidDictionary_CreatesSuccessfully()
    {
        // Arrange
        var pageFiles = new Dictionary<int, string>
        {
            { 1, @"C:\output\invoice_page_1.txt" },
            { 2, @"C:\output\invoice_page_2.txt" },
            { 3, @"C:\output\invoice_page_3.txt" }
        };

        // Act
        var result = new ExtractionResult
        {
            PdfFilePath = @"C:\input\invoice.pdf",
            PageTextFiles = pageFiles,
            TotalPages = 3,
            SkippedPages = 0,
            ExtractedPages = 3,
            Duration = TimeSpan.FromSeconds(10),
            Method = TextExtractionMethod.PdfPig,
            TotalPromptTokens = 0,
            TotalCompletionTokens = 0,
            TotalTokens = 0
        };

        // Assert
        Assert.That(result.PageTextFiles, Is.Not.Null);
        Assert.That(result.PageTextFiles.Count, Is.EqualTo(3));
        Assert.That(result.PageTextFiles[1], Is.EqualTo(@"C:\output\invoice_page_1.txt"));
        Assert.That(result.PageTextFiles[2], Is.EqualTo(@"C:\output\invoice_page_2.txt"));
        Assert.That(result.PageTextFiles[3], Is.EqualTo(@"C:\output\invoice_page_3.txt"));
    }

    [Test]
    public void PageTextFiles_IsReadOnlyDictionary_CannotBeModified()
    {
        // Arrange
        var pageFiles = new Dictionary<int, string>
        {
            { 1, @"C:\output\test_page_1.txt" }
        };

        var result = new ExtractionResult
        {
            PdfFilePath = @"C:\input\test.pdf",
            PageTextFiles = pageFiles,
            TotalPages = 1,
            SkippedPages = 0,
            ExtractedPages = 1,
            Duration = TimeSpan.FromSeconds(5),
            Method = TextExtractionMethod.OpenAI,
            TotalPromptTokens = 100,
            TotalCompletionTokens = 50,
            TotalTokens = 150
        };

        // Assert - Verify it's an IReadOnlyDictionary
        Assert.That(result.PageTextFiles, Is.InstanceOf<IReadOnlyDictionary<int, string>>());
    }

    [Test]
    public void PageTextFiles_WithMultiplePages_MaintainsCorrectOrder()
    {
        // Arrange
        var pageFiles = new Dictionary<int, string>
        {
            { 5, @"C:\output\doc_page_5.txt" },
            { 1, @"C:\output\doc_page_1.txt" },
            { 3, @"C:\output\doc_page_3.txt" },
            { 2, @"C:\output\doc_page_2.txt" },
            { 4, @"C:\output\doc_page_4.txt" }
        };

        // Act
        var result = new ExtractionResult
        {
            PdfFilePath = @"C:\input\doc.pdf",
            PageTextFiles = pageFiles,
            TotalPages = 5,
            SkippedPages = 0,
            ExtractedPages = 5,
            Duration = TimeSpan.FromSeconds(20),
            Method = TextExtractionMethod.LMStudio,
            TotalPromptTokens = 500,
            TotalCompletionTokens = 250,
            TotalTokens = 750
        };

        // Assert - Verify all pages are accessible by key
        Assert.That(result.PageTextFiles.ContainsKey(1), Is.True);
        Assert.That(result.PageTextFiles.ContainsKey(2), Is.True);
        Assert.That(result.PageTextFiles.ContainsKey(3), Is.True);
        Assert.That(result.PageTextFiles.ContainsKey(4), Is.True);
        Assert.That(result.PageTextFiles.ContainsKey(5), Is.True);
    }

    [Test]
    public void PageTextFiles_WithSinglePage_WorksCorrectly()
    {
        // Arrange
        var pageFiles = new Dictionary<int, string>
        {
            { 1, @"C:\output\single_page_1.txt" }
        };

        // Act
        var result = new ExtractionResult
        {
            PdfFilePath = @"C:\input\single.pdf",
            PageTextFiles = pageFiles,
            TotalPages = 1,
            SkippedPages = 0,
            ExtractedPages = 1,
            Duration = TimeSpan.FromSeconds(2),
            Method = TextExtractionMethod.PdfPig,
            TotalPromptTokens = 0,
            TotalCompletionTokens = 0,
            TotalTokens = 0
        };

        // Assert
        Assert.That(result.PageTextFiles.Count, Is.EqualTo(1));
        Assert.That(result.PageTextFiles[1], Is.EqualTo(@"C:\output\single_page_1.txt"));
    }

    [Test]
    public void PageTextFiles_WithEmptyDictionary_WorksCorrectly()
    {
        // Arrange
        var pageFiles = new Dictionary<int, string>();

        // Act
        var result = new ExtractionResult
        {
            PdfFilePath = @"C:\input\empty.pdf",
            PageTextFiles = pageFiles,
            TotalPages = 0,
            SkippedPages = 0,
            ExtractedPages = 0,
            Duration = TimeSpan.FromSeconds(0),
            Method = TextExtractionMethod.PdfPig,
            TotalPromptTokens = 0,
            TotalCompletionTokens = 0,
            TotalTokens = 0
        };

        // Assert
        Assert.That(result.PageTextFiles, Is.Not.Null);
        Assert.That(result.PageTextFiles.Count, Is.EqualTo(0));
    }

    [Test]
    public void ExtractionResult_WithAllPropertiesSet_CreatesSuccessfully()
    {
        // Arrange
        var pageFiles = new Dictionary<int, string>
        {
            { 1, @"C:\output\full_page_1.txt" },
            { 2, @"C:\output\full_page_2.txt" }
        };

        var duration = TimeSpan.FromSeconds(15);

        // Act
        var result = new ExtractionResult
        {
            PdfFilePath = @"C:\input\full.pdf",
            PageTextFiles = pageFiles,
            TotalPages = 2,
            SkippedPages = 1,
            ExtractedPages = 1,
            Duration = duration,
            Method = TextExtractionMethod.OpenAI,
            TotalPromptTokens = 1000,
            TotalCompletionTokens = 500,
            TotalTokens = 1500
        };

        // Assert - Verify all properties
        Assert.That(result.PdfFilePath, Is.EqualTo(@"C:\input\full.pdf"));
        Assert.That(result.PageTextFiles.Count, Is.EqualTo(2));
        Assert.That(result.TotalPages, Is.EqualTo(2));
        Assert.That(result.SkippedPages, Is.EqualTo(1));
        Assert.That(result.ExtractedPages, Is.EqualTo(1));
        Assert.That(result.Duration, Is.EqualTo(duration));
        Assert.That(result.Method, Is.EqualTo(TextExtractionMethod.OpenAI));
        Assert.That(result.TotalPromptTokens, Is.EqualTo(1000));
        Assert.That(result.TotalCompletionTokens, Is.EqualTo(500));
        Assert.That(result.TotalTokens, Is.EqualTo(1500));
    }

    [Test]
    public void PageTextFiles_FileNamingPattern_FollowsConvention()
    {
        // Arrange
        var pageFiles = new Dictionary<int, string>
        {
            { 1, @"C:\output\document_page_1.txt" },
            { 2, @"C:\output\document_page_2.txt" },
            { 3, @"C:\output\document_page_3.txt" }
        };

        // Act
        var result = new ExtractionResult
        {
            PdfFilePath = @"C:\input\document.pdf",
            PageTextFiles = pageFiles,
            TotalPages = 3,
            SkippedPages = 0,
            ExtractedPages = 3,
            Duration = TimeSpan.FromSeconds(10),
            Method = TextExtractionMethod.PdfPig,
            TotalPromptTokens = 0,
            TotalCompletionTokens = 0,
            TotalTokens = 0
        };

        // Assert - Verify naming pattern {PdfName}_page_{PageNumber}.txt
        foreach (var (pageNumber, filePath) in result.PageTextFiles)
        {
            var expectedFileName = $"document_page_{pageNumber}.txt";
            Assert.That(filePath, Does.EndWith(expectedFileName));
        }
    }
}

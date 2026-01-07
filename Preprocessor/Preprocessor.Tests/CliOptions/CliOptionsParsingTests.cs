using CommandLine;
using Preprocessor.CliOptions;

namespace Preprocessor.Tests.CliOptions;

/// <summary>
/// Tests for CLI options parsing (verb-based commands).
/// </summary>
[TestFixture]
public class CliOptionsParsingTests
{
    #region JSON Verb Tests

    [Test]
    public void JsonVerb_WithDefaultArguments_ParsesSuccessfully()
    {
        // Arrange
        var args = new[] { "json" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);

        // Assert
        Assert.That(result.Tag, Is.EqualTo(ParserResultType.Parsed));

        var parsed = result as Parsed<object>;
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Value, Is.InstanceOf<JsonOptions>());

        var opts = (JsonOptions)parsed.Value;
        Assert.That(opts.Input, Is.EqualTo("pdfs"));
        Assert.That(opts.Output, Is.EqualTo("./embeddings.json"));
        Assert.That(opts.Append, Is.False);
        Assert.That(opts.Method, Is.EqualTo("pdfpig"));
        Assert.That(opts.Provider, Is.EqualTo(EmbeddingProvider.OpenAI));
        Assert.That(opts.EmbeddingModel, Is.EqualTo("text-embedding-3-small"));
    }

    [Test]
    public void JsonVerb_WithAllArguments_ParsesSuccessfully()
    {
        // Arrange
        var args = new[]
        {
            "json",
            "-i", "./my-pdfs",
            "-o", "./my-output.json",
            "-a",
            "-m", "pdfpig",
            "-p", "Ollama",
            "--embedding-model", "nomic-embed-text",
            "--ollama-url", "http://localhost:11434",
            "--openai-api-key", "sk-test123"
        };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);

        // Assert
        Assert.That(result.Tag, Is.EqualTo(ParserResultType.Parsed));

        var parsed = result as Parsed<object>;
        Assert.That(parsed, Is.Not.Null);

        var opts = (JsonOptions)parsed!.Value;
        Assert.That(opts.Input, Is.EqualTo("./my-pdfs"));
        Assert.That(opts.Output, Is.EqualTo("./my-output.json"));
        Assert.That(opts.Append, Is.True);
        Assert.That(opts.Method, Is.EqualTo("pdfpig"));
        Assert.That(opts.Provider, Is.EqualTo(EmbeddingProvider.Ollama));
        Assert.That(opts.EmbeddingModel, Is.EqualTo("nomic-embed-text"));
        Assert.That(opts.OllamaUrl, Is.EqualTo("http://localhost:11434"));
        Assert.That(opts.OpenAIApiKey, Is.EqualTo("sk-test123"));
    }

    [Test]
    public void JsonVerb_WithShortFlags_ParsesSuccessfully()
    {
        // Arrange
        var args = new[]
        {
            "json",
            "-i", "./pdfs",
            "-o", "./output.json",
            "-a",
            "-p", "LMStudio"
        };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);

        // Assert
        Assert.That(result.Tag, Is.EqualTo(ParserResultType.Parsed));

        var parsed = result as Parsed<object>;
        var opts = (JsonOptions)parsed!.Value;

        Assert.That(opts.Input, Is.EqualTo("./pdfs"));
        Assert.That(opts.Output, Is.EqualTo("./output.json"));
        Assert.That(opts.Append, Is.True);
        Assert.That(opts.Provider, Is.EqualTo(EmbeddingProvider.LMStudio));
    }

    [Test]
    public void JsonVerb_EffectiveUrl_ReturnsCorrectDefaultForOllama()
    {
        // Arrange
        var args = new[] { "json", "-p", "Ollama" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (JsonOptions)((Parsed<object>)result).Value;

        // Assert
        Assert.That(opts.EffectiveUrl, Is.EqualTo("http://localhost:11434"));
    }

    [Test]
    public void JsonVerb_EffectiveUrl_ReturnsCorrectDefaultForLMStudio()
    {
        // Arrange
        var args = new[] { "json", "-p", "LMStudio" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (JsonOptions)((Parsed<object>)result).Value;

        // Assert
        Assert.That(opts.EffectiveUrl, Is.EqualTo("http://localhost:1234"));
    }

    [Test]
    public void JsonVerb_EffectiveUrl_ReturnsCorrectDefaultForOpenAI()
    {
        // Arrange
        var args = new[] { "json", "-p", "OpenAI" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (JsonOptions)((Parsed<object>)result).Value;

        // Assert
        Assert.That(opts.EffectiveUrl, Is.EqualTo("https://api.openai.com/v1"));
    }

    [Test]
    public void JsonVerb_EffectiveUrl_OverridesDefaultWithOllamaUrl()
    {
        // Arrange
        var args = new[] { "json", "-p", "Ollama", "--ollama-url", "http://custom:8080" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (JsonOptions)((Parsed<object>)result).Value;

        // Assert
        Assert.That(opts.EffectiveUrl, Is.EqualTo("http://custom:8080"));
    }

    #endregion

    #region CosmosDB Verb Tests

    [Test]
    public void CosmosDbVerb_WithDefaultArguments_ParsesSuccessfully()
    {
        // Arrange
        var args = new[] { "cosmosdb" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);

        // Assert
        Assert.That(result.Tag, Is.EqualTo(ParserResultType.Parsed));

        var parsed = result as Parsed<object>;
        Assert.That(parsed, Is.Not.Null);
        Assert.That(parsed!.Value, Is.InstanceOf<CosmosDbOptions>());

        var opts = (CosmosDbOptions)parsed.Value;
        Assert.That(opts.Input, Is.EqualTo("pdfs"));
        Assert.That(opts.Url, Is.EqualTo("http://localhost:5000"));
        Assert.That(opts.Operation, Is.EqualTo("add"));
        Assert.That(opts.BatchSize, Is.EqualTo(100));
        Assert.That(opts.Provider, Is.EqualTo(EmbeddingProvider.OpenAI));
        Assert.That(opts.EmbeddingModel, Is.EqualTo("text-embedding-3-small"));
    }

    [Test]
    public void CosmosDbVerb_WithAllArguments_ParsesSuccessfully()
    {
        // Arrange
        var args = new[]
        {
            "cosmosdb",
            "-i", "./my-pdfs",
            "-u", "https://backend.azurewebsites.net",
            "-k", "test-api-key",
            "-o", "update",
            "-b", "50",
            "-p", "Ollama",
            "--embedding-model", "nomic-embed-text"
        };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);

        // Assert
        Assert.That(result.Tag, Is.EqualTo(ParserResultType.Parsed));

        var parsed = result as Parsed<object>;
        var opts = (CosmosDbOptions)parsed!.Value;

        Assert.That(opts.Input, Is.EqualTo("./my-pdfs"));
        Assert.That(opts.Url, Is.EqualTo("https://backend.azurewebsites.net"));
        Assert.That(opts.ApiKey, Is.EqualTo("test-api-key"));
        Assert.That(opts.Operation, Is.EqualTo("update"));
        Assert.That(opts.BatchSize, Is.EqualTo(50));
        Assert.That(opts.Provider, Is.EqualTo(EmbeddingProvider.Ollama));
        Assert.That(opts.EmbeddingModel, Is.EqualTo("nomic-embed-text"));
    }

    [Test]
    public void CosmosDbVerb_EffectiveApiKey_ReturnsCliArgument()
    {
        // Arrange
        var args = new[] { "cosmosdb", "-k", "cli-key" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (CosmosDbOptions)((Parsed<object>)result).Value;

        // Assert
        Assert.That(opts.EffectiveApiKey, Is.EqualTo("cli-key"));
    }

    [Test]
    public void CosmosDbVerb_EffectiveApiKey_FallsBackToEnvironmentVariable()
    {
        // Arrange
        var args = new[] { "cosmosdb" };
        Environment.SetEnvironmentVariable("FUNDDOCS_API_KEY", "env-key");

        try
        {
            // Act
            var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
            var opts = (CosmosDbOptions)((Parsed<object>)result).Value;

            // Assert
            Assert.That(opts.EffectiveApiKey, Is.EqualTo("env-key"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("FUNDDOCS_API_KEY", null);
        }
    }

    [Test]
    public void CosmosDbVerb_EffectiveApiKey_PrefersCliOverEnvironment()
    {
        // Arrange
        var args = new[] { "cosmosdb", "-k", "cli-key" };
        Environment.SetEnvironmentVariable("FUNDDOCS_API_KEY", "env-key");

        try
        {
            // Act
            var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
            var opts = (CosmosDbOptions)((Parsed<object>)result).Value;

            // Assert
            Assert.That(opts.EffectiveApiKey, Is.EqualTo("cli-key"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("FUNDDOCS_API_KEY", null);
        }
    }

    #endregion

    #region Error Cases

    [Test]
    public void NoVerb_ReturnsError()
    {
        // Arrange
        var args = new[] { "-i", "./pdfs" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);

        // Assert
        Assert.That(result.Tag, Is.EqualTo(ParserResultType.NotParsed));
    }

    [Test]
    public void UnknownVerb_ReturnsError()
    {
        // Arrange
        var args = new[] { "invalid-verb", "-i", "./pdfs" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);

        // Assert
        Assert.That(result.Tag, Is.EqualTo(ParserResultType.NotParsed));
    }

    [Test]
    public void InvalidOption_ReturnsError()
    {
        // Arrange
        var args = new[] { "json", "--invalid-option", "value" };

        // Act
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);

        // Assert
        Assert.That(result.Tag, Is.EqualTo(ParserResultType.NotParsed));
    }

    #endregion

    #region Validation Tests

    [Test]
    public void JsonOptions_Validate_InvalidMethod_ReturnsError()
    {
        // Arrange
        var args = new[] { "json", "-m", "invalid-method", "-i", "." }; // Use current directory as input
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (JsonOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Has.Count.EqualTo(1));
        Assert.That(errors[0], Does.Contain("Invalid method"));
    }

    [Test]
    public void JsonOptions_Validate_NonExistentInputDirectory_ReturnsError()
    {
        // Arrange
        var args = new[] { "json", "-i", "./non-existent-directory-xyz" };
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (JsonOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Has.Count.GreaterThan(0));
        Assert.That(errors.Any(e => e.Contains("Input directory does not exist")), Is.True);
    }

    [Test]
    public void JsonOptions_Validate_InvalidUrl_ReturnsError()
    {
        // Arrange
        var args = new[] { "json", "--ollama-url", "not-a-valid-url" };
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (JsonOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Has.Count.GreaterThan(0));
        Assert.That(errors.Any(e => e.Contains("Invalid endpoint URL format")), Is.True);
    }

    [Test]
    public void JsonOptions_Validate_ValidOptions_ReturnsNoErrors()
    {
        // Arrange
        var args = new[] { "json", "-i", "." }; // Current directory always exists
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (JsonOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    [Test]
    public void CosmosDbOptions_Validate_InvalidUrl_ReturnsError()
    {
        // Arrange
        var args = new[] { "cosmosdb", "-u", "not-a-valid-url", "-k", "key" };
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (CosmosDbOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Has.Count.GreaterThan(0));
        Assert.That(errors.Any(e => e.Contains("Invalid backend URL format")), Is.True);
    }

    [Test]
    public void CosmosDbOptions_Validate_MissingApiKey_ReturnsError()
    {
        // Arrange
        var args = new[] { "cosmosdb" };
        Environment.SetEnvironmentVariable("FUNDDOCS_API_KEY", null); // Ensure no env var

        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (CosmosDbOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Has.Count.GreaterThan(0));
        Assert.That(errors.Any(e => e.Contains("API key is required")), Is.True);
    }

    [Test]
    public void CosmosDbOptions_Validate_InvalidOperation_ReturnsError()
    {
        // Arrange
        var args = new[] { "cosmosdb", "-o", "invalid-operation", "-k", "key" };
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (CosmosDbOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Has.Count.GreaterThan(0));
        Assert.That(errors.Any(e => e.Contains("Invalid operation")), Is.True);
    }

    [Test]
    public void CosmosDbOptions_Validate_InvalidBatchSize_ReturnsError()
    {
        // Arrange
        var args = new[] { "cosmosdb", "-b", "0", "-k", "key" };
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (CosmosDbOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Has.Count.GreaterThan(0));
        Assert.That(errors.Any(e => e.Contains("Batch size must be between")), Is.True);
    }

    [Test]
    public void CosmosDbOptions_Validate_ValidOptions_ReturnsNoErrors()
    {
        // Arrange
        var args = new[] { "cosmosdb", "-i", ".", "-k", "test-key" };
        var result = Parser.Default.ParseArguments<JsonOptions, CosmosDbOptions>(args);
        var opts = (CosmosDbOptions)((Parsed<object>)result).Value;

        // Act
        var errors = opts.Validate().ToList();

        // Assert
        Assert.That(errors, Is.Empty);
    }

    #endregion
}

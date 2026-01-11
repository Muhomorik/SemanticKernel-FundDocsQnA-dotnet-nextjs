using Backend.API.Domain.Interfaces;
using Backend.API.Domain.ValueObjects;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace Backend.API.Infrastructure.LLM.SemanticKernel;

/// <summary>
/// Adapter that wraps Semantic Kernel's IEmbeddingGenerator to implement domain interface.
/// </summary>
public class SemanticKernelEmbeddingGenerator : Domain.Interfaces.IEmbeddingGenerator
{
    private readonly Microsoft.Extensions.AI.IEmbeddingGenerator<string, Embedding<float>> _skEmbeddingService;

    public SemanticKernelEmbeddingGenerator(
        Microsoft.Extensions.AI.IEmbeddingGenerator<string, Embedding<float>> skEmbeddingService)
    {
        _skEmbeddingService = skEmbeddingService;
    }

    public async Task<EmbeddingVector> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken)
    {
        var result = await _skEmbeddingService.GenerateAsync(
            text,
            cancellationToken: cancellationToken);

        return new EmbeddingVector(result.Vector.ToArray());
    }
}

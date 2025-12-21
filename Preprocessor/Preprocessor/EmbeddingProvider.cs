namespace Preprocessor;

/// <summary>
/// Supported embedding providers.
/// </summary>
/// <remarks>
/// <para><strong>Provider Differences:</strong></para>
/// <list type="table">
///   <listheader>
///     <term>Provider</term>
///     <description>Endpoint / Characteristics</description>
///   </listheader>
///   <item>
///     <term>Ollama</term>
///     <description>
///       Uses Ollama's native API endpoint <c>/api/embed</c>.
///       Default URL: http://localhost:11434
///       Requires: ollama pull nomic-embed-text
///     </description>
///   </item>
///   <item>
///     <term>LMStudio</term>
///     <description>
///       Uses OpenAI-compatible API endpoint <c>/v1/embeddings</c>.
///       Default URL: http://localhost:1234
///       Requires: Model loaded in LM Studio's Embedding section
///     </description>
///   </item>
/// </list>
/// <para><strong>Selecting a Provider:</strong></para>
/// <list type="bullet">
///   <item>
///     <description>
///       <strong>Ollama:</strong> Best for CLI workflows and automated scripts.
///       Use --provider ollama --ollama-url http://localhost:11434
///     </description>
///   </item>
///   <item>
///     <description>
///       <strong>LM Studio:</strong> Best for GUI-driven workflows with visual model management.
///       Use --provider lmstudio --ollama-url http://localhost:1234
///     </description>
///   </item>
/// </list>
/// </remarks>
public enum EmbeddingProvider
{
    /// <summary>
    /// Ollama embedding provider using native /api/embed endpoint.
    /// Default port: 11434
    /// </summary>
    Ollama,

    /// <summary>
    /// LM Studio embedding provider using OpenAI-compatible /v1/embeddings endpoint.
    /// Default port: 1234
    /// </summary>
    LMStudio
}

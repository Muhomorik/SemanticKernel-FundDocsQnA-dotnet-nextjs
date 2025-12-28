# Preprocessor

> Part of [PDF Q&A Application](../README.md). See [Configuration & Secrets Guide](../docs/SECRETS-MANAGEMENT.md) for complete project configuration.

A .NET Console application that extracts text from PDF documents and generates embeddings using Semantic Kernel with Ollama, LM Studio, or OpenAI.

**Note:** The Preprocessor has no secrets - all configuration is via CLI arguments (documented below).

## Purpose

Convert PDFs into searchable embeddings (run once, or when adding new PDFs).

## Quick Start (F5)

All parameters have defaults. Just press F5 to debug:

- Reads PDFs from `bin/Debug/net8.0/pdfs/`
- Writes output to `bin/Debug/net8.0/output.json`
- Uses `pdfpig` extraction method
- **Default provider: LM Studio** at `http://localhost:1234`
- Use `--provider ollama` to switch to Ollama

Create a `pdfs` folder in your build output directory and add PDF files there.

## Prerequisites

- .NET 8 SDK
- **Embedding Provider**: Choose one (LM Studio is default for local development)
  - LM Studio (default) - GUI-based, port 1234, free
  - Ollama - CLI-based, port 11434, free
  - OpenAI - Cloud-based, requires API key (~$0.02 per 1M tokens)

## Choosing a Provider

| Provider | Port/URL | Best For | Endpoint | Cost |
|----------|----------|----------|----------|------|
| **LM Studio** (default) | 1234 | GUI workflows, visual model management, beginners | `/v1/embeddings` (OpenAI-compatible) | Free (local) |
| **Ollama** | 11434 | CLI workflows, automation, servers | `/api/embed` (native) | Free (local) |
| **OpenAI** | Cloud API | Production deployment, backend compatibility | `/v1/embeddings` (cloud) | ~$0.02 per 1M tokens |

Use `--provider lmstudio` (default), `--provider ollama`, or `--provider openai` to select your provider.

⚠️ **CRITICAL:** OpenAI embeddings (1536 dimensions) are NOT compatible with Ollama/LM Studio embeddings (384 dimensions). When switching providers, you MUST regenerate ALL embeddings from scratch.

### Embedding Provider Setup (Windows)

#### Option 1: Ollama (CLI)

1. Download installer from [ollama.com/download/windows](https://ollama.com/download/windows)
2. Run the installer and follow prompts
3. Open PowerShell and pull models:

   ```powershell
   ollama pull nomic-embed-text   # embeddings (required)
   ```

#### Option 2: LM Studio (GUI)

1. Download from [lmstudio.ai](https://lmstudio.ai/)
2. Search and download models in the app:
   - `nomic-embed-text` for embeddings
3. Start the local server (Developer tab → Start Server)
4. Use with `--ollama-url http://localhost:1234`

#### Option 3: OpenAI (Cloud)

**⚠️ For Production Deployment Only** - Required for compatibility with backend semantic search.

1. **Get an API key** from [https://platform.openai.com/api-keys](https://platform.openai.com/api-keys)

2. **Set the API key** via environment variable (recommended):

   ```powershell
   # Windows (PowerShell)
   $env:OPENAI_API_KEY = "sk-..."

   # Linux/macOS (Bash)
   export OPENAI_API_KEY="sk-..."
   ```

3. **Run with OpenAI provider:**

   ```bash
   dotnet run -- --provider openai --embedding-model text-embedding-3-small -i ./pdfs -o ./output.json
   ```

**Important Notes:**
- **Cost:** ~$0.02 per 1M tokens (batch processing ~10MB of PDFs costs a few cents)
- **Model:** Must use `text-embedding-3-small` for backend compatibility
- **Dimensions:** Outputs 1536-dimensional embeddings (vs 384 for local models)
- **Regeneration Required:** Cannot mix OpenAI embeddings with Ollama/LM Studio embeddings
- **API Key Security:** Never commit API keys to version control. Use environment variables.

#### Alternative Models

| Purpose     | Option              | Alternatives                     |
|-------------|--------------------|----------------------------------|
| Embeddings  | `--embedding-model` | `mxbai-embed-large`, `all-minilm`|

#### Choosing a Nomic Embed Text Model

**For Ollama users:**

```bash
ollama pull nomic-embed-text  # Automatically gets the latest stable version (v1.5)
```

**For LM Studio users:**

When browsing models in LM Studio, you'll see multiple Nomic Embed Text versions. Here's how to choose:

| Model Name | Description | Recommendation |
|------------|-------------|----------------|
| **nomic-embed-text-v1.5-GGUF** | Latest stable release | ✅ **Recommended** - Best balance of quality and performance |
| Nomic-embed-text-v1.5-Embedding-GGUF | Same as v1.5-GGUF | Alternative naming, functionally identical |
| nomic-embed-text-v1-GGUF | Original v1 release | Use only if you need compatibility with older systems |
| nomic-embed-text-v2-moe-GGUF | Mixture of Experts (experimental) | Larger and slower, potentially better quality but not widely tested |
| Nomic-Embed-Text-V2-GGUF | Version 2 (non-MoE) | Not as mature as v1.5, avoid for now |

**Quick recommendation:** Download `nomic-embed-text-v1.5-GGUF` in LM Studio for the best experience.

**Using LM Studio:**

1. Search for and download the model in LM Studio's model browser
2. Load the model in the "Embedding" section
3. Start the local server (Developer tab → Start Server)
4. Use `--ollama-url http://localhost:1234` when running the preprocessor
5. The `--embedding-model` parameter can be any value (LM Studio uses the loaded model)

## CLI Parameters

| Parameter          | Short | Required | Default                   | Description                    |
|--------------------|-------|----------|---------------------------|--------------------------------|
| `--method`         | `-m`  | No       | `pdfpig`                  | Extraction method              |
| `--input`          | `-i`  | No       | `pdfs`                    | Folder with PDFs               |
| `--output`         | `-o`  | No       | `output.json`             | Output JSON path               |
| `--append`         | `-a`  | No       | `false`                   | Append to existing JSON        |
| `--provider`       | `-p`  | No       | `lmstudio`                | `ollama`, `lmstudio`, or `openai` |
| `--embedding-model`| -     | No       | `nomic-embed-text`        | Embedding model (use `text-embedding-3-small` for OpenAI) |
| `--ollama-url`     | -     | No       | Auto (provider-based)     | Provider endpoint override     |
| `--openai-api-key` | -     | No       | `null`                    | OpenAI API key (or set `OPENAI_API_KEY` env var) |

## Usage

```bash
# LM Studio (default provider - local, free)
dotnet run --project Preprocessor -- -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# Ollama (explicit provider selection - local, free)
dotnet run --project Preprocessor -- --provider ollama -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# OpenAI (cloud provider - for production deployment)
# Set API key first: $env:OPENAI_API_KEY = "sk-..."
dotnet run --project Preprocessor -- --provider openai --embedding-model text-embedding-3-small -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# OpenAI with inline API key (not recommended - use environment variable instead)
dotnet run --project Preprocessor -- --provider openai --openai-api-key "sk-..." --embedding-model text-embedding-3-small -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# Append to existing file
dotnet run --project Preprocessor -- -i ./new-pdfs -o ./Preprocessor/output/embeddings.json --append

# Override default provider URL
dotnet run --project Preprocessor -- --provider lmstudio --ollama-url http://localhost:8080 -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json
```

### Production Deployment Workflow

When deploying to Azure or other production environments:

1. **Regenerate embeddings with OpenAI** (required for backend compatibility):
   ```bash
   # Set API key
   $env:OPENAI_API_KEY = "sk-..."

   # Generate embeddings with OpenAI
   dotnet run --project Preprocessor -- --provider openai --embedding-model text-embedding-3-small -i ./Preprocessor/pdfs -o ./embeddings.json
   ```

2. **Copy to backend**:
   ```bash
   cp ./embeddings.json ./backend/Backend.API/Data/embeddings.json
   ```

3. **Verify dimensions** (should be 1536):
   ```powershell
   # Check first embedding array length in JSON
   (Get-Content ./embeddings.json | ConvertFrom-Json)[0].embedding.Count
   # Should output: 1536
   ```

4. **Deploy to Azure** with the updated embeddings file.

## Output Format

```json
[
  {
    "id": "document_page1_chunk0",
    "text": "Extracted text content...",
    "embedding": [0.123, -0.456, ...],
    "source": "document.pdf",
    "page": 1
  }
]
```

## Running Tests

```bash
dotnet test
```

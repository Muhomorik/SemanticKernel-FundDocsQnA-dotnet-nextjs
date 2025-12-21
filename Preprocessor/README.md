# Preprocessor

A .NET Console application that extracts text from PDF documents and generates embeddings using Semantic Kernel with Ollama or LM Studio.

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
- **Embedding Provider**: Choose one (LM Studio is default)
  - LM Studio (default) - GUI-based, port 1234
  - Ollama - CLI-based, port 11434

## Choosing a Provider

| Provider | Port | Best For | Endpoint |
|----------|------|----------|----------|
| **LM Studio** (default) | 1234 | GUI workflows, visual model management, beginners | `/v1/embeddings` (OpenAI-compatible) |
| **Ollama** | 11434 | CLI workflows, automation, servers | `/api/embed` (native) |

Use `--provider lmstudio` (default) or `--provider ollama` to select your provider.

### Embedding Provider Setup (Windows)

#### Option 1: Ollama (CLI)

1. Download installer from [ollama.com/download/windows](https://ollama.com/download/windows)
2. Run the installer and follow prompts
3. Open PowerShell and pull models:

   ```powershell
   ollama pull nomic-embed-text   # embeddings (required)
   ollama pull llava              # vision (optional)
   ```

#### Option 2: LM Studio (GUI)

1. Download from [lmstudio.ai](https://lmstudio.ai/)
2. Search and download models in the app:
   - `nomic-embed-text` for embeddings
   - `llava` for vision (optional)
3. Start the local server (Developer tab → Start Server)
4. Use with `--ollama-url http://localhost:1234`

#### Alternative Models

| Purpose     | Option              | Alternatives                     |
|-------------|--------------------|----------------------------------|
| Embeddings  | `--embedding-model` | `mxbai-embed-large`, `all-minilm`|
| Vision      | `--vision-model`    | `llava:13b`, `bakllava`          |

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
| `--method`         | `-m`  | No       | `pdfpig`                  | `pdfpig` or `ollama-vision`    |
| `--input`          | `-i`  | No       | `pdfs`                    | Folder with PDFs               |
| `--output`         | `-o`  | No       | `output.json`             | Output JSON path               |
| `--append`         | `-a`  | No       | `false`                   | Append to existing JSON        |
| `--provider`       | `-p`  | No       | `lmstudio`                | `ollama` or `lmstudio`         |
| `--vision-model`   | -     | No       | `llava`                   | Vision model (ollama-vision)   |
| `--embedding-model`| -     | No       | `nomic-embed-text`        | Embedding model                |
| `--ollama-url`     | -     | No       | Auto (provider-based)     | Provider endpoint override     |

## Usage

```bash
# LM Studio (default provider)
dotnet run --project Preprocessor -- -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# Ollama (explicit provider selection)
dotnet run --project Preprocessor -- --provider ollama -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# Extract using Ollama Vision
dotnet run --project Preprocessor -- -m ollama-vision --provider ollama -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# Append to existing file
dotnet run --project Preprocessor -- -i ./new-pdfs -o ./Preprocessor/output/embeddings.json --append

# Override default provider URL
dotnet run --project Preprocessor -- --provider lmstudio --ollama-url http://localhost:8080 -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json
```

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

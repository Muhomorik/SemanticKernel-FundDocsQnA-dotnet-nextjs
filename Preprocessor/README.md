# Preprocessor

A .NET Console application that extracts text from PDF documents and generates embeddings using Semantic Kernel with Ollama.

## Purpose

Convert PDFs into searchable embeddings (run once, or when adding new PDFs).

## Quick Start (F5)

All parameters have defaults. Just press F5 to debug:

- Reads PDFs from `bin/Debug/net8.0/pdfs/`
- Writes output to `bin/Debug/net8.0/output.json`
- Uses `pdfpig` extraction method
- Connects to Ollama at `http://localhost:11434`

Create a `pdfs` folder in your build output directory and add PDF files there.

## Prerequisites

- .NET 8 SDK
- Ollama running locally (default: <http://localhost:11434>)

### Ollama Setup (Windows)

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

## CLI Parameters

| Parameter          | Short | Required | Default                   | Description                    |
|--------------------|-------|----------|---------------------------|--------------------------------|
| `--method`         | `-m`  | No       | `pdfpig`                  | `pdfpig` or `ollama-vision`    |
| `--input`          | `-i`  | No       | `pdfs`                    | Folder with PDFs               |
| `--output`         | `-o`  | No       | `output.json`             | Output JSON path               |
| `--append`         | `-a`  | No       | `false`                   | Append to existing JSON        |
| `--vision-model`   | -     | No       | `llava`                   | Vision model (ollama-vision)   |
| `--embedding-model`| -     | No       | `nomic-embed-text`        | Embedding model                |
| `--ollama-url`     | -     | No       | `http://localhost:11434`  | Ollama endpoint                |

## Usage

```bash
# Extract using PdfPig
dotnet run --project Preprocessor -- -m pdfpig -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# Extract using Ollama Vision
dotnet run --project Preprocessor -- -m ollama-vision -i ./Preprocessor/pdfs -o ./Preprocessor/output/embeddings.json

# Append to existing file
dotnet run --project Preprocessor -- -m pdfpig -i ./new-pdfs -o ./Preprocessor/output/embeddings.json --append
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

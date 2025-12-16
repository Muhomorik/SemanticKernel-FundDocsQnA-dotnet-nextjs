# Preprocessor

A .NET Console application that extracts text from PDF documents and generates embeddings using Semantic Kernel with Ollama.

## Purpose

Convert PDFs into searchable embeddings (run once, or when adding new PDFs).

## Prerequisites

- .NET 8 SDK
- Ollama running locally (default: <http://localhost:11434>)

### Ollama Setup

```bash
ollama pull nomic-embed-text   # embeddings
ollama pull llava              # vision (optional)
```

## CLI Parameters

| Parameter | Short | Required | Default | Description |
|-----------|-------|----------|---------|-------------|
| `--method` | `-m` | Yes | - | `pdfpig` or `ollama-vision` |
| `--input` | `-i` | Yes | - | Folder with PDFs |
| `--output` | `-o` | Yes | - | Output JSON path |
| `--append` | `-a` | No | `false` | Append to existing JSON |
| `--vision-model` | - | No | `llava` | Vision model (ollama-vision only) |
| `--embedding-model` | - | No | `nomic-embed-text` | Embedding model |
| `--ollama-url` | - | No | `http://localhost:11434` | Ollama endpoint |

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

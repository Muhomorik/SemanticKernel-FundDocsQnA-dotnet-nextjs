# LM Studio OCR Extractor Integration Tests

Comprehensive integration tests for the LM Studio OCR text extraction pipeline.

## Prerequisites

### Required

1. **LM Studio** running locally (default: `http://localhost:1234`)
2. **Vision model loaded** in LM Studio:
   - Recommended: `qwen/qwen2.5-vl-7b` (7B model, requires ~5-6 GB VRAM)
   - Alternative: `llava-v1.6-mistral-7b`
3. **Sample PDF files** in the test directory
4. **.NET 9.0 SDK**

### Hardware Recommendations

- **GPU**: NVIDIA GPU with at least 6 GB VRAM (e.g., GTX 1060 6GB)
- **RAM**: 16 GB+ recommended
- **Storage**: 10 GB+ for model downloads

## Configuration

### Environment Variables (Optional)

Override default settings with environment variables:

```powershell
# LM Studio URL
$env:LMSTUDIO_URL = "http://localhost:1234"

# Vision model name
$env:LMSTUDIO_VISION_MODEL = "qwen/qwen2.5-vl-7b"
```

### LM Studio Setup

1. **Download and install** LM Studio from [https://lmstudio.ai/](https://lmstudio.ai/)
2. **Download a vision model**:
   - Open LM Studio → Search for "qwen2.5-vl" or "llava"
   - Download `lmstudio-community/Qwen2.5-VL-7B-Instruct-GGUF` (Q4_K_M variant recommended)
3. **Load the model**:
   - Go to "Chat" tab → Click model dropdown → Select the downloaded vision model
   - Ensure server is running (green "Running" indicator at top)

## Running the Tests

### Run All Integration Tests

```bash
cd PdfTextExtractor
dotnet test PdfTextExtractor.Core.Tests/PdfTextExtractor.Core.Tests.csproj --filter "Category=Integration&Category=LMStudio"
```

### Run Specific Test Categories

#### 1. DPI Configuration Tests (Recommended First)

Test different image resolutions to find optimal settings for your GPU:

```bash
dotnet test --filter "TestCategory=Integration&FullyQualifiedName~ExtractAsync_DifferentDpiSettings"
```

**What it tests:**
- ✅ DPI 150: Low memory usage (~1 GB VRAM for image buffer)
- ✅ DPI 200: Balanced quality and memory (~1.2 GB VRAM)
- ✅ DPI 300: Standard quality (~1.8 GB VRAM)

**Expected outcome:**
- Identifies maximum safe DPI for your GPU
- Provides extraction time and quality metrics
- **Action**: Note which DPI works without GPU OOM errors

#### 2. High DPI Stress Tests (Manual)

Test limits of your GPU (may cause OOM):

```bash
dotnet test --filter "FullyQualifiedName~ExtractAsync_HighDpiSettings_MayCauseGpuOom"
```

**What it tests:**
- ⚠️ DPI 450: High resolution (may OOM on 6 GB GPU)
- ⚠️ DPI 600: Very high resolution (likely to OOM)

**Expected outcome:**
- Determines upper limit of DPI before GPU OOM
- **If test fails**: Reduce DPI to last successful value from previous test

#### 3. Chunk Size Tests

Test different text chunking strategies:

```bash
dotnet test --filter "FullyQualifiedName~ExtractAsync_DifferentChunkSizes"
```

**What it tests:**
- Small chunks (500 chars): More granular, more chunks
- Medium chunks (1000 chars): Default, balanced
- Large chunks (2000 chars): Fewer chunks, faster processing

**Expected outcome:**
- Verifies chunking logic works correctly
- Helps choose optimal chunk size for your use case

#### 4. GPU Memory Optimization Tests

Test recommended safe configurations:

```bash
dotnet test --filter "FullyQualifiedName~ExtractAsync_OptimizedConfigurations_ForGpuMemory"
```

**What it tests:**
| DPI | Chunk Size | Expected GPU Impact |
|-----|------------|-------------------|
| 150 | 500 | Very Low (< 1 GB) |
| 200 | 1000 | Low (~ 1-1.2 GB) |
| 300 | 1000 | Medium (~ 1.5-1.8 GB) |

**Expected outcome:**
- ✅ All configurations should pass on 6 GB GPU
- Provides baseline safe configurations

#### 5. Multi-Page PDF Test

Test full document processing:

```bash
dotnet test --filter "FullyQualifiedName~ExtractAsync_MultiPagePdf_ProcessesAllPages"
```

**What it tests:**
- Processes all pages sequentially
- Verifies page numbering and chunk assignment
- Tests memory cleanup between pages

**Expected outcome:**
- Confirms all pages are processed in order
- No memory leaks between pages

#### 6. Event Publishing Test

Verify event system:

```bash
dotnet test --filter "FullyQualifiedName~ExtractAsync_PublishesExpectedEvents"
```

**What it tests:**
- All lifecycle events are published
- Event correlation and session IDs are correct

**Expected outcome:**
- Logs all event types published during extraction

#### 7. Stress Test (Manual)

Run multiple iterations to test stability:

```bash
dotnet test --filter "FullyQualifiedName~ExtractAsync_MultipleIterations_RemainsStable"
```

**What it tests:**
- 5 consecutive extractions with 1-second delay between
- Memory stability and cleanup
- Consistent performance

**Expected outcome:**
- ≥80% success rate
- No performance degradation over iterations

## Troubleshooting

### GPU Out of Memory (OOM) Error

**Error in logs:**
```
llama_kv_cache: CUDA0 buffer size = 144.00 MiB
ggml_gallocr_reserve_n_impl: failed to allocate CUDA0 buffer of size 1730277632
```

**Solutions (in order of effectiveness):**

1. **Reduce DPI** (Most effective):
   ```csharp
   RasterizationDpi = 200  // Down from 300
   ```
   - Each 50 DPI reduction saves ~300-500 MB VRAM

2. **Reduce model context length** in LM Studio:
   - Settings → Context Length → Set to 2048 (from default 4096)
   - Saves ~200 MB KV cache

3. **Reduce GPU layer offload** in LM Studio:
   - Model Settings → GPU Offload → Reduce from 19 to 12-14 layers
   - Frees ~800 MB-1 GB VRAM

4. **Use smaller vision model**:
   - Switch from 7B to 3B model (if available)
   - Reduces model VRAM by ~60%

### LM Studio Connection Failed

**Error:**
```
Failed to connect to LM Studio at http://localhost:1234
```

**Solutions:**
1. Verify LM Studio is running (check status indicator)
2. Verify vision model is loaded (not just downloaded)
3. Check firewall settings allow localhost:1234
4. Try alternative port if 1234 is occupied

### Slow Extraction Performance

**Symptoms:**
- Each page takes > 30 seconds
- Total extraction time > 2 minutes for 3-page PDF

**Solutions:**
1. **Increase GPU offload** (more layers on GPU = faster):
   - LM Studio → Model Settings → GPU Offload → Increase layers
2. **Check GPU utilization**:
   - Use `nvidia-smi` to monitor GPU usage
   - Should be 80-100% during OCR processing
3. **Reduce DPI** (faster processing, lower quality):
   - 150 DPI: ~50% faster than 300 DPI
   - 200 DPI: ~30% faster than 300 DPI

## Test Results Interpretation

### Sample Output

```
=== Testing DPI: 200 ===
LM Studio URL: http://localhost:1234
Vision Model: qwen/qwen2.5-vl-7b
Extraction completed in 12.45s
Total chunks: 15
Total events: 87
Pages processed: 3
```

### Key Metrics

| Metric | Good | Warning | Bad |
|--------|------|---------|-----|
| **Extraction time** (per page) | < 10s | 10-20s | > 20s |
| **GPU OOM errors** | 0 | 0 | > 0 |
| **Success rate** | 100% | 80-99% | < 80% |
| **Memory usage** | < 5 GB | 5-5.5 GB | > 5.5 GB |

### Optimal Configuration (GTX 1060 6GB)

Based on test results, recommended settings:

```csharp
var parameters = new LMStudioParameters
{
    RasterizationDpi = 200,          // Balance of quality and memory
    ChunkSize = 1000,                // Default, works well
    VisionModelName = "qwen/qwen2.5-vl-7b",
    LMStudioUrl = "http://localhost:1234"
};
```

**LM Studio Settings:**
- Context Length: 2048
- GPU Offload: 12-14 layers
- Temperature: 0.1 (extraction precision)

## Next Steps

1. **Run DPI tests first** to find your GPU's safe maximum
2. **Use recommended configuration** for production
3. **Run stress test** to verify stability over time
4. **Monitor GPU memory** during production use (`nvidia-smi -l 1`)

## Support

If tests consistently fail:
1. Check LM Studio developer logs for detailed errors
2. Verify GPU drivers are up-to-date
3. Try reducing DPI to 150 and context to 2048
4. Consider using smaller 3B vision model

## References

- [LM Studio Documentation](https://lmstudio.ai/docs)
- [Qwen2.5-VL Model Card](https://huggingface.co/Qwen/Qwen2.5-VL-7B-Instruct)
- [PDF to Image Configuration](https://github.com/sungaila/PDFtoImage)

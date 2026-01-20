using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;

namespace PdfTextExtractor.Wpf.ViewModels;

/// <summary>
/// ViewModel for LM Studio configuration settings.
/// </summary>
public sealed class LMStudioConfigViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private string _lmStudioUrl;
    private string _visionModelName;
    private int _dpi;
    private int _chunkSize;
    private int _maxTokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="LMStudioConfigViewModel"/> class.
    /// Runtime constructor for dependency injection.
    /// </summary>
    public LMStudioConfigViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();

        // Initialize with default values
        _lmStudioUrl = "http://localhost:1234";
        _visionModelName = "qwen/qwen2.5-vl-7b";
        _dpi = 150;
        _chunkSize = 1000;
        _maxTokens = 200;

        // Initialize commands
        SetDpiCommand = new DelegateCommand<string>(OnSetDpi);
        SetMaxTokensCommand = new DelegateCommand<string>(OnSetMaxTokens);
    }

    /// <summary>
    /// Gets or sets the LM Studio URL.
    /// </summary>
    public string LMStudioUrl
    {
        get => _lmStudioUrl;
        set => SetProperty(ref _lmStudioUrl, value, nameof(LMStudioUrl));
    }

    /// <summary>
    /// Gets or sets the vision model name.
    /// </summary>
    public string VisionModelName
    {
        get => _visionModelName;
        set => SetProperty(ref _visionModelName, value, nameof(VisionModelName));
    }

    /// <summary>
    /// Gets or sets the rasterization DPI.
    /// </summary>
    public int Dpi
    {
        get => _dpi;
        set => SetProperty(ref _dpi, value, nameof(Dpi));
    }

    /// <summary>
    /// Gets or sets the chunk size in characters.
    /// </summary>
    public int ChunkSize
    {
        get => _chunkSize;
        set => SetProperty(ref _chunkSize, value, nameof(ChunkSize));
    }

    /// <summary>
    /// Gets or sets the maximum tokens for vision model output.
    /// </summary>
    public int MaxTokens
    {
        get => _maxTokens;
        set => SetProperty(ref _maxTokens, value, nameof(MaxTokens));
    }

    /// <summary>
    /// Gets the command to set DPI from preset buttons.
    /// </summary>
    public ICommand SetDpiCommand { get; }

    /// <summary>
    /// Gets the command to set max tokens from preset buttons.
    /// </summary>
    public ICommand SetMaxTokensCommand { get; }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>True if configuration is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(VisionModelName);
    }

    private void OnSetDpi(string? dpiValue)
    {
        if (!string.IsNullOrWhiteSpace(dpiValue) && int.TryParse(dpiValue, out int dpi))
        {
            Dpi = dpi;
            _logger.Info($"LM Studio DPI preset selected: {dpi}");
        }
    }

    private void OnSetMaxTokens(string? tokenValue)
    {
        if (!string.IsNullOrWhiteSpace(tokenValue) && int.TryParse(tokenValue, out int tokens))
        {
            MaxTokens = tokens;
            _logger.Info($"LM Studio Max tokens set to: {tokens}");
        }
    }
}

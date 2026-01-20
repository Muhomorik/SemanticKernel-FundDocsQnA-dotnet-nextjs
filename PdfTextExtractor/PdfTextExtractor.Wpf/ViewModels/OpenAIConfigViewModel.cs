using System.Windows.Input;
using DevExpress.Mvvm;
using NLog;

namespace PdfTextExtractor.Wpf.ViewModels;

/// <summary>
/// ViewModel for OpenAI configuration settings.
/// </summary>
public sealed class OpenAIConfigViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private string _openAIApiKey;
    private string _openAIModelName;
    private int _openAIDpi;
    private int _chunkSize;
    private int _maxTokens;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIConfigViewModel"/> class.
    /// Runtime constructor for dependency injection.
    /// </summary>
    public OpenAIConfigViewModel()
    {
        _logger = LogManager.GetCurrentClassLogger();

        // Initialize with default values
        _openAIApiKey = "";
        _openAIModelName = "gpt-4o";
        _openAIDpi = 150;
        _chunkSize = 1000;
        _maxTokens = 2000;

        // Initialize commands
        SetOpenAIDpiCommand = new DelegateCommand<string>(OnSetOpenAIDpi);
        SetOpenAIMaxTokensCommand = new DelegateCommand<string>(OnSetOpenAIMaxTokens);
    }

    /// <summary>
    /// Gets or sets the OpenAI API key.
    /// </summary>
    public string OpenAIApiKey
    {
        get => _openAIApiKey;
        set => SetProperty(ref _openAIApiKey, value, nameof(OpenAIApiKey));
    }

    /// <summary>
    /// Gets or sets the OpenAI vision model name.
    /// </summary>
    public string OpenAIModelName
    {
        get => _openAIModelName;
        set => SetProperty(ref _openAIModelName, value, nameof(OpenAIModelName));
    }

    /// <summary>
    /// Gets or sets the rasterization DPI for OpenAI.
    /// </summary>
    public int OpenAIDpi
    {
        get => _openAIDpi;
        set => SetProperty(ref _openAIDpi, value, nameof(OpenAIDpi));
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
    public ICommand SetOpenAIDpiCommand { get; }

    /// <summary>
    /// Gets the command to set max tokens from preset buttons.
    /// </summary>
    public ICommand SetOpenAIMaxTokensCommand { get; }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>True if configuration is valid; otherwise, false.</returns>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(OpenAIApiKey) &&
               !string.IsNullOrWhiteSpace(OpenAIModelName);
    }

    private void OnSetOpenAIDpi(string? dpiValue)
    {
        if (!string.IsNullOrWhiteSpace(dpiValue) && int.TryParse(dpiValue, out int dpi))
        {
            OpenAIDpi = dpi;
            _logger.Info($"OpenAI DPI preset selected: {dpi}");
        }
    }

    private void OnSetOpenAIMaxTokens(string? tokenValue)
    {
        if (!string.IsNullOrWhiteSpace(tokenValue) && int.TryParse(tokenValue, out int tokens))
        {
            MaxTokens = tokens;
            _logger.Info($"OpenAI Max tokens set to: {tokens}");
        }
    }
}

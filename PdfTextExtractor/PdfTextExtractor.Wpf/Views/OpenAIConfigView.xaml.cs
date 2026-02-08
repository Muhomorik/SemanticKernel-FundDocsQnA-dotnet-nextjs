using System.Windows;
using System.Windows.Controls;
using PdfTextExtractor.Wpf.ViewModels;

namespace PdfTextExtractor.Wpf.Views;

/// <summary>
/// Interaction logic for OpenAIConfigView.xaml
/// </summary>
public partial class OpenAIConfigView : System.Windows.Controls.UserControl
{
    public OpenAIConfigView()
    {
        InitializeComponent();
    }

    private void OnOpenAIApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is OpenAIConfigViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.OpenAIApiKey = passwordBox.Password;
        }
    }
}

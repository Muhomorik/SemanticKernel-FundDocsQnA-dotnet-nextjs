using System.Windows;
using System.Windows.Controls;
using PdfTextExtractor.Wpf.ViewModels;

namespace PdfTextExtractor.Wpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnOpenAIApiKeyChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && sender is PasswordBox passwordBox)
        {
            viewModel.OpenAIApiKey = passwordBox.Password;
        }
    }
}
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace PdfTextExtractor.Wpf.Converters;

/// <summary>
/// Converts status text to a <see cref="Brush"/> for dynamic status bar background color.
/// Red for errors/failures, yellow for cancellations, default gray for normal status.
/// </summary>
public class StatusBarBrushConverter : IValueConverter
{
    /// <summary>
    /// Converts a status string to a <see cref="Brush"/> based on keywords.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            var lowerStatus = status.ToLower();

            // Red for errors and failures
            if (lowerStatus.Contains("error") || lowerStatus.Contains("failed"))
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)); // Bootstrap danger red
            }

            // Yellow for cancellations and warnings
            if (lowerStatus.Contains("cancel"))
            {
                return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)); // Bootstrap warning yellow
            }
        }

        // Default dark gray for normal status
        return new SolidColorBrush(System.Windows.Media.Color.FromRgb(52, 58, 64));
    }

    /// <summary>
    /// Not implemented (one-way converter).
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("StatusBarBrushConverter is a one-way converter.");
    }
}

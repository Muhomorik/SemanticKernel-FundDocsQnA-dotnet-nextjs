using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using PdfTextExtractor.Wpf.ViewModels;

namespace PdfTextExtractor.Wpf.Converters;

/// <summary>
/// Converts <see cref="PageStatus"/> to a <see cref="Brush"/> for visual status indication.
/// </summary>
public class PageStatusToBrushConverter : IValueConverter
{
    /// <summary>
    /// Converts a <see cref="PageStatus"/> to a <see cref="Brush"/>.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PageStatus status)
        {
            return status switch
            {
                PageStatus.Completed => System.Windows.Media.Brushes.Green,
                PageStatus.OcrProcessing => System.Windows.Media.Brushes.Purple,
                PageStatus.Extracting => System.Windows.Media.Brushes.DodgerBlue,
                PageStatus.Rasterizing => System.Windows.Media.Brushes.Orange,
                PageStatus.Failed => System.Windows.Media.Brushes.Red,
                _ => System.Windows.Media.Brushes.Gray
            };
        }

        return System.Windows.Media.Brushes.Gray;
    }

    /// <summary>
    /// Not implemented (one-way converter).
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("PageStatusToBrushConverter is a one-way converter.");
    }
}

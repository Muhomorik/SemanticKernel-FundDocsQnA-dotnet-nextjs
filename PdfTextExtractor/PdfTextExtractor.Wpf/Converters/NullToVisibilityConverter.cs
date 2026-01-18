using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PdfTextExtractor.Wpf.Converters;

/// <summary>
/// Converts null values to <see cref="Visibility"/> for conditional UI display.
/// Supports inversion via ConverterParameter="Invert".
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a value to Visibility based on whether it's null.
    /// </summary>
    /// <param name="value">The value to check for null.</param>
    /// <param name="targetType">Target type (Visibility).</param>
    /// <param name="parameter">Pass "Invert" to show when null, hide when not null.</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>Visibility.Visible if not null, Visibility.Collapsed if null (or inverted).</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNull = value == null || (value is string str && string.IsNullOrWhiteSpace(str));
        bool invert = parameter?.ToString() == "Invert";

        return (isNull ^ invert) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// Not implemented (one-way converter).
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("NullToVisibilityConverter is a one-way converter.");
    }
}

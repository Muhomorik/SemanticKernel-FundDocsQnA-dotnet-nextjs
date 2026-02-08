using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PdfTextExtractor.Wpf.Converters;

/// <summary>
/// Converts boolean values to <see cref="Visibility"/> for conditional UI display.
/// Supports inversion via ConverterParameter="Invert".
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    /// <summary>
    /// Converts a boolean to Visibility.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <param name="targetType">Target type (Visibility).</param>
    /// <param name="parameter">Pass "Invert" to invert the logic.</param>
    /// <param name="culture">Culture info.</param>
    /// <returns>Visibility.Visible if true, Visibility.Collapsed if false (or inverted).</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            bool invert = parameter?.ToString() == "Invert";
            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        return Visibility.Collapsed;
    }

    /// <summary>
    /// Not implemented (one-way converter).
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException("BoolToVisibilityConverter is a one-way converter.");
    }
}

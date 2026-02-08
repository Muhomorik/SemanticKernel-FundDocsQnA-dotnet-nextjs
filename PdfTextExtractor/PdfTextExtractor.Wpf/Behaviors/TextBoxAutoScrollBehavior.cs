using System.Windows;
using System.Windows.Controls;

namespace PdfTextExtractor.Wpf.Behaviors;

/// <summary>
/// Attached behavior that automatically scrolls a TextBox to the end when its text changes.
/// This is useful for log-style outputs where the newest content should always be visible.
/// </summary>
public static class TextBoxAutoScrollBehavior
{
    /// <summary>
    /// Attached property to enable/disable auto-scroll behavior on a TextBox.
    /// </summary>
    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(TextBoxAutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    /// <summary>
    /// Gets the value of the AutoScrollToEnd attached property.
    /// </summary>
    public static bool GetAutoScrollToEnd(DependencyObject obj)
    {
        return (bool)obj.GetValue(AutoScrollToEndProperty);
    }

    /// <summary>
    /// Sets the value of the AutoScrollToEnd attached property.
    /// </summary>
    public static void SetAutoScrollToEnd(DependencyObject obj, bool value)
    {
        obj.SetValue(AutoScrollToEndProperty, value);
    }

    /// <summary>
    /// Handles changes to the AutoScrollToEnd property by attaching/detaching event handlers.
    /// </summary>
    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox)
            return;

        if ((bool)e.NewValue)
        {
            textBox.TextChanged += OnTextChanged;
        }
        else
        {
            textBox.TextChanged -= OnTextChanged;
        }
    }

    /// <summary>
    /// Event handler that scrolls the TextBox to the end when text changes.
    /// </summary>
    private static void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.ScrollToEnd();
        }
    }
}

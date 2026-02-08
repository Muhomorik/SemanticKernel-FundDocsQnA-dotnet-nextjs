using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YieldRaccoon.Wpf.Behaviors;

/// <summary>
/// Attached behavior that flashes an element's background when Status changes to "Completed".
/// Only triggers on actual change, not on initial load or virtualization recycling.
/// </summary>
public static class CompletionFlashBehavior
{
    public static readonly DependencyProperty StatusProperty =
        DependencyProperty.RegisterAttached(
            "Status",
            typeof(object),
            typeof(CompletionFlashBehavior),
            new PropertyMetadata(null, OnStatusChanged));

    public static readonly DependencyProperty FlashColorProperty =
        DependencyProperty.RegisterAttached(
            "FlashColor",
            typeof(Color),
            typeof(CompletionFlashBehavior),
            new PropertyMetadata(Colors.LightGreen));

    public static readonly DependencyProperty FlashDurationProperty =
        DependencyProperty.RegisterAttached(
            "FlashDuration",
            typeof(Duration),
            typeof(CompletionFlashBehavior),
            new PropertyMetadata(new Duration(TimeSpan.FromSeconds(1))));

    public static readonly DependencyProperty FlashHoldDurationProperty =
        DependencyProperty.RegisterAttached(
            "FlashHoldDuration",
            typeof(Duration),
            typeof(CompletionFlashBehavior),
            new PropertyMetadata(new Duration(TimeSpan.FromSeconds(0.5))));

    public static object? GetStatus(DependencyObject obj) => obj.GetValue(StatusProperty);
    public static void SetStatus(DependencyObject obj, object? value) => obj.SetValue(StatusProperty, value);

    public static Color GetFlashColor(DependencyObject obj) => (Color)obj.GetValue(FlashColorProperty);
    public static void SetFlashColor(DependencyObject obj, Color value) => obj.SetValue(FlashColorProperty, value);

    public static Duration GetFlashDuration(DependencyObject obj) => (Duration)obj.GetValue(FlashDurationProperty);
    public static void SetFlashDuration(DependencyObject obj, Duration value) => obj.SetValue(FlashDurationProperty, value);

    public static Duration GetFlashHoldDuration(DependencyObject obj) => (Duration)obj.GetValue(FlashHoldDurationProperty);
    public static void SetFlashHoldDuration(DependencyObject obj, Duration value) => obj.SetValue(FlashHoldDurationProperty, value);

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
            return;

        var oldValue = e.OldValue?.ToString();
        var newValue = e.NewValue?.ToString();

        // Only animate when status changes TO "Completed" from something else
        // Skip initial binding (oldValue is null) to avoid flashing on load
        if (oldValue != null && newValue == "Completed" && oldValue != "Completed")
        {
            PlayFlashAnimation(element);
        }
    }

    private static void PlayFlashAnimation(FrameworkElement element)
    {
        var flashColor = GetFlashColor(element);
        var fadeDuration = GetFlashDuration(element);
        var holdDuration = GetFlashHoldDuration(element);

        // Create a new brush for animation
        var brush = new SolidColorBrush(flashColor);
        brush.BeginAnimation(SolidColorBrush.ColorProperty, null); // Clear any existing animation

        // Set the background
        if (element is Panel panel)
        {
            panel.Background = brush;
        }
        else if (element is Border border)
        {
            border.Background = brush;
        }
        else if (element is Control control)
        {
            control.Background = brush;
        }
        else
        {
            return; // Element type not supported
        }

        // Use keyframe animation to hold the color before fading
        var animation = new ColorAnimationUsingKeyFrames();

        // Hold at flash color for the hold duration
        animation.KeyFrames.Add(new LinearColorKeyFrame(
            flashColor,
            KeyTime.FromTimeSpan(holdDuration.TimeSpan)));

        // Then fade to transparent over the fade duration
        animation.KeyFrames.Add(new EasingColorKeyFrame(
            Colors.Transparent,
            KeyTime.FromTimeSpan(holdDuration.TimeSpan + fadeDuration.TimeSpan),
            new QuadraticEase { EasingMode = EasingMode.EaseOut }));

        brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }
}

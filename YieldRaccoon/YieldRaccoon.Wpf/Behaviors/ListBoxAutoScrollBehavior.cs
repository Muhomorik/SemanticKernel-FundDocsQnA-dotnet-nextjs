using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace YieldRaccoon.Wpf.Behaviors;

/// <summary>
/// Attached behavior that automatically scrolls a ListBox to the end when new items are added,
/// with smooth animation support.
/// </summary>
public static class ListBoxAutoScrollBehavior
{
    #region AutoScrollToEnd Attached Property

    public static readonly DependencyProperty AutoScrollToEndProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToEnd",
            typeof(bool),
            typeof(ListBoxAutoScrollBehavior),
            new PropertyMetadata(false, OnAutoScrollToEndChanged));

    public static bool GetAutoScrollToEnd(DependencyObject obj) =>
        (bool)obj.GetValue(AutoScrollToEndProperty);

    public static void SetAutoScrollToEnd(DependencyObject obj, bool value) =>
        obj.SetValue(AutoScrollToEndProperty, value);

    #endregion

    #region AnimationDuration Attached Property

    public static readonly DependencyProperty AnimationDurationProperty =
        DependencyProperty.RegisterAttached(
            "AnimationDuration",
            typeof(Duration),
            typeof(ListBoxAutoScrollBehavior),
            new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(300))));

    public static Duration GetAnimationDuration(DependencyObject obj) =>
        (Duration)obj.GetValue(AnimationDurationProperty);

    public static void SetAnimationDuration(DependencyObject obj, Duration value) =>
        obj.SetValue(AnimationDurationProperty, value);

    #endregion

    private static void OnAutoScrollToEndChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListBox listBox)
            return;

        if ((bool)e.NewValue)
        {
            listBox.Loaded += ListBox_Loaded;
            listBox.Unloaded += ListBox_Unloaded;

            // If already loaded, attach immediately
            if (listBox.IsLoaded)
            {
                AttachCollectionChangedHandler(listBox);
            }
        }
        else
        {
            listBox.Loaded -= ListBox_Loaded;
            listBox.Unloaded -= ListBox_Unloaded;
            DetachCollectionChangedHandler(listBox);
        }
    }

    private static void ListBox_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            AttachCollectionChangedHandler(listBox);
        }
    }

    private static void ListBox_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ListBox listBox)
        {
            DetachCollectionChangedHandler(listBox);
        }
    }

    private static void AttachCollectionChangedHandler(ListBox listBox)
    {
        if (listBox.ItemsSource is INotifyCollectionChanged notifyCollection)
        {
            // Create debounce timer for this listbox
            var debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                listBox.UpdateLayout();
                AnimateScrollToEnd(listBox);
            };
            SetDebounceTimer(listBox, debounceTimer);

            // Store handler reference for cleanup
            NotifyCollectionChangedEventHandler handler = (s, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    // Debounce: restart timer on each add, animation triggers after items stop being added
                    var timer = GetDebounceTimer(listBox);
                    if (timer != null)
                    {
                        timer.Stop();
                        timer.Start();
                    }
                }
            };

            notifyCollection.CollectionChanged += handler;

            // Store handler for later removal
            SetCollectionChangedHandler(listBox, handler);
        }
    }

    private static void DetachCollectionChangedHandler(ListBox listBox)
    {
        // Stop and clean up debounce timer
        var timer = GetDebounceTimer(listBox);
        if (timer != null)
        {
            timer.Stop();
            SetDebounceTimer(listBox, null);
        }

        var handler = GetCollectionChangedHandler(listBox);
        if (handler != null && listBox.ItemsSource is INotifyCollectionChanged notifyCollection)
        {
            notifyCollection.CollectionChanged -= handler;
            SetCollectionChangedHandler(listBox, null);
        }
    }

    #region CollectionChangedHandler Storage Property

    private static readonly DependencyProperty CollectionChangedHandlerProperty =
        DependencyProperty.RegisterAttached(
            "CollectionChangedHandler",
            typeof(NotifyCollectionChangedEventHandler),
            typeof(ListBoxAutoScrollBehavior),
            new PropertyMetadata(null));

    private static NotifyCollectionChangedEventHandler? GetCollectionChangedHandler(DependencyObject obj) =>
        (NotifyCollectionChangedEventHandler?)obj.GetValue(CollectionChangedHandlerProperty);

    private static void SetCollectionChangedHandler(DependencyObject obj, NotifyCollectionChangedEventHandler? value) =>
        obj.SetValue(CollectionChangedHandlerProperty, value);

    #endregion

    #region ScrollAnimationHelper Storage Property

    // Keeps the animation helper alive during animation to prevent GC collection
    private static readonly DependencyProperty ScrollAnimationHelperProperty =
        DependencyProperty.RegisterAttached(
            "ScrollAnimationHelper",
            typeof(ScrollAnimationHelper),
            typeof(ListBoxAutoScrollBehavior),
            new PropertyMetadata(null));

    private static void SetScrollAnimationHelper(DependencyObject obj, ScrollAnimationHelper? value) =>
        obj.SetValue(ScrollAnimationHelperProperty, value);

    #endregion

    #region Debounce Timer Storage Property

    // Timer for debouncing rapid item additions
    private static readonly DependencyProperty DebounceTimerProperty =
        DependencyProperty.RegisterAttached(
            "DebounceTimer",
            typeof(DispatcherTimer),
            typeof(ListBoxAutoScrollBehavior),
            new PropertyMetadata(null));

    private static DispatcherTimer? GetDebounceTimer(DependencyObject obj) =>
        (DispatcherTimer?)obj.GetValue(DebounceTimerProperty);

    private static void SetDebounceTimer(DependencyObject obj, DispatcherTimer? value) =>
        obj.SetValue(DebounceTimerProperty, value);

    #endregion

    private static void AnimateScrollToEnd(ListBox listBox)
    {
        var scrollViewer = GetScrollViewer(listBox);
        if (scrollViewer == null)
            return;

        // Force layout update to get accurate scroll extent
        scrollViewer.UpdateLayout();

        var duration = GetAnimationDuration(listBox);
        var targetOffset = scrollViewer.ScrollableHeight;
        var currentOffset = scrollViewer.VerticalOffset;

        // Skip animation if already at the bottom or no scrolling needed
        if (Math.Abs(targetOffset - currentOffset) < 1)
            return;

        // Create and start the animated scroll
        // Store helper reference on the listbox to prevent GC during animation
        var helper = new ScrollAnimationHelper(scrollViewer);
        SetScrollAnimationHelper(listBox, helper);
        helper.AnimateVerticalOffset(currentOffset, targetOffset, duration);
    }

    private static ScrollViewer? GetScrollViewer(DependencyObject element)
    {
        if (element is ScrollViewer scrollViewer)
            return scrollViewer;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
        {
            var child = VisualTreeHelper.GetChild(element, i);
            var result = GetScrollViewer(child);
            if (result != null)
                return result;
        }

        return null;
    }
}

/// <summary>
/// Helper class to animate ScrollViewer.VerticalOffset, which is a read-only property.
/// Uses a dependency property that updates ScrollViewer position when animated.
/// Inherits from Animatable to support BeginAnimation.
/// </summary>
internal sealed class ScrollAnimationHelper : Animatable
{
    private readonly ScrollViewer _scrollViewer;

    public ScrollAnimationHelper(ScrollViewer scrollViewer)
    {
        _scrollViewer = scrollViewer;
    }

    protected override Freezable CreateInstanceCore() => new ScrollAnimationHelper(_scrollViewer);

    public static readonly DependencyProperty VerticalOffsetProperty =
        DependencyProperty.Register(
            nameof(VerticalOffset),
            typeof(double),
            typeof(ScrollAnimationHelper),
            new PropertyMetadata(0.0, OnVerticalOffsetChanged));

    public double VerticalOffset
    {
        get => (double)GetValue(VerticalOffsetProperty);
        set => SetValue(VerticalOffsetProperty, value);
    }

    private static void OnVerticalOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollAnimationHelper helper)
        {
            helper._scrollViewer.ScrollToVerticalOffset((double)e.NewValue);
        }
    }

    public void AnimateVerticalOffset(double from, double to, Duration duration)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = duration,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        BeginAnimation(VerticalOffsetProperty, animation);
    }
}

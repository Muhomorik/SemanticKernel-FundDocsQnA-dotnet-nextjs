using DevExpress.Mvvm;
using YieldRaccoon.Domain.Events.AboutFund;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel for displaying an about-fund event in the control panel.
/// </summary>
public class AboutFundEventViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the event timestamp.
    /// </summary>
    public DateTimeOffset Timestamp
    {
        get => GetProperty(() => Timestamp);
        set => SetProperty(() => Timestamp, value);
    }

    /// <summary>
    /// Gets or sets the event type display name.
    /// </summary>
    public string EventType
    {
        get => GetProperty(() => EventType);
        set => SetProperty(() => EventType, value);
    }

    /// <summary>
    /// Gets or sets the human-readable event description.
    /// </summary>
    public string Description
    {
        get => GetProperty(() => Description);
        set => SetProperty(() => Description, value);
    }

    /// <summary>
    /// Gets or sets the Segoe Fluent Icons code for this event.
    /// </summary>
    public string? Icon
    {
        get => GetProperty(() => Icon);
        set => SetProperty(() => Icon, value);
    }

    /// <summary>
    /// Gets or sets the icon color for this event.
    /// </summary>
    public string? IconColor
    {
        get => GetProperty(() => IconColor);
        set => SetProperty(() => IconColor, value);
    }

    /// <summary>
    /// Creates an <see cref="AboutFundEventViewModel"/> from an <see cref="IAboutFundEvent"/>.
    /// </summary>
    public static AboutFundEventViewModel FromEvent(IAboutFundEvent evt)
    {
        return evt switch
        {
            AboutFundSessionStarted e => new AboutFundEventViewModel
            {
                Timestamp = e.OccurredAt,
                EventType = "Session Started",
                Description = $"Started browsing {e.TotalFunds} funds",
                Icon = "\uE768",  // Play icon
                IconColor = "#2ECC71"
            },
            AboutFundNavigationStarted e => new AboutFundEventViewModel
            {
                Timestamp = e.OccurredAt,
                EventType = "Navigating",
                Description = $"[{e.Index + 1}] {e.Isin}",
                Icon = "\uE8AD",  // Globe icon
                IconColor = "#3498DB"
            },
            AboutFundNavigationCompleted e => new AboutFundEventViewModel
            {
                Timestamp = e.OccurredAt,
                EventType = "Loaded",
                Description = $"[{e.Index + 1}] {e.Isin}",
                Icon = "\uE73E",  // Checkmark icon
                IconColor = "#2ECC71"
            },
            AboutFundNavigationFailed e => new AboutFundEventViewModel
            {
                Timestamp = e.OccurredAt,
                EventType = "Failed",
                Description = $"{e.Isin}: {e.Reason}",
                Icon = "\uE783",  // Error icon
                IconColor = "#E74C3C"
            },
            AboutFundSessionCompleted e => new AboutFundEventViewModel
            {
                Timestamp = e.OccurredAt,
                EventType = "Session Done",
                Description = $"Visited {e.FundsVisited} funds in {e.Duration:mm\\:ss}",
                Icon = "\uE930",  // Completed icon
                IconColor = "#2ECC71"
            },
            AboutFundSessionCancelled e => new AboutFundEventViewModel
            {
                Timestamp = e.OccurredAt,
                EventType = "Cancelled",
                Description = $"Visited {e.FundsVisited} funds. {e.Reason}",
                Icon = "\uE711",  // Cancel icon
                IconColor = "#F39C12"
            },
            _ => new AboutFundEventViewModel
            {
                Timestamp = evt.OccurredAt,
                EventType = "Unknown",
                Description = evt.GetType().Name,
                Icon = "\uE946",  // Info icon
                IconColor = "#95A5A6"
            }
        };
    }
}

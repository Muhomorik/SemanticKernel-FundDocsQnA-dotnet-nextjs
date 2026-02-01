using DevExpress.Mvvm;
using YieldRaccoon.Wpf.Models;

namespace YieldRaccoon.Wpf.ViewModels;

/// <summary>
/// ViewModel wrapper for <see cref="InterceptedFund"/> that implements INotifyPropertyChanged
/// for proper WPF data binding and change notification.
/// </summary>
public class InterceptedFundViewModel : BindableBase
{
    /// <summary>
    /// Gets or sets the fund ISIN code.
    /// </summary>
    public string? Isin
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the fund name.
    /// </summary>
    public string? Name
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the net asset value (NAV).
    /// </summary>
    public decimal? Nav
    {
        get => GetValue<decimal?>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the currency code.
    /// </summary>
    public string? CurrencyCode
    {
        get => GetValue<string?>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the number of owners.
    /// </summary>
    public int? NumberOfOwners
    {
        get => GetValue<int?>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets the NAV date.
    /// </summary>
    public DateOnly? NavDate
    {
        get => GetValue<DateOnly?>();
        set => SetValue(value);
    }

    /// <summary>
    /// Gets or sets whether the NAV date is today.
    /// </summary>
    public bool IsNavDateToday
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// Updates this ViewModel from an <see cref="InterceptedFund"/> model.
    /// </summary>
    /// <param name="fund">The source fund to update from.</param>
    public void UpdateFrom(InterceptedFund fund)
    {
        Isin = fund.Isin;
        Name = fund.Name;
        Nav = fund.Nav;
        CurrencyCode = fund.CurrencyCode;
        NumberOfOwners = fund.NumberOfOwners;

        // Parse NavDate string to DateOnly and determine if it's today
        if (!string.IsNullOrEmpty(fund.NavDate) && DateOnly.TryParse(fund.NavDate, out var parsedDate))
        {
            NavDate = parsedDate;
            IsNavDateToday = parsedDate == DateOnly.FromDateTime(DateTime.Today);
        }
        else if (!string.IsNullOrEmpty(fund.NavDate) && DateTime.TryParse(fund.NavDate, out var parsedDateTime))
        {
            NavDate = DateOnly.FromDateTime(parsedDateTime);
            IsNavDateToday = NavDate == DateOnly.FromDateTime(DateTime.Today);
        }
        else
        {
            NavDate = null;
            IsNavDateToday = false;
        }
    }

    /// <summary>
    /// Creates a new <see cref="InterceptedFundViewModel"/> from an <see cref="InterceptedFund"/>.
    /// </summary>
    /// <param name="fund">The source fund.</param>
    /// <returns>A new ViewModel instance.</returns>
    public static InterceptedFundViewModel FromModel(InterceptedFund fund)
    {
        var vm = new InterceptedFundViewModel();
        vm.UpdateFrom(fund);
        return vm;
    }
}

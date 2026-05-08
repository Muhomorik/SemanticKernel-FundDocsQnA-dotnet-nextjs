using System.Globalization;
using System.IO;

namespace YieldRaccoon.Wpf.Configuration;

/// <summary>
/// Builds the family + ISO-week tags used in CSV export filenames. Filename grammar:
/// <c>YieldRaccoon_{kind}_{family}_{iso_week}.csv</c> — e.g., <c>YieldRaccoon_summary_all_2026-W18.csv</c>.
/// All three weekly artifacts (summary, snapshot, metadata) share the same family + ISO-week tags so
/// a "week bundle" matches by simple filename glob.
/// </summary>
internal static class IsoWeekFilenameBuilder
{
    /// <summary>
    /// Returns the family component of the filename: a sanitized, lower-cased company name when
    /// <paramref name="companyName"/> is provided; otherwise the literal <c>"all"</c>.
    /// </summary>
    public static string BuildFamilyTag(string? companyName)
    {
        if (string.IsNullOrWhiteSpace(companyName))
            return "all";

        var trimmed = companyName.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(trimmed.Select(c =>
            Array.IndexOf(invalid, c) >= 0 ? '_' : c).ToArray());
        return sanitized.Replace(' ', '_').ToLowerInvariant();
    }

    /// <summary>
    /// Returns the ISO 8601 week designation for <paramref name="when"/> in <c>YYYY-Www</c> format
    /// (e.g., <c>2026-W18</c>). The year component is the ISO week-year, which differs from the
    /// calendar year around new-year boundaries — e.g., 2027-01-01 may belong to ISO week 2026-W53.
    /// </summary>
    public static string BuildIsoWeekTag(DateTime when)
    {
        var year = ISOWeek.GetYear(when);
        var week = ISOWeek.GetWeekOfYear(when);
        return $"{year:D4}-W{week:D2}";
    }
}

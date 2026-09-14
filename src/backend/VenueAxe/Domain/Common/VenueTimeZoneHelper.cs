using System;

namespace VenueAxe.Domain.Common;

public static class VenueTimeZoneHelper
{
    public const string DefaultTimeZoneId = "America/New_York";

    public static TimeZoneInfo GetTimeZone(string? timezoneId)
    {
        if (string.IsNullOrWhiteSpace(timezoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId.Trim());
        }
        catch
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    /// <summary>
    /// Gets the UTC start and end bounds corresponding to [00:00:00.000, next day 00:00:00.000) for a given DateOnly in the venue's timezone.
    /// </summary>
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtc) GetUtcDayRange(DateOnly date, TimeZoneInfo tz)
    {
        var localStart = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var offset = tz.GetUtcOffset(localStart);
        var startUtc = new DateTimeOffset(localStart, offset).ToUniversalTime();
        var endUtc = startUtc.AddDays(1);
        return (startUtc, endUtc);
    }

    /// <summary>
    /// Constructs a DateTimeOffset from a venue-local DateOnly and time in hours and minutes.
    /// </summary>
    public static DateTimeOffset ToVenueDateTimeOffset(DateOnly date, int hour, int minute, TimeZoneInfo tz)
    {
        var localDt = new DateTime(date.Year, date.Month, date.Day, hour % 24, minute % 60, 0, DateTimeKind.Unspecified);
        var offset = tz.GetUtcOffset(localDt);
        return new DateTimeOffset(localDt, offset);
    }

    /// <summary>
    /// Converts a DateTimeOffset instant to the venue's local timezone offset.
    /// </summary>
    public static DateTimeOffset ConvertToVenueTime(DateTimeOffset dto, TimeZoneInfo tz)
    {
        return TimeZoneInfo.ConvertTime(dto, tz);
    }

    /// <summary>
    /// Extracts the local DateOnly for an instant in the venue's timezone.
    /// </summary>
    public static DateOnly GetVenueLocalDate(DateTimeOffset dto, TimeZoneInfo tz)
    {
        var local = TimeZoneInfo.ConvertTime(dto, tz);
        return DateOnly.FromDateTime(local.DateTime);
    }
}

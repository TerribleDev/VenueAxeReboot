using System;
using VenueAxe.Domain.Common;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class VenueTimeZoneAndScheduleMatrixTests
{
    [Fact]
    public void GetTimeZone_WithNullOrInvalid_ReturnsFallback()
    {
        var tzNull = VenueTimeZoneHelper.GetTimeZone(null);
        Assert.NotNull(tzNull);

        var tzEmpty = VenueTimeZoneHelper.GetTimeZone("   ");
        Assert.NotNull(tzEmpty);

        var tzInvalid = VenueTimeZoneHelper.GetTimeZone("Invalid/TimeZone_DoesNotExist");
        Assert.NotNull(tzInvalid);
    }

    [Fact]
    public void GetUtcDayRange_ForCentralTime_IncludesLateNightBookings()
    {
        var tz = VenueTimeZoneHelper.GetTimeZone("America/Chicago");
        var date = new DateOnly(2026, 9, 11);

        var (startUtc, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(date, tz);

        // 10:00 PM Central time on Sept 11 = Sept 12 at 03:00:00 UTC (during CDT -05:00)
        var tenPmUtc = new DateTimeOffset(2026, 9, 12, 3, 0, 0, TimeSpan.Zero);

        Assert.True(tenPmUtc >= startUtc, "10 PM Central booking should be >= start of day UTC");
        Assert.True(tenPmUtc < endUtc, "10 PM Central booking should be < end of day UTC");
    }

    [Fact]
    public void GetUtcDayRange_ExcludesPreviousDayLateBookings()
    {
        var tz = VenueTimeZoneHelper.GetTimeZone("America/Chicago");
        var date = new DateOnly(2026, 9, 11);

        var (startUtc, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(date, tz);

        // 10:00 PM Central time on Sept 10 = Sept 11 at 03:00:00 UTC
        var previousDayTenPmUtc = new DateTimeOffset(2026, 9, 11, 3, 0, 0, TimeSpan.Zero);

        Assert.False(previousDayTenPmUtc >= startUtc, "Sept 10 10 PM Central booking should NOT be >= Sept 11 start of day UTC");
    }

    [Fact]
    public void GetUtcDayRange_ForEasternTime_IncludesLateNightBookings()
    {
        var tz = VenueTimeZoneHelper.GetTimeZone("America/New_York");
        var date = new DateOnly(2026, 9, 11);

        var (startUtc, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(date, tz);

        // 10:00 PM Eastern time on Sept 11 = Sept 12 at 02:00:00 UTC (during EDT -04:00)
        var tenPmUtc = new DateTimeOffset(2026, 9, 12, 2, 0, 0, TimeSpan.Zero);

        Assert.True(tenPmUtc >= startUtc, "10 PM Eastern booking should be >= start of day UTC");
        Assert.True(tenPmUtc < endUtc, "10 PM Eastern booking should be < end of day UTC");
    }

    [Fact]
    public void GetVenueLocalDate_CorrectlyResolvesDate_WhenUtcIsNextDay()
    {
        var tz = VenueTimeZoneHelper.GetTimeZone("America/Chicago");
        // Sept 12 at 03:00 UTC is Sept 11 at 22:00 (10 PM) in America/Chicago
        var lateNightBookingUtc = new DateTimeOffset(2026, 9, 12, 3, 0, 0, TimeSpan.Zero);

        var localDate = VenueTimeZoneHelper.GetVenueLocalDate(lateNightBookingUtc, tz);

        Assert.Equal(new DateOnly(2026, 9, 11), localDate);
    }

    [Fact]
    public void ToVenueDateTimeOffset_CreatesAccurateLocalOffset()
    {
        var tz = VenueTimeZoneHelper.GetTimeZone("America/Chicago");
        var date = new DateOnly(2026, 9, 11);

        var slot = VenueTimeZoneHelper.ToVenueDateTimeOffset(date, 22, 0, tz);

        Assert.Equal(22, slot.Hour);
        Assert.Equal(0, slot.Minute);
        Assert.Equal(date.Year, slot.Year);
        Assert.Equal(date.Month, slot.Month);
        Assert.Equal(date.Day, slot.Day);
        // In CDT, offset is -5 hours
        Assert.Equal(TimeSpan.FromHours(-5), slot.Offset);
        // And when converted to UTC:
        Assert.Equal(new DateTimeOffset(2026, 9, 12, 3, 0, 0, TimeSpan.Zero), slot.ToUniversalTime());
    }
}

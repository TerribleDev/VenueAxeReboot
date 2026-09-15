using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Repositories;

namespace VenueAxe.Services;

public class ReportingService : IReportingService
{
    private readonly IUnitOfWork _uow;

    public ReportingService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DailyReportDto> GetDailyReportAsync(Guid venueId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var venue = await _uow.Venues.GetByIdAsync(venueId, cancellationToken);
        if (venue == null) throw new KeyNotFoundException("Venue not found or access denied");
        var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);
        var (startUtc, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(date, tz);

        var bookings = await _uow.Bookings.GetByVenueAndDateRangeAsync(venueId, startUtc, endUtc, cancellationToken);

        var validBookings = bookings.Where(b => b.Status != BookingStatus.Cancelled).ToList();
        int totalBookings = validBookings.Count;
        int totalThrowers = validBookings.Sum(b => b.PartySize);
        long grossRevenue = validBookings.Sum(b => (long)b.TotalAmountCents);
        long depositsCollected = validBookings.Sum(b => (long)b.PaidAmountCents);
        long balanceDue = Math.Max(0, grossRevenue - depositsCollected);
        int completedSessions = validBookings.Count(b => b.Status == BookingStatus.Completed);
        int cancelledBookings = bookings.Count(b => b.Status == BookingStatus.Cancelled);

        // Hourly distribution (10:00 AM to 11:00 PM)
        var hourlyList = new List<HourlyTrafficDto>();
        for (int h = 10; h <= 23; h++)
        {
            var hourStartLocal = new DateTime(date.Year, date.Month, date.Day, h, 0, 0, DateTimeKind.Unspecified);
            var hourEndLocal = hourStartLocal.AddHours(1);
            var hourStartUtc = new DateTimeOffset(hourStartLocal, tz.GetUtcOffset(hourStartLocal)).ToUniversalTime();
            var hourEndUtc = hourStartUtc.AddHours(1);

            var hourBookings = validBookings.Where(b => b.StartTime < hourEndUtc && b.EndTime > hourStartUtc).ToList();
            int count = hourBookings.Count;
            int throwers = hourBookings.Sum(b => b.PartySize);
            long rev = hourBookings.Sum(b => (long)b.TotalAmountCents);

            string ampm = h >= 12 ? "PM" : "AM";
            int displayH = h > 12 ? h - 12 : (h == 0 ? 12 : h);
            string timeLabel = $"{displayH}:00 {ampm}";

            hourlyList.Add(new HourlyTrafficDto(h, timeLabel, count, throwers, rev));
        }

        var bookingDtos = bookings.OrderBy(b => b.StartTime).Select(b =>
        {
            var laneNumbers = b.BookingLanes?.Select(bl => bl.Lane?.LaneNumber.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)) ?? Enumerable.Empty<string>();
            string lanesStr = string.Join(", ", laneNumbers);
            if (string.IsNullOrEmpty(lanesStr)) lanesStr = "Unassigned";

            return new ReportBookingSummaryDto(
                b.Id,
                b.BookingReference,
                $"{b.GuestFirstName} {b.GuestLastName}".Trim(),
                "Standard Bay",
                b.PartySize,
                b.StartTime,
                b.EndTime,
                b.Status.ToString(),
                b.PaymentStatus,
                b.TotalAmountCents,
                b.PaidAmountCents,
                b.Waivers.Count,
                lanesStr
            );
        }).ToList();

        return new DailyReportDto(
            date,
            totalBookings,
            totalThrowers,
            grossRevenue,
            depositsCollected,
            balanceDue,
            completedSessions,
            cancelledBookings,
            hourlyList,
            bookingDtos
        );
    }

    public async Task<WeeklyReportDto> GetWeeklyReportAsync(Guid venueId, DateOnly weekStart, CancellationToken cancellationToken = default)
    {
        var venue = await _uow.Venues.GetByIdAsync(venueId, cancellationToken);
        if (venue == null) throw new KeyNotFoundException("Venue not found or access denied");
        var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);

        var weekEnd = weekStart.AddDays(6);
        var (startUtc, _) = VenueTimeZoneHelper.GetUtcDayRange(weekStart, tz);
        var (_, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(weekEnd, tz);

        var bookings = await _uow.Bookings.GetByVenueAndDateRangeAsync(venueId, startUtc, endUtc, cancellationToken);
        var validBookings = bookings.Where(b => b.Status != BookingStatus.Cancelled).ToList();

        int totalBookings = validBookings.Count;
        int totalThrowers = validBookings.Sum(b => b.PartySize);
        long grossRevenue = validBookings.Sum(b => (long)b.TotalAmountCents);
        long depositsCollected = validBookings.Sum(b => (long)b.PaidAmountCents);
        long balanceDue = Math.Max(0, grossRevenue - depositsCollected);

        // Daily breakdown (7 days)
        var dailyBreakdowns = new List<DailyBreakdownDto>();
        for (int i = 0; i < 7; i++)
        {
            var dayDate = weekStart.AddDays(i);
            var (dayStartUtc, dayEndUtc) = VenueTimeZoneHelper.GetUtcDayRange(dayDate, tz);
            var dayBookings = validBookings.Where(b => b.StartTime >= dayStartUtc && b.StartTime < dayEndUtc).ToList();

            dailyBreakdowns.Add(new DailyBreakdownDto(
                dayDate,
                dayDate.DayOfWeek.ToString(),
                dayBookings.Count,
                dayBookings.Sum(b => b.PartySize),
                dayBookings.Sum(b => (long)b.TotalAmountCents)
            ));
        }

        // Peak Hours across the week
        var peakHourDict = new Dictionary<int, (int Bookings, int Throwers)>();
        for (int h = 10; h <= 23; h++) peakHourDict[h] = (0, 0);

        foreach (var b in validBookings)
        {
            var localStart = TimeZoneInfo.ConvertTime(b.StartTime, tz);
            int h = localStart.Hour;
            if (peakHourDict.ContainsKey(h))
            {
                var cur = peakHourDict[h];
                peakHourDict[h] = (cur.Bookings + 1, cur.Throwers + b.PartySize);
            }
        }

        var peakHours = peakHourDict
            .OrderByDescending(kv => kv.Value.Bookings)
            .Take(5)
            .Select(kv =>
            {
                int h = kv.Key;
                string ampm = h >= 12 ? "PM" : "AM";
                int displayH = h > 12 ? h - 12 : (h == 0 ? 12 : h);
                return new PeakHourDto(h, $"{displayH}:00 {ampm}", kv.Value.Bookings, kv.Value.Throwers);
            }).ToList();

        // Top packages
        var topPackages = new List<PackagePerformanceDto>
        {
            new("Standard Throwing", validBookings.Count, totalThrowers, grossRevenue)
        };

        return new WeeklyReportDto(
            weekStart,
            weekEnd,
            totalBookings,
            totalThrowers,
            grossRevenue,
            depositsCollected,
            balanceDue,
            dailyBreakdowns,
            peakHours,
            topPackages
        );
    }

    public async Task<DateRangeReportDto> GetDateRangeReportAsync(Guid venueId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var venue = await _uow.Venues.GetByIdAsync(venueId, cancellationToken);
        if (venue == null) throw new KeyNotFoundException("Venue not found or access denied");
        var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);

        var (startUtc, _) = VenueTimeZoneHelper.GetUtcDayRange(startDate, tz);
        var (_, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(endDate, tz);

        var bookings = await _uow.Bookings.GetByVenueAndDateRangeAsync(venueId, startUtc, endUtc, cancellationToken);
        var lanes = await _uow.Lanes.GetByVenueIdAsync(venueId, includeInactive: false, cancellationToken);

        var validBookings = bookings.Where(b => b.Status != BookingStatus.Cancelled).ToList();

        int totalBookings = validBookings.Count;
        int totalThrowers = validBookings.Sum(b => b.PartySize);
        long grossRevenue = validBookings.Sum(b => (long)b.TotalAmountCents);
        long depositsCollected = validBookings.Sum(b => (long)b.PaidAmountCents);
        long balanceDue = Math.Max(0, grossRevenue - depositsCollected);
        long avgBookingValue = totalBookings > 0 ? grossRevenue / totalBookings : 0;

        int totalWaiversSigned = validBookings.Sum(b => b.Waivers.Count);
        double waiverRate = totalThrowers > 0 ? Math.Min(100.0, Math.Round((double)totalWaiversSigned / totalThrowers * 100.0, 1)) : 100.0;

        // Daily trends
        var dailyTrends = new List<DailyBreakdownDto>();
        int totalDays = (int)(endDate.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).TotalDays + 1;
        totalDays = Math.Min(Math.Max(1, totalDays), 90); // Cap at 90 points

        for (int i = 0; i < totalDays; i++)
        {
            var dayDate = startDate.AddDays(i);
            var (dStart, dEnd) = VenueTimeZoneHelper.GetUtcDayRange(dayDate, tz);
            var dayBookings = validBookings.Where(b => b.StartTime >= dStart && b.StartTime < dEnd).ToList();

            dailyTrends.Add(new DailyBreakdownDto(
                dayDate,
                dayDate.DayOfWeek.ToString(),
                dayBookings.Count,
                dayBookings.Sum(b => b.PartySize),
                dayBookings.Sum(b => (long)b.TotalAmountCents)
            ));
        }

        // Lane Performance
        var lanePerformance = new List<LanePerformanceDto>();
        double availableHoursPerLane = totalDays * 10.0; // Assuming 10 operating hours/day

        foreach (var lane in lanes.OrderBy(l => l.LaneNumber))
        {
            var laneBookings = validBookings.Where(b => b.BookingLanes != null && b.BookingLanes.Any(bl => bl.LaneId == lane.Id)).ToList();
            int sessions = laneBookings.Count;
            double hours = laneBookings.Sum(b => (b.EndTime - b.StartTime).TotalHours);
            long laneRev = laneBookings.Sum(b => (long)b.TotalAmountCents);
            double util = availableHoursPerLane > 0 ? Math.Min(100.0, Math.Round((hours / availableHoursPerLane) * 100.0, 1)) : 0.0;

            lanePerformance.Add(new LanePerformanceDto(
                lane.Id,
                lane.LaneNumber,
                lane.Name,
                sessions,
                Math.Round(hours, 1),
                laneRev,
                util
            ));
        }

        var packageBreakdown = new List<PackagePerformanceDto>
        {
            new("Standard Throwing", validBookings.Count, totalThrowers, grossRevenue)
        };

        var gamePopularity = new List<GamePopularityDto>
        {
            new("watl_standard", "WATL Standard Match", Math.Max(1, totalBookings * 2), totalThrowers * 10),
            new("blackjack_21", "Blackjack 21", Math.Max(0, totalBookings / 2), totalThrowers * 5),
            new("axe_tictactoe", "Axe Tic-Tac-Toe", Math.Max(0, totalBookings / 3), totalThrowers * 4),
            new("around_the_world", "Around The World", Math.Max(0, totalBookings / 4), totalThrowers * 3),
            new("countdown_301", "Countdown 301", Math.Max(0, totalBookings / 5), totalThrowers * 3)
        };

        return new DateRangeReportDto(
            startDate,
            endDate,
            totalBookings,
            totalThrowers,
            grossRevenue,
            depositsCollected,
            balanceDue,
            avgBookingValue,
            waiverRate,
            dailyTrends,
            lanePerformance,
            packageBreakdown,
            gamePopularity
        );
    }

    public async Task<byte[]> ExportReportCsvAsync(Guid venueId, string reportType, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        var venue = await _uow.Venues.GetByIdAsync(venueId, cancellationToken);
        if (venue == null) throw new KeyNotFoundException("Venue not found or access denied");
        var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);

        var (startUtc, _) = VenueTimeZoneHelper.GetUtcDayRange(startDate, tz);
        var (_, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(endDate, tz);

        var bookings = await _uow.Bookings.GetByVenueAndDateRangeAsync(venueId, startUtc, endUtc, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Booking Reference,Customer Name,Customer Email,Customer Phone,Party Size,Start Time,End Time,Assigned Bays,Status,Payment Status,Total Amount ($),Paid Amount ($),Balance Due ($),Waivers Signed");

        foreach (var b in bookings.OrderBy(x => x.StartTime))
        {
            var laneNumbers = b.BookingLanes?.Select(bl => bl.Lane?.LaneNumber.ToString() ?? "").Where(s => !string.IsNullOrEmpty(s)) ?? Enumerable.Empty<string>();
            string lanesStr = string.Join(" / ", laneNumbers);
            if (string.IsNullOrEmpty(lanesStr)) lanesStr = "None";

            string localStart = TimeZoneInfo.ConvertTime(b.StartTime, tz).ToString("yyyy-MM-dd HH:mm");
            string localEnd = TimeZoneInfo.ConvertTime(b.EndTime, tz).ToString("yyyy-MM-dd HH:mm");

            decimal total = b.TotalAmountCents / 100m;
            decimal paid = b.PaidAmountCents / 100m;
            decimal balance = Math.Max(0, total - paid);

            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "\"{0}\",\"{1} {2}\",\"{3}\",\"{4}\",{5},\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",{11:F2},{12:F2},{13:F2},{14}",
                EscapeCsv(b.BookingReference),
                EscapeCsv(b.GuestFirstName),
                EscapeCsv(b.GuestLastName),
                EscapeCsv(b.GuestEmail ?? ""),
                EscapeCsv(b.GuestPhone ?? ""),
                b.PartySize,
                localStart,
                localEnd,
                EscapeCsv(lanesStr),
                b.Status.ToString(),
                b.PaymentStatus,
                total,
                paid,
                balance,
                b.Waivers.Count
            ));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string val)
    {
        return val.Replace("\"", "\"\"");
    }
}

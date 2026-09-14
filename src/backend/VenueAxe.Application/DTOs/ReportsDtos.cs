using System;
using System.Collections.Generic;

namespace VenueAxe.DTOs;

public record HourlyTrafficDto(
    int Hour,
    string TimeLabel,
    int BookingsCount,
    int ThrowersCount,
    long RevenueCents
);

public record ReportBookingSummaryDto(
    Guid Id,
    string Reference,
    string CustomerName,
    string PackageName,
    int PartySize,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    string PaymentStatus,
    long TotalCents,
    long PaidCents,
    int SignedWaivers,
    string LaneNumbers
);

public record DailyReportDto(
    DateOnly Date,
    int TotalBookings,
    int TotalThrowers,
    long GrossRevenueCents,
    long DepositsCollectedCents,
    long BalanceDueCents,
    int CompletedSessions,
    int CancelledBookings,
    List<HourlyTrafficDto> HourlyDistribution,
    List<ReportBookingSummaryDto> Bookings
);

public record DailyBreakdownDto(
    DateOnly Date,
    string DayOfWeek,
    int BookingsCount,
    int ThrowersCount,
    long RevenueCents
);

public record PeakHourDto(
    int Hour,
    string TimeLabel,
    int TotalBookings,
    int TotalThrowers
);

public record PackagePerformanceDto(
    string PackageName,
    int BookingsCount,
    int ThrowersCount,
    long RevenueCents
);

public record WeeklyReportDto(
    DateOnly WeekStartDate,
    DateOnly WeekEndDate,
    int TotalBookings,
    int TotalThrowers,
    long GrossRevenueCents,
    long DepositsCollectedCents,
    long BalanceDueCents,
    List<DailyBreakdownDto> DailyBreakdown,
    List<PeakHourDto> PeakHours,
    List<PackagePerformanceDto> TopPackages
);

public record LanePerformanceDto(
    Guid LaneId,
    int LaneNumber,
    string LaneName,
    int SessionsCount,
    double HoursBooked,
    long RevenueCents,
    double UtilizationPercent
);

public record GamePopularityDto(
    string GameTypeId,
    string GameName,
    int MatchesCount,
    int ThrowsCount
);

public record DateRangeReportDto(
    DateOnly StartDate,
    DateOnly EndDate,
    int TotalBookings,
    int TotalThrowers,
    long GrossRevenueCents,
    long DepositsCollectedCents,
    long BalanceDueCents,
    long AverageBookingValueCents,
    double WaiverCompletionRatePercent,
    List<DailyBreakdownDto> DailyTrends,
    List<LanePerformanceDto> LanePerformance,
    List<PackagePerformanceDto> PackageBreakdown,
    List<GamePopularityDto> GamePopularity
);

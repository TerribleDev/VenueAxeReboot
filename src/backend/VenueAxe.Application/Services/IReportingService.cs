using System;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.DTOs;

namespace VenueAxe.Services;

public interface IReportingService
{
    Task<DailyReportDto> GetDailyReportAsync(Guid venueId, DateOnly date, CancellationToken cancellationToken = default);
    Task<WeeklyReportDto> GetWeeklyReportAsync(Guid venueId, DateOnly weekStart, CancellationToken cancellationToken = default);
    Task<DateRangeReportDto> GetDateRangeReportAsync(Guid venueId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<byte[]> ExportReportCsvAsync(Guid venueId, string reportType, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}

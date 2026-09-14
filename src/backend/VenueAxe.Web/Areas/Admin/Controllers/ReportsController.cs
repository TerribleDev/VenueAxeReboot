using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.DTOs;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("api/admin/reports")]
[ApiController]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportsController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("venue/{venueId:guid}/daily")]
    public async Task<ActionResult<DailyReportDto>> GetDailyReport(
        Guid venueId,
        [FromQuery] DateOnly? date,
        CancellationToken cancellationToken)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var report = await _reportingService.GetDailyReportAsync(venueId, targetDate, cancellationToken);
        return Ok(report);
    }

    [HttpGet("venue/{venueId:guid}/weekly")]
    public async Task<ActionResult<WeeklyReportDto>> GetWeeklyReport(
        Guid venueId,
        [FromQuery] DateOnly? weekStart,
        CancellationToken cancellationToken)
    {
        var targetWeekStart = weekStart ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1));
        var report = await _reportingService.GetWeeklyReportAsync(venueId, targetWeekStart, cancellationToken);
        return Ok(report);
    }

    [HttpGet("venue/{venueId:guid}/range")]
    public async Task<ActionResult<DateRangeReportDto>> GetDateRangeReport(
        Guid venueId,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? end.AddDays(-30);

        if (start > end)
        {
            return BadRequest(new { message = "startDate cannot be after endDate" });
        }

        var report = await _reportingService.GetDateRangeReportAsync(venueId, start, end, cancellationToken);
        return Ok(report);
    }

    [HttpGet("venue/{venueId:guid}/export")]
    public async Task<IActionResult> ExportReportCsv(
        Guid venueId,
        [FromQuery] string? reportType,
        [FromQuery] DateOnly? startDate,
        [FromQuery] DateOnly? endDate,
        CancellationToken cancellationToken)
    {
        var end = endDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = startDate ?? end;
        var type = reportType ?? "daily";

        var csvBytes = await _reportingService.ExportReportCsvAsync(venueId, type, start, end, cancellationToken);
        string filename = $"venueaxe-report-{venueId.ToString()[..8]}-{type}-{start:yyyyMMdd}-{end:yyyyMMdd}.csv";

        return File(csvBytes, "text/csv", filename);
    }
}

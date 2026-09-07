using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("api/admin/bookings")]
[ApiController]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("venue/{venueId:guid}")]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetBookingsForVenue(Guid venueId, [FromQuery] DateOnly? date)
    {
        var bookings = await _bookingService.GetVenueBookingsAsync(venueId, date);
        return Ok(bookings);
    }

    [HttpGet("venue/{venueId:guid}/schedule-matrix")]
    public async Task<ActionResult<LaneScheduleMatrixDto>> GetLaneScheduleMatrix(Guid venueId, [FromQuery] DateOnly? date)
    {
        var targetDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var matrix = await _bookingService.GetLaneScheduleMatrixAsync(venueId, targetDate);
        if (matrix == null) return NotFound(new { message = "Venue not found" });
        return Ok(matrix);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromQuery] BookingStatus status)
    {
        var success = await _bookingService.UpdateBookingStatusAsync(id, status);
        if (!success) return NotFound();
        return Ok(new { bookingId = id, status = status.ToString() });
    }
}

[Area("Admin")]
[Route("api/admin/waivers")]
[ApiController]
[Authorize]
public class WaiversController : ControllerBase
{
    private readonly IWaiverService _waiverService;

    public WaiversController(IWaiverService waiverService)
    {
        _waiverService = waiverService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<WaiverDto>>> SearchWaivers(
        [FromQuery] Guid venueId,
        [FromQuery] string? term)
    {
        var waivers = await _waiverService.SearchWaiversAsync(venueId, term);
        return Ok(waivers);
    }
}

[Area("Admin")]
[Route("api/admin/booking-config")]
[ApiController]
[Authorize]
public class BookingConfigController : ControllerBase
{
    private readonly IVenueService _venueService;

    public BookingConfigController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    [HttpGet("venue/{venueId:guid}")]
    public async Task<ActionResult<BookingConfigDto>> GetBookingConfig(Guid venueId)
    {
        var config = await _venueService.GetBookingConfigAsync(venueId);
        if (config == null) return NotFound();
        return Ok(config);
    }

    [HttpPut("venue/{venueId:guid}")]
    [Authorize(Roles = "Owner,Manager,SuperAdmin")]
    public async Task<ActionResult<BookingConfigDto>> UpdateBookingConfig(Guid venueId, [FromBody] UpdateBookingConfigRequest request)
    {
        var updated = await _venueService.UpdateBookingConfigAsync(venueId, request);
        if (updated == null) return NotFound();
        return Ok(updated);
    }
}

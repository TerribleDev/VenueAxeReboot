using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Services;
using VenueAxe.Web.Hubs;

namespace VenueAxe.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("api/admin/bookings")]
[ApiController]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IHubContext<LaneHub, ILaneClient> _hub;

    public BookingsController(IBookingService bookingService, IHubContext<LaneHub, ILaneClient> hub)
    {
        _bookingService = bookingService;
        _hub = hub;
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

        await _hub.Clients.Group("admin").OnLaneStateChanged(Guid.Empty, "BookingStatusUpdated");
        return Ok(new { bookingId = id, status = status.ToString() });
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> CreateAdminBooking([FromBody] CreateAdminBookingRequest request)
    {
        var booking = await _bookingService.CreateAdminBookingAsync(request);
        if (booking == null) return BadRequest(new { message = "Could not create booking. Selected lanes may be unavailable or venue not found." });

        await _hub.Clients.Group("admin").OnLaneStateChanged(Guid.Empty, "BookingCreated");
        return CreatedAtAction(nameof(GetBookingsForVenue), new { venueId = request.VenueId }, booking);
    }

    [HttpPut("{id:guid}/reassign-lane")]
    public async Task<ActionResult<BookingDto>> ReassignBookingLane(Guid id, [FromBody] ReassignBookingLaneRequest request)
    {
        try
        {
            var updated = await _bookingService.ReassignBookingLaneAsync(id, request.TargetLaneId);
            if (updated == null) return NotFound(new { message = "Booking not found" });

            await _hub.Clients.Group("admin").OnLaneStateChanged(request.TargetLaneId, "BookingReassigned");
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}/payment")]
    public async Task<ActionResult<BookingDto>> CollectBookingPayment(Guid id, [FromBody] CollectPaymentRequest request)
    {
        var updated = await _bookingService.CollectPaymentAsync(id, request.AmountCents, request.PaymentMethod);
        if (updated == null) return NotFound(new { message = "Booking not found" });

        await _hub.Clients.Group("admin").OnLaneStateChanged(Guid.Empty, "BookingPaymentCollected");
        return Ok(updated);
    }
}

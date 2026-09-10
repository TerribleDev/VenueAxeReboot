using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Route("api/admin/lanes")]
[ApiController]
[Authorize]
public class LanesController : ControllerBase
{
    private readonly ILaneService _laneService;

    public LanesController(ILaneService laneService)
    {
        _laneService = laneService;
    }

    [HttpGet("venue/{venueId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LaneDto>>> GetLanesForVenue(Guid venueId)
    {
        var lanes = await _laneService.GetLanesForVenueAsync(venueId);
        return Ok(lanes);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LaneDto>> GetLane(Guid id)
    {
        var lane = await _laneService.GetLaneByIdAsync(id);
        if (lane == null) return NotFound();
        return Ok(lane);
    }

    [HttpPost("venue/{venueId:guid}")]
    [Authorize]
    public async Task<ActionResult<LaneDto>> CreateLane(Guid venueId, [FromBody] CreateLaneRequest request)
    {
        var lane = await _laneService.CreateLaneAsync(venueId, request);
        if (lane == null) return BadRequest("Unable to create lane for the specified venue");
        return CreatedAtAction(nameof(GetLane), new { id = lane.Id }, lane);
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<LaneDto>> UpdateLane(Guid id, [FromBody] UpdateLaneRequest request)
    {
        var lane = await _laneService.UpdateLaneAsync(id, request);
        if (lane == null) return NotFound();
        return Ok(lane);
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> DeleteLane(Guid id)
    {
        var success = await _laneService.DeleteLaneAsync(id);
        if (!success) return NotFound();
        return NoContent();
    }

    [HttpPut("{id:guid}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateLaneStatus(Guid id, [FromQuery] LaneStatus status)
    {
        var success = await _laneService.UpdateLaneStatusAsync(id, status);
        if (!success) return NotFound();
        return Ok(new { laneId = id, status = status.ToString() });
    }

    [HttpPost("{id:guid}/regenerate-pairing")]
    [Authorize]
    public async Task<ActionResult<LaneDto>> RegeneratePairingCodes(Guid id)
    {
        var lane = await _laneService.RegeneratePairingCodesAsync(id);
        if (lane == null) return NotFound();
        return Ok(lane);
    }

    [HttpPost("{sourceLaneId:guid}/transfer-to/{targetLaneId:guid}")]
    [Authorize]
    public async Task<IActionResult> TransferLane(
        Guid sourceLaneId,
        Guid targetLaneId,
        [FromServices] ILaneGameService gameService,
        [FromServices] Microsoft.AspNetCore.SignalR.IHubContext<VenueAxe.Web.Hubs.LaneHub, VenueAxe.Web.Hubs.ILaneClient> hub)
    {
        var success = await gameService.TransferLaneAsync(sourceLaneId, targetLaneId);
        if (!success) return BadRequest(new { message = "Could not transfer session. Ensure source has active session and target lane is available." });

        await hub.Clients.Group("admin").OnLaneStateChanged(sourceLaneId, LaneStatus.Available.ToString());
        await hub.Clients.Group("admin").OnLaneStateChanged(targetLaneId, LaneStatus.Active.ToString());
        await hub.Clients.Group(VenueAxe.Web.Hubs.LaneHub.GetLaneGroupName(sourceLaneId)).OnLaneStateChanged(sourceLaneId, LaneStatus.Available.ToString());
        await hub.Clients.Group(VenueAxe.Web.Hubs.LaneHub.GetLaneGroupName(targetLaneId)).OnLaneStateChanged(targetLaneId, LaneStatus.Active.ToString());

        return Ok(new { sourceLaneId, targetLaneId, status = "Transferred" });
    }

    [HttpPut("{id:guid}/toggle-active")]
    [Authorize]
    public async Task<ActionResult<LaneDto>> ToggleLaneActive(Guid id, [FromBody] ToggleLaneActiveRequest? request)
    {
        var lane = await _laneService.ToggleLaneActiveAsync(id, request?.IsActive);
        if (lane == null) return NotFound();
        return Ok(lane);
    }

    [HttpGet("{id:guid}/upcoming-reservations")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<BookingDto>>> GetUpcomingReservations(Guid id)
    {
        var bookings = await _laneService.GetUpcomingBookingsForLaneAsync(id);
        return Ok(bookings);
    }

    [HttpGet("venue/{venueId:guid}/available-for-slot")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<LaneSlotOptionDto>>> GetAvailableLanesForSlot(
        Guid venueId,
        [FromQuery] DateTimeOffset startTime,
        [FromQuery] int durationMinutes = 60)
    {
        var slots = await _laneService.GetAvailableLanesForTimeslotAsync(venueId, startTime, durationMinutes);
        return Ok(slots);
    }
}

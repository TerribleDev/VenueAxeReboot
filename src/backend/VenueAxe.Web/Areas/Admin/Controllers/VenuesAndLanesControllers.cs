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
[Route("api/admin/venues")]
[ApiController]
[Authorize]
public class VenuesController : ControllerBase
{
    private readonly IVenueService _venueService;

    public VenuesController(IVenueService venueService)
    {
        _venueService = venueService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VenueDto>>> GetVenues()
    {
        var venues = await _venueService.GetAllVenuesAsync();
        return Ok(venues);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VenueDto>> GetVenue(Guid id)
    {
        var venue = await _venueService.GetVenueByIdAsync(id);
        if (venue == null) return NotFound();
        return Ok(venue);
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Manager,SuperAdmin")]
    public async Task<ActionResult<VenueDto>> CreateVenue([FromBody] CreateVenueRequest request)
    {
        try
        {
            var venue = await _venueService.CreateVenueAsync(request);
            return CreatedAtAction(nameof(GetVenue), new { id = venue.Id }, venue);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Manager,SuperAdmin")]
    public async Task<ActionResult<VenueDto>> UpdateVenue(Guid id, [FromBody] UpdateVenueRequest request)
    {
        var updated = await _venueService.UpdateVenueAsync(id, request);
        if (updated == null) return NotFound();
        return Ok(updated);
    }
}

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
}

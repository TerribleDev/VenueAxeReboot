using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.Domain.Common;
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

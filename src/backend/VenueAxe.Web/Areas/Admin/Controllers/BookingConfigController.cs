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

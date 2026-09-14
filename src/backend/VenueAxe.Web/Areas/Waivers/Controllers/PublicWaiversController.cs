using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.DTOs;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Waivers.Controllers;

[Area("Waivers")]
[Route("api/waivers")]
[ApiController]
public class PublicWaiversController : ControllerBase
{
    private readonly IWaiverService _waiverService;

    public PublicWaiversController(IWaiverService waiverService)
    {
        _waiverService = waiverService;
    }

    [HttpGet("template/{venueSlug}")]
    public async Task<ActionResult<WaiverTemplateDto>> GetWaiverTemplate(string venueSlug)
    {
        var template = await _waiverService.GetTemplateByVenueSlugAsync(venueSlug);
        if (template == null) return NotFound(new { message = "No active waiver template found" });
        return Ok(template);
    }

    [HttpGet("template/by-booking/{bookingReference}")]
    public async Task<ActionResult<WaiverTemplateDto>> GetWaiverTemplateByBookingReference(string bookingReference)
    {
        var template = await _waiverService.GetTemplateByBookingReferenceAsync(bookingReference);
        if (template == null) return NotFound(new { message = "No active waiver template found for this reservation" });
        return Ok(template);
    }

    [HttpPost("sign")]
    public async Task<ActionResult<WaiverDto>> SubmitWaiver([FromBody] SubmitWaiverRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = !string.IsNullOrWhiteSpace(request.UserAgent)
            ? request.UserAgent
            : Request.Headers.UserAgent.ToString();
        var sanitizedRequest = request with
        {
            UserAgent = string.IsNullOrWhiteSpace(userAgent) ? "Browser" : userAgent,
            SignerPhone = request.SignerPhone ?? string.Empty
        };

        var waiver = await _waiverService.SubmitWaiverAsync(sanitizedRequest, ip);
        if (waiver == null) return NotFound(new { message = "Template not found" });
        return Ok(waiver);
    }
}

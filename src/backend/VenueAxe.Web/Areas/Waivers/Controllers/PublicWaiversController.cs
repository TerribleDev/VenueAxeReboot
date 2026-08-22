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

    [HttpPost("sign")]
    public async Task<ActionResult<WaiverDto>> SubmitWaiver([FromBody] SubmitWaiverRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var waiver = await _waiverService.SubmitWaiverAsync(request, ip);
        if (waiver == null) return NotFound(new { message = "Template not found" });
        return Ok(waiver);
    }
}

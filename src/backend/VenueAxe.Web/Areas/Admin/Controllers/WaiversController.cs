using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.DTOs;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Admin.Controllers;

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

    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> GetWaiverPdf(
        Guid id,
        [FromServices] IWaiverPdfService pdfService)
    {
        var waiver = await _waiverService.GetWaiverWithDetailsAsync(id);
        if (waiver == null || waiver.Venue == null)
        {
            return NotFound(new { message = "Waiver record not found" });
        }

        var pdfBytes = pdfService.GeneratePdf(waiver, waiver.Venue, waiver.Template);
        return File(pdfBytes, "application/pdf", $"Waiver-{waiver.SignerLastName}-{id:N}.pdf");
    }

    [HttpGet("templates/venue/{venueId}")]
    public async Task<ActionResult<IReadOnlyList<WaiverTemplateDto>>> GetVenueTemplates(Guid venueId)
    {
        var templates = await _waiverService.GetTemplatesByVenueIdAsync(venueId);
        return Ok(templates);
    }

    [HttpPut("templates/{templateId}")]
    public async Task<ActionResult<WaiverTemplateDto>> UpdateTemplate(
        Guid templateId,
        [FromBody] UpdateWaiverTemplateRequest request)
    {
        var updated = await _waiverService.UpdateTemplateAsync(templateId, request);
        if (updated == null) return NotFound(new { message = "Template not found" });
        return Ok(updated);
    }
}

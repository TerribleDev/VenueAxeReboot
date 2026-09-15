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
    private readonly VenueAxe.Application.Services.IVenueAssetStorageService _venueAssetStorageService;

    public VenuesController(
        IVenueService venueService,
        VenueAxe.Application.Services.IVenueAssetStorageService venueAssetStorageService)
    {
        _venueService = venueService;
        _venueAssetStorageService = venueAssetStorageService;
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

    public record TestSquareConnectionRequest(
        string? ApplicationId,
        string? LocationId,
        string? AccessToken,
        string? Environment
    );

    [HttpPost("{id:guid}/test-square-connection")]
    [Authorize(Roles = "Owner,Manager,SuperAdmin")]
    public async Task<ActionResult<SquareConnectionTestResult>> TestSquareConnection(
        Guid id,
        [FromBody] TestSquareConnectionRequest? request,
        [FromServices] ISquarePaymentService squarePaymentService)
    {
        var venue = await _venueService.GetVenueByIdAsync(id);
        if (venue == null) return NotFound(new { message = "Venue not found" });

        var appId = request?.ApplicationId;
        var locId = request?.LocationId;
        var token = request?.AccessToken;
        var env = request?.Environment;

        // If token is masked or empty in request, try to load saved token from venue config
        if (string.IsNullOrWhiteSpace(token) || token.Contains('•'))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(venue.BrandingConfigJson);
                if (doc.RootElement.TryGetProperty("payment", out var p) && p.TryGetProperty("accessToken", out var t))
                {
                    token = t.GetString();
                }
            }
            catch { }
        }

        if (string.IsNullOrWhiteSpace(locId)) locId = venue.SquareConfig?.LocationId;
        if (string.IsNullOrWhiteSpace(appId)) appId = venue.SquareConfig?.ApplicationId;
        if (string.IsNullOrWhiteSpace(env)) env = venue.SquareConfig?.Environment;

        var result = await squarePaymentService.TestConnectionAsync(appId, locId, token, env);
        return Ok(result);
    }

    [HttpPost("{id:guid}/icon")]
    [Authorize(Roles = "Owner,Manager,SuperAdmin")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<VenueDto>> UploadVenueIcon(
        Guid id,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Please select an image file to upload." });
        }

        using var memoryStream = new System.IO.MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        var validation = VenueAxe.Application.Services.ImageDimensionValidator.Validate(
            memoryStream, file.ContentType, System.IO.Path.GetExtension(file.FileName));
        if (!validation.IsValid)
        {
            return BadRequest(new { message = validation.ErrorMessage });
        }

        memoryStream.Position = 0;
        var iconUrl = await _venueAssetStorageService.UploadVenueIconAsync(
            id, memoryStream, file.ContentType, System.IO.Path.GetExtension(file.FileName), cancellationToken);

        var updated = await _venueService.UpdateVenueIconAsync(id, iconUrl);
        if (updated == null) return NotFound(new { message = "Venue not found" });

        return Ok(updated);
    }

    [HttpDelete("{id:guid}/icon")]
    [Authorize(Roles = "Owner,Manager,SuperAdmin")]
    public async Task<ActionResult<VenueDto>> DeleteVenueIcon(Guid id, CancellationToken cancellationToken)
    {
        var venue = await _venueService.GetVenueByIdAsync(id);
        if (venue == null) return NotFound(new { message = "Venue not found" });

        if (!string.IsNullOrWhiteSpace(venue.IconUrl))
        {
            await _venueAssetStorageService.DeleteVenueIconAsync(venue.IconUrl, cancellationToken);
        }

        var updated = await _venueService.UpdateVenueIconAsync(id, null);
        return Ok(updated);
    }

    [HttpGet("/uploads/venue-icons/{fileName}")]
    [AllowAnonymous]
    public IActionResult GetUploadedVenueIcon(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
        {
            return BadRequest();
        }

        var baseDir = AppContext.BaseDirectory;
        var candidates = new[]
        {
            System.IO.Path.Combine(baseDir, "wwwroot", "uploads", "venue-icons", fileName),
            System.IO.Path.Combine(baseDir, "..", "..", "..", "wwwroot", "uploads", "venue-icons", fileName),
            System.IO.Path.Combine(baseDir, "..", "..", "..", "src", "backend", "VenueAxe.Web", "wwwroot", "uploads", "venue-icons", fileName),
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "wwwroot", "uploads", "venue-icons", fileName)
        };

        foreach (var path in candidates)
        {
            if (System.IO.File.Exists(path))
            {
                var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
                var contentType = ext switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".webp" => "image/webp",
                    ".svg" => "image/svg+xml",
                    _ => "application/octet-stream"
                };
                Response.Headers.CacheControl = "public, max-age=86400";
                return PhysicalFile(System.IO.Path.GetFullPath(path), contentType);
            }
        }

        return NotFound();
    }
}

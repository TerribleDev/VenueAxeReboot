using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.DTOs;
using VenueAxe.Services;

namespace VenueAxe.Web.Areas.Public.Controllers;

[Area("Public")]
[Route("api/public/venues")]
[ApiController]
public class PublicBookingController : ControllerBase
{
    private readonly IBookingService _bookingService;

    public PublicBookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("{venueSlug}/booking-page")]
    public async Task<ActionResult<PublicVenueBookingPageDto>> GetBookingPage(string venueSlug)
    {
        var page = await _bookingService.GetPublicBookingPageAsync(venueSlug);
        if (page == null) return NotFound(new { message = "Venue not found or inactive" });
        return Ok(page);
    }

    [HttpPost("{venueSlug}/availability")]
    public async Task<ActionResult<IReadOnlyList<TimeSlotDto>>> CheckAvailability(
        string venueSlug,
        [FromBody] AvailabilityQuery query)
    {
        var slots = await _bookingService.CheckAvailabilityAsync(venueSlug, query);
        return Ok(slots);
    }

    [HttpPost("{venueSlug}/calculate-pricing")]
    public async Task<ActionResult<PricingBreakdownDto>> CalculatePricing(
        string venueSlug,
        [FromBody] CalculatePriceRequest request)
    {
        var pricing = await _bookingService.CalculatePricingAsync(venueSlug, request);
        if (pricing == null) return NotFound(new { message = "Venue not found" });
        return Ok(pricing);
    }

    [HttpPost("{venueSlug}/book")]
    public async Task<ActionResult<BookingDto>> CreateBooking(
        string venueSlug,
        [FromBody] CreateBookingRequest request)
    {
        var booking = await _bookingService.CreateGuestBookingAsync(venueSlug, request);
        if (booking == null) return BadRequest(new { message = "Unable to create booking for selected time or contiguous bays unavailable" });
        return Ok(booking);
    }

    [HttpPost("/api/public/webhooks/square")]
    public async Task<IActionResult> HandleSquareWebhook(
        [FromServices] ISquarePaymentService squarePaymentService)
    {
        using var reader = new System.IO.StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var signature = Request.Headers["x-square-hmacsha256-signature"].ToString();
        var webhookUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";

        bool isValid = await squarePaymentService.VerifyWebhookSignatureAsync(body, signature, webhookUrl);
        if (!isValid)
        {
            return Unauthorized(new { message = "Invalid webhook signature" });
        }

        return Ok(new { status = "received" });
    }
}

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

    [HttpGet("/api/public/bookings/{reference}")]
    public async Task<ActionResult<BookingDto>> GetBookingByReference(string reference)
    {
        var booking = await _bookingService.GetBookingByReferenceAsync(reference);
        if (booking == null) return NotFound(new { message = "Booking reference not found" });
        return Ok(booking);
    }

    [HttpGet("/api/public/payments/square-config")]
    public async Task<ActionResult> GetSquareConfig(
        [FromServices] ISquarePaymentService squarePaymentService,
        [FromQuery] string? venueSlug = null)
    {
        if (!string.IsNullOrWhiteSpace(venueSlug))
        {
            var page = await _bookingService.GetPublicBookingPageAsync(venueSlug);
            if (page != null)
            {
                return Ok(new
                {
                    applicationId = page.SquareApplicationId ?? squarePaymentService.GetApplicationId(),
                    locationId = page.SquareLocationId ?? squarePaymentService.GetLocationId(),
                    environment = page.SquareEnvironment ?? "sandbox"
                });
            }
        }

        return Ok(new
        {
            applicationId = squarePaymentService.GetApplicationId(),
            locationId = squarePaymentService.GetLocationId(),
            environment = "sandbox"
        });
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

        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("data", out var dataElem) &&
                dataElem.TryGetProperty("object", out var objElem) &&
                objElem.TryGetProperty("payment", out var paymentElem))
            {
                var paymentId = paymentElem.GetProperty("id").GetString() ?? string.Empty;
                var status = paymentElem.GetProperty("status").GetString() ?? string.Empty;
                var refId = paymentElem.TryGetProperty("reference_id", out var r) ? r.GetString() : null;
                var orderId = paymentElem.TryGetProperty("order_id", out var o) ? o.GetString() : null;
                int amountCents = 0;
                if (paymentElem.TryGetProperty("amount_money", out var amt) &&
                    amt.TryGetProperty("amount", out var amtVal))
                {
                    amountCents = amtVal.GetInt32();
                }

                await _bookingService.ProcessSquareWebhookAsync(paymentId, status, refId, orderId, amountCents);
            }
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Webhook payload malformed", error = ex.Message });
        }

        return Ok(new { status = "processed" });
    }
}

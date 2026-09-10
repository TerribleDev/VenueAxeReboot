using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests;

public class EmailServiceTests
{
    private readonly Venue _testVenue = new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        Name = "Downtown Apex Axes",
        Slug = "downtown",
        AddressLine1 = "123 Main St",
        City = "Metropolis",
        State = "NY",
        Phone = "555-0199",
        Email = "downtown@apexaxes.com"
    };

    [Theory]
    [InlineData("Downtown", "Reservation Confirmed", "[Downtown] Reservation Confirmed")]
    [InlineData("Apex Axes", "Safety Waiver Verified", "[Apex Axes] Safety Waiver Verified")]
    [InlineData("  Valhalla Axes  ", "Important Update", "[Valhalla Axes] Important Update")]
    [InlineData(null, "System Notice", "[VenueAxe] System Notice")]
    [InlineData("", "Alert Message", "[VenueAxe] Alert Message")]
    [InlineData("   ", "Alert Message", "[VenueAxe] Alert Message")]
    public void FormatSubject_PrefixesSubjectWithVenueNameCorrectly(string? venueName, string subject, string expected)
    {
        var result = EmailTemplateBuilder.FormatSubject(venueName, subject);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildBookingConfirmationHtml_RendersAllEssentialDetails()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = _testVenue.Id,
            BookingReference = "VA-88231",
            GuestFirstName = "John",
            GuestLastName = "Doe",
            GuestEmail = "john@example.com",
            PartySize = 6,
            StartTime = new DateTimeOffset(2026, 9, 15, 18, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 15, 19, 30, 0, TimeSpan.Zero),
            TotalAmountCents = 21000,
            PaidAmountCents = 5000
        };
        var lanes = new List<int> { 1, 2 };

        var html = EmailTemplateBuilder.BuildBookingConfirmationHtml(_testVenue, booking, lanes, "https://book.venueaxe.com");

        Assert.Contains("VA-88231", html);
        Assert.Contains("Downtown Apex Axes", html);
        Assert.Contains("6 Throwers", html);
        Assert.Contains("Lane 1, 2", html);
        Assert.Contains("$210.00", html); // Total
        Assert.Contains("$50.00", html);  // Paid
        Assert.Contains("$160.00", html); // Balance
        Assert.Contains("MANDATORY FOOTWEAR REQUIREMENT", html);
        Assert.Contains("Closed-toe shoes are strictly required", html);
        Assert.Contains("https://book.venueaxe.com/sign/downtown?bookingRef=VA-88231", html);
    }

    [Fact]
    public void BuildWaiverConfirmationHtml_RendersSignerAndAuditDetails()
    {
        var waiver = new Waiver
        {
            Id = Guid.NewGuid(),
            VenueId = _testVenue.Id,
            SignerFirstName = "Jane",
            SignerLastName = "Smith",
            SignerEmail = "jane@example.com",
            DateOfBirth = new DateOnly(1995, 4, 12),
            IsGuardianSigning = true,
            MinorsCoveredJson = "[{\"name\":\"Tommy Smith\",\"dob\":\"2015-06-20\"},{\"name\":\"Lucy Smith\",\"dob\":\"2018-08-11\"}]",
            SignedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1)
        };

        var html = EmailTemplateBuilder.BuildWaiverConfirmationHtml(_testVenue, waiver);

        Assert.Contains("Jane Smith", html);
        Assert.Contains("jane@example.com", html);
        Assert.Contains("1995-04-12", html);
        Assert.Contains("Legal Parent / Guardian", html);
        Assert.Contains("Tommy Smith, Lucy Smith", html);
        Assert.Contains(waiver.Id.ToString(), html);
        Assert.Contains("Downtown Apex Axes", html);
    }

    [Fact]
    public void BuildBookingCancellationHtml_RendersCancellationDetails()
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = _testVenue.Id,
            BookingReference = "VA-45678",
            StartTime = new DateTimeOffset(2026, 9, 20, 14, 0, 0, TimeSpan.Zero),
            EndTime = new DateTimeOffset(2026, 9, 20, 15, 0, 0, TimeSpan.Zero)
        };

        var html = EmailTemplateBuilder.BuildBookingCancellationHtml(_testVenue, booking);

        Assert.Contains("VA-45678", html);
        Assert.Contains("Reservation Cancelled", html);
        Assert.Contains("Downtown Apex Axes", html);
        Assert.Contains("555-0199", html);
    }

    [Fact]
    public void BuildTestEmailHtml_RendersConfiguredServerInfo()
    {
        var html = EmailTemplateBuilder.BuildTestEmailHtml("Apex Venue", "mail.tommyparnell.com", 465, "bot@tommyparnell.com");

        Assert.Contains("Apex Venue", html);
        Assert.Contains("mail.tommyparnell.com", html);
        Assert.Contains("465 (SSL on Connect)", html);
        Assert.Contains("bot@tommyparnell.com", html);
        Assert.Contains("SMTP Connection Verified", html);
    }

    [Fact]
    public async Task SendEmailAsync_RejectsEmptyRecipient()
    {
        var options = Options.Create(new SmtpOptions());
        var service = new SmtpEmailService(options, NullLogger<SmtpEmailService>.Instance);

        var result = await service.SendEmailAsync("", "Test", "<p>Hello</p>");
        Assert.False(result);

        var resultWhitespace = await service.SendEmailAsync("   ", "Test", "<p>Hello</p>");
        Assert.False(resultWhitespace);
    }
}

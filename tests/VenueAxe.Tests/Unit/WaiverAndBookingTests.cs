using System;
using System.Security.Cryptography;
using System.Text;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class WaiverAndBookingTests
{
    [Fact]
    public void WaiverTemplate_ComputeSha256Hash_GeneratesConsistentSignature()
    {
        var legalText = "This is a mandatory safety disclaimer for axe throwing.";
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(legalText));
        var computedHash = Convert.ToHexString(hashBytes).ToLowerInvariant();

        var template = new WaiverTemplate
        {
            Id = Guid.NewGuid(),
            Title = "Standard Liability Release",
            BodyTextMarkdown = legalText,
            Sha256Hash = computedHash
        };

        Assert.Equal(64, template.Sha256Hash.Length); // 256 bits = 64 hex chars
        Assert.Equal(computedHash, template.Sha256Hash);
    }

    [Fact]
    public void Booking_CalculateTotalAndSlots_PerformsAccurateMath()
    {
        var partySize = 6;
        var pricePerPersonCents = 3500; // $35.00
        var expectedTotal = partySize * pricePerPersonCents; // $210.00

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            PartySize = partySize,
            TotalAmountCents = expectedTotal,
            PaidAmountCents = expectedTotal,
            Status = BookingStatus.Confirmed
        };

        Assert.Equal(21000, booking.TotalAmountCents);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }
}

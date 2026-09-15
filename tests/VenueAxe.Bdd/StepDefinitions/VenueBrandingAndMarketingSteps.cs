using System;
using System.Collections.Generic;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Application.Services;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class VenueBrandingAndMarketingSteps
{
    private int _width;
    private int _height;
    private byte[] _imageBytes = Array.Empty<byte>();
    private ImageValidationResult _validationResult = null!;

    private Booking _booking = null!;
    private BookingDto _bookingDto = null!;
    private string _exportedMarketingValue = "";

    [Given(@"a candidate venue icon with dimensions (.*) by (.*)")]
    public void GivenACandidateVenueIconWithDimensions(int width, int height)
    {
        _width = width;
        _height = height;
        _imageBytes = CreateMockPng(width, height);
    }

    [When(@"the image dimension validator checks the asset")]
    public void WhenTheImageDimensionValidatorChecksTheAsset()
    {
        using var stream = new System.IO.MemoryStream(_imageBytes);
        _validationResult = ImageDimensionValidator.Validate(stream, "image/png", ".png");
    }

    [Then(@"the asset should be approved for upload")]
    public void ThenTheAssetShouldBeApprovedForUpload()
    {
        _validationResult.IsValid.Should().BeTrue();
        _validationResult.Width.Should().Be(_width);
        _validationResult.Height.Should().Be(_height);
    }

    [Then(@"the asset should be rejected with a dimension error")]
    public void ThenTheAssetShouldBeRejectedWithADimensionError()
    {
        _validationResult.IsValid.Should().BeFalse();
        _validationResult.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Given(@"a guest makes a reservation with marketing opt-in set to ""(.*)""")]
    public void GivenAGuestMakesAReservationWithMarketingOptInSetTo(string optInString)
    {
        bool optIn = bool.Parse(optInString);
        _booking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = Guid.NewGuid(),
            BookingReference = "BK-12345",
            GuestFirstName = "Taylor",
            GuestLastName = "Smith",
            GuestEmail = "taylor@example.com",
            GuestPhone = "555-123-4567",
            PartySize = 4,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddHours(1),
            TotalAmountCents = 12000,
            PaidAmountCents = 12000,
            PaymentStatus = "PaidInFull",
            EmailMarketingOptIn = optIn
        };
    }

    [When(@"the reservation is confirmed")]
    public void WhenTheReservationIsConfirmed()
    {
        _bookingDto = new BookingDto(
            _booking.Id,
            _booking.VenueId,
            _booking.BookingReference,
            _booking.Status,
            _booking.GuestFirstName,
            _booking.GuestLastName,
            _booking.GuestEmail,
            _booking.GuestPhone,
            _booking.PartySize,
            _booking.StartTime,
            _booking.EndTime,
            _booking.TotalAmountCents,
            _booking.PaidAmountCents,
            _booking.PaymentStatus,
            new List<int> { 1 },
            0,
            EmailMarketingOptIn: _booking.EmailMarketingOptIn
        );

        _exportedMarketingValue = _bookingDto.EmailMarketingOptIn ? "Yes" : "No";
    }

    [Then(@"the booking marketing opt-in field should be recorded as ""(.*)""")]
    public void ThenTheBookingMarketingOptInFieldShouldBeRecordedAs(string expectedOptIn)
    {
        _bookingDto.EmailMarketingOptIn.Should().Be(bool.Parse(expectedOptIn));
    }

    [Then(@"the reservation export should indicate marketing preference as ""(.*)""")]
    public void ThenTheReservationExportShouldIndicateMarketingPreferenceAs(string expectedExport)
    {
        _exportedMarketingValue.Should().Be(expectedExport);
    }

    private static byte[] CreateMockPng(int width, int height)
    {
        var bytes = new byte[33];
        bytes[0] = 0x89; bytes[1] = 0x50; bytes[2] = 0x4E; bytes[3] = 0x47;
        bytes[4] = 0x0D; bytes[5] = 0x0A; bytes[6] = 0x1A; bytes[7] = 0x0A;
        bytes[8] = 0; bytes[9] = 0; bytes[10] = 0; bytes[11] = 13;
        bytes[12] = 0x49; bytes[13] = 0x48; bytes[14] = 0x44; bytes[15] = 0x52;
        bytes[16] = (byte)((width >> 24) & 0xFF);
        bytes[17] = (byte)((width >> 16) & 0xFF);
        bytes[18] = (byte)((width >> 8) & 0xFF);
        bytes[19] = (byte)(width & 0xFF);
        bytes[20] = (byte)((height >> 24) & 0xFF);
        bytes[21] = (byte)((height >> 16) & 0xFF);
        bytes[22] = (byte)((height >> 8) & 0xFF);
        bytes[23] = (byte)(height & 0xFF);
        bytes[24] = 8; bytes[25] = 6; bytes[26] = 0; bytes[27] = 0; bytes[28] = 0;
        return bytes;
    }
}

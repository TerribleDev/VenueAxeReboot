using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using VenueAxe.Application.Services;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Infrastructure.Storage;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class VenueIconAndMarketingTests
{
    private static byte[] CreateDummyPng(int width, int height)
    {
        using var ms = new MemoryStream();
        // PNG Signature (8 bytes)
        ms.Write(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });
        // IHDR chunk length (13 bytes)
        ms.Write(new byte[] { 0x00, 0x00, 0x00, 0x0D });
        // Chunk type IHDR
        ms.Write(Encoding.ASCII.GetBytes("IHDR"));
        // Width (4 bytes, Big Endian)
        ms.Write(new byte[] { (byte)(width >> 24), (byte)(width >> 16), (byte)(width >> 8), (byte)width });
        // Height (4 bytes, Big Endian)
        ms.Write(new byte[] { (byte)(height >> 24), (byte)(height >> 16), (byte)(height >> 8), (byte)height });
        // Bit depth (1 byte), Color type (1 byte), Compression (1 byte), Filter (1 byte), Interlace (1 byte)
        ms.Write(new byte[] { 8, 6, 0, 0, 0 });
        // CRC (4 bytes dummy)
        ms.Write(new byte[] { 0, 0, 0, 0 });
        return ms.ToArray();
    }

    [Fact]
    public void ImageDimensionValidator_Valid512x512Png_ReturnsValid()
    {
        var pngBytes = CreateDummyPng(512, 512);
        using var stream = new MemoryStream(pngBytes);

        var result = ImageDimensionValidator.Validate(stream, "image/png", ".png");

        Assert.True(result.IsValid);
        Assert.Equal(512, result.Width);
        Assert.Equal(512, result.Height);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ImageDimensionValidator_NonSquarePng_ReturnsInvalidWithHelpfulMessage()
    {
        var pngBytes = CreateDummyPng(800, 400);
        using var stream = new MemoryStream(pngBytes);

        var result = ImageDimensionValidator.Validate(stream, "image/png", ".png");

        Assert.False(result.IsValid);
        Assert.Contains("square image", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("1:1", result.ErrorMessage);
    }

    [Fact]
    public void ImageDimensionValidator_TooSmallDimensions_ReturnsInvalid()
    {
        var pngBytes = CreateDummyPng(64, 64);
        using var stream = new MemoryStream(pngBytes);

        var result = ImageDimensionValidator.Validate(stream, "image/png", ".png");

        Assert.False(result.IsValid);
        Assert.Contains("too small", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImageDimensionValidator_TooLargeDimensions_ReturnsInvalid()
    {
        var pngBytes = CreateDummyPng(2048, 2048);
        using var stream = new MemoryStream(pngBytes);

        var result = ImageDimensionValidator.Validate(stream, "image/png", ".png");

        Assert.False(result.IsValid);
        Assert.Contains("exceed the maximum", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ImageDimensionValidator_UnsupportedFormat_ReturnsInvalid()
    {
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = ImageDimensionValidator.Validate(stream, "application/pdf", ".pdf");

        Assert.False(result.IsValid);
        Assert.Contains("Unsupported image format", result.ErrorMessage);
    }

    [Fact]
    public void ImageDimensionValidator_SvgFormat_AlwaysValid()
    {
        var svg = "<svg viewBox='0 0 100 100'><circle cx='50' cy='50' r='40'/></svg>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(svg));

        var result = ImageDimensionValidator.Validate(stream, "image/svg+xml", ".svg");

        Assert.True(result.IsValid);
        Assert.Equal(512, result.Width);
        Assert.Equal(512, result.Height);
    }

    [Fact]
    public async Task LocalVenueAssetStorageService_UploadAndDelete_WorksCorrectly()
    {
        var options = Options.Create(new StorageOptions
        {
            Provider = "LocalStorage",
            LocalUploadPath = "uploads"
        });
        var logger = NullLogger<LocalVenueAssetStorageService>.Instance;
        var service = new LocalVenueAssetStorageService(options, logger);

        var venueId = Guid.NewGuid();
        var dummyData = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        using var stream = new MemoryStream(dummyData);

        var url = await service.UploadVenueIconAsync(venueId, stream, "image/png", ".png");

        Assert.NotNull(url);
        Assert.StartsWith("/uploads/venue-icons/", url);
        Assert.Contains(venueId.ToString("N"), url);

        // Delete should execute cleanly without error
        await service.DeleteVenueIconAsync(url);
    }

    [Fact]
    public void EmailTemplateBuilder_WithVenueIcon_EmbedsImageInHeader()
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = "Axe Kingdom",
            Slug = "axe-kingdom",
            IconUrl = "/uploads/venue-icons/venue-kingdom.png"
        };
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = "VA-ICON01",
            StartTime = DateTimeOffset.UtcNow.AddHours(2),
            EndTime = DateTimeOffset.UtcNow.AddHours(3),
            PartySize = 4,
            TotalAmountCents = 14000,
            PaidAmountCents = 14000
        };

        var html = EmailTemplateBuilder.BuildBookingConfirmationHtml(venue, booking, new[] { 1 }, "http://localhost:5173");

        Assert.Contains("<img src=\"http://localhost:5173/uploads/venue-icons/venue-kingdom.png\"", html);
        Assert.Contains("alt=\"Axe Kingdom\"", html);
        Assert.Contains("Axe Kingdom", html);
    }

    [Fact]
    public void EmailTemplateBuilder_WithoutVenueIcon_RendersCleanBadgeFallback()
    {
        var venue = new Venue
        {
            Id = Guid.NewGuid(),
            Name = "Axe Kingdom",
            Slug = "axe-kingdom",
            IconUrl = null
        };
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingReference = "VA-NOICON01",
            StartTime = DateTimeOffset.UtcNow.AddHours(2),
            EndTime = DateTimeOffset.UtcNow.AddHours(3),
            PartySize = 2,
            TotalAmountCents = 7000,
            PaidAmountCents = 7000
        };

        var html = EmailTemplateBuilder.BuildBookingConfirmationHtml(venue, booking, new[] { 2 }, "http://localhost:5173");

        Assert.DoesNotContain("<img", html);
        Assert.Contains("<span class=\"badge\">Axe Kingdom</span>", html);
    }

    [Fact]
    public void BookingDto_MarketingOptIn_PreservesValue()
    {
        var dto = new BookingDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "VA-MKT01",
            BookingStatus.Confirmed,
            "Sarah",
            "Connor",
            "sarah@sky.net",
            "555-0100",
            4,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            14000,
            14000,
            "PaidInFull",
            new System.Collections.Generic.List<int> { 1 },
            4,
            EmailMarketingOptIn: false
        );

        Assert.False(dto.EmailMarketingOptIn);

        var dtoDefault = new BookingDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "VA-MKT02",
            BookingStatus.Confirmed,
            "John",
            "Connor",
            "john@sky.net",
            "555-0101",
            2,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            7000,
            7000,
            "PaidInFull",
            new System.Collections.Generic.List<int> { 2 },
            2
        );

        Assert.True(dtoDefault.EmailMarketingOptIn);
    }
}

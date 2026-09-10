using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VenueAxe.Data;
using VenueAxe.Data.Repositories;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Infrastructure.Pdf;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Functional;

public class WaiverAuditFunctionalTests
{
    private static UserContext CreateUserCtx(Guid tenantId, Guid? venueId = null)
    {
        var ctx = new UserContext();
        ctx.SetManualContext(tenantId, Guid.NewGuid(), venueId);
        return ctx;
    }

    private static VenueAxeDbContext CreateDbContext(IUserContext userContext, string dbName)
    {
        var options = new DbContextOptionsBuilder<VenueAxeDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        return new VenueAxeDbContext(options, userContext);
    }

    [Fact]
    public async Task SubmitWaiver_GeneratesAuditTrail_AndLinksToBooking()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var userCtx = CreateUserCtx(tenantId, venueId);

        string templateBody = "I hereby release and hold harmless VenueAxe and all operators from any injury.";
        string expectedHash;
        using (var sha256 = SHA256.Create())
        {
            expectedHash = Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(templateBody)));
        }

        // 1. Seed Venue, WaiverTemplate, and an Active Booking
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var venue = new Venue
            {
                Id = venueId,
                TenantId = tenantId,
                Name = "Timberland Axe Lounge",
                Slug = "timberland",
                AddressLine1 = "77 Pine St",
                City = "Seattle",
                State = "WA",
                PostalCode = "98101"
            };
            db.Venues.Add(venue);

            db.WaiverTemplates.Add(new WaiverTemplate
            {
                Id = templateId,
                TenantId = tenantId,
                VenueId = venueId,
                Title = "General Liability & Minor Participant Release",
                BodyTextMarkdown = templateBody,
                Sha256Hash = expectedHash,
                VersionNumber = 1,
                IsActive = true
            });

            db.Bookings.Add(new Booking
            {
                Id = bookingId,
                TenantId = tenantId,
                VenueId = venueId,
                BookingReference = "VA-778899",
                GuestFirstName = "John",
                GuestLastName = "Doe",
                GuestEmail = "john.doe@example.com",
                GuestPhone = "555-444-3333",
                StartTime = DateTimeOffset.UtcNow.AddHours(2),
                EndTime = DateTimeOffset.UtcNow.AddHours(3),
                PartySize = 4,
                Status = BookingStatus.Confirmed
            });

            await db.SaveChangesAsync();
        }

        // 2. Submit signed waiver via WaiverService
        Guid waiverId;
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var uow = new UnitOfWork(db, userCtx);
            var waiverService = new WaiverService(uow);

            var request = new SubmitWaiverRequest(
                TemplateId: templateId,
                BookingId: bookingId,
                SignerFirstName: "John",
                SignerLastName: "Doe",
                SignerEmail: "john.doe@example.com",
                SignerPhone: "555-444-3333",
                DateOfBirth: new DateOnly(1990, 5, 20),
                IsGuardianSigning: true,
                MinorsCoveredJson: "[{\"fullName\":\"Johnny Doe Jr\",\"dateOfBirth\":\"2015-08-10\"}]",
                SignatureImagePngBase64: "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
                SignatureVectorSvg: null,
                UserAgent: "Mozilla/5.0 TestBrowser",
                BookingReference: "VA-778899"
            );

            var result = await waiverService.SubmitWaiverAsync(request, "192.168.1.100");
            Assert.NotNull(result);
            waiverId = result.Id;

            Assert.Equal("John", result.SignerFirstName);
            Assert.Equal("Doe", result.SignerLastName);
            Assert.Equal(bookingId, result.BookingId);
        }

        // 3. Verify database persistence and audit fields
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var waiver = await db.Waivers.Include(w => w.Venue).Include(w => w.Template).FirstOrDefaultAsync(w => w.Id == waiverId);
            Assert.NotNull(waiver);
            Assert.Equal("192.168.1.100", waiver.IpAddress);
            Assert.Equal("Mozilla/5.0 TestBrowser", waiver.UserAgent);
            Assert.True(waiver.IsGuardianSigning);
            Assert.NotNull(waiver.MinorsCoveredJson);
            Assert.Contains("Johnny Doe Jr", waiver.MinorsCoveredJson);

            // 4. Verify PDF generation creates valid PDF byte stream with %PDF header
            var pdfService = new WaiverPdfService();
            var pdfBytes = pdfService.GeneratePdf(waiver, waiver.Venue!, waiver.Template);

            Assert.NotNull(pdfBytes);
            Assert.True(pdfBytes.Length > 200, "PDF should contain valid binary structure");

            string header = Encoding.ASCII.GetString(pdfBytes, 0, 5);
            Assert.Equal("%PDF-", header);
        }
    }
}

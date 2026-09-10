using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using VenueAxe.Data;
using VenueAxe.Data.Repositories;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Functional;

public class BookingAllocationFunctionalTests
{
    private class FakeSquarePaymentService : ISquarePaymentService
    {
        public Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new SquarePaymentResult(true, "sq_tx_mock", "COMPLETED", "CARD", "PAID", null));
        public Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signatureHeader, string webhookUrl) => Task.FromResult(true);
        public string? GetApplicationId() => "sq0idp-test";
        public string? GetLocationId() => "loc-test";
    }

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
    public async Task CreateBooking_AllocatesContiguousLanes_AndCalculatesPricingAccurately()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var userCtx = CreateUserCtx(tenantId, venueId);

        // 1. Seed Venue, 4 Lanes, and BookingConfig
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var venue = new Venue
            {
                Id = venueId,
                TenantId = tenantId,
                Name = "Apex Axe Arena",
                Slug = "apex-axe",
                AddressLine1 = "100 Timber Way",
                City = "Portland",
                State = "OR",
                PostalCode = "97201",
                BusinessHoursJson = "{\"monday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"tuesday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"wednesday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"thursday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"friday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"saturday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"sunday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"}}"
            };
            db.Venues.Add(venue);

            for (int i = 1; i <= 4; i++)
            {
                db.Lanes.Add(new Lane
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    VenueId = venueId,
                    LaneNumber = i,
                    Name = $"Lane {i:D2}",
                    MaxThrowers = 6,
                    IsActive = true
                });
            }

            db.BookingConfigs.Add(new BookingConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VenueId = venueId,
                BasePriceCents = 3500, // $35/person
                PeakPriceCents = 4000,
                MinPartySize = 1,
                MaxPartySize = 24,
                SlotDurationsMinutes = new[] { 60, 90, 120 },
                DepositType = DepositType.FullPayment
            });

            await db.SaveChangesAsync();
        }

        // 2. Execute BookingService CreateGuestBookingAsync for 12 throwers (requires 2 adjacent lanes)
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var uow = new UnitOfWork(db, userCtx);
            var bookingService = new BookingService(uow, new FakeSquarePaymentService(), NullLogger<BookingService>.Instance);

            var startTime = DateTimeOffset.UtcNow.Date.AddDays(1).AddHours(18); // Tomorrow 6 PM
            var request = new CreateBookingRequest(
                GuestFirstName: "Marcus",
                GuestLastName: "Vance",
                GuestEmail: "marcus@example.com",
                GuestPhone: "555-123-4567",
                StartTime: startTime,
                DurationMinutes: 60,
                PartySize: 12,
                Notes: "Birthday Party"
            );

            var result = await bookingService.CreateGuestBookingAsync("apex-axe", request);

            Assert.NotNull(result);
            Assert.StartsWith("VA-", result.BookingReference);
            Assert.Equal(BookingStatus.Confirmed, result.Status);
            Assert.Equal(2, result.AssignedLaneNumbers.Count);
            // Verify contiguous allocation: e.g. Lane 1 and 2
            Assert.Equal(1, Math.Abs(result.AssignedLaneNumbers[0] - result.AssignedLaneNumbers[1]));

            // Verify financial calculations:
            // 12 throwers * $40.00 peak rate (after 17:00) = $480.00 (48000 cents)
            Assert.Equal(48000, result.TotalAmountCents);
        }
    }

    [Fact]
    public async Task OverlappingBooking_CannotOverallocateOccupiedLanes()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var userCtx = CreateUserCtx(tenantId, venueId);

        // Seed Venue with only 2 lanes (max 6 throwers each)
        using (var db = CreateDbContext(userCtx, dbName))
        {
            db.Venues.Add(new Venue
            {
                Id = venueId,
                TenantId = tenantId,
                Name = "Tiny Axe",
                Slug = "tiny-axe",
                AddressLine1 = "1 Small Rd",
                City = "Bend",
                State = "OR",
                PostalCode = "97701",
                BusinessHoursJson = "{\"monday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"tuesday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"wednesday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"thursday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"friday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"saturday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"sunday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"}}"
            });

            db.Lanes.Add(new Lane
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VenueId = venueId,
                LaneNumber = 1,
                Name = "Lane 01",
                MaxThrowers = 6,
                IsActive = true
            });

            db.Lanes.Add(new Lane
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VenueId = venueId,
                LaneNumber = 2,
                Name = "Lane 02",
                MaxThrowers = 6,
                IsActive = true
            });

            db.BookingConfigs.Add(new BookingConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VenueId = venueId,
                BasePriceCents = 3000,
                MinPartySize = 1,
                MaxPartySize = 12,
                SlotDurationsMinutes = new[] { 60, 90, 120 }
            });

            await db.SaveChangesAsync();
        }

        var slotStart = DateTimeOffset.UtcNow.Date.AddDays(2).AddHours(14); // 2 PM

        // First reservation books both lanes (12 throwers)
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var uow = new UnitOfWork(db, userCtx);
            var bookingService = new BookingService(uow, new FakeSquarePaymentService(), NullLogger<BookingService>.Instance);

            var b1 = await bookingService.CreateGuestBookingAsync("tiny-axe", new CreateBookingRequest(
                GuestFirstName: "Alice",
                GuestLastName: "Cooper",
                GuestEmail: "alice@example.com",
                GuestPhone: "555-001-0002",
                StartTime: slotStart,
                DurationMinutes: 60,
                PartySize: 12
            ));

            Assert.NotNull(b1);
            Assert.Equal(2, b1.AssignedLaneNumbers.Count);
        }

        // Second reservation attempts to book overlapping slot (e.g. 2:00 PM to 3:00 PM) -> should fail contiguous lane allocation
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var uow = new UnitOfWork(db, userCtx);
            var bookingService = new BookingService(uow, new FakeSquarePaymentService(), NullLogger<BookingService>.Instance);

            var b2 = await bookingService.CreateGuestBookingAsync("tiny-axe", new CreateBookingRequest(
                GuestFirstName: "Bob",
                GuestLastName: "Marley",
                GuestEmail: "bob@example.com",
                GuestPhone: "555-001-0003",
                StartTime: slotStart,
                DurationMinutes: 60,
                PartySize: 4
            ));

            Assert.Null(b2);
        }
    }

    [Fact]
    public async Task CancelBooking_RestoresLaneAvailability()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var userCtx = CreateUserCtx(tenantId, venueId);

        using (var db = CreateDbContext(userCtx, dbName))
        {
            db.Venues.Add(new Venue
            {
                Id = venueId,
                TenantId = tenantId,
                Name = "Target Hub",
                Slug = "target-hub",
                AddressLine1 = "500 Elm",
                City = "Dallas",
                State = "TX",
                PostalCode = "75001",
                BusinessHoursJson = "{\"monday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"tuesday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"wednesday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"thursday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"friday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"saturday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"},\"sunday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"}}"
            });

            db.Lanes.Add(new Lane
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VenueId = venueId,
                LaneNumber = 1,
                Name = "Lane 01",
                MaxThrowers = 6,
                IsActive = true
            });

            db.BookingConfigs.Add(new BookingConfig
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                VenueId = venueId,
                BasePriceCents = 2500,
                MinPartySize = 1,
                MaxPartySize = 6,
                SlotDurationsMinutes = new[] { 60, 90, 120 }
            });

            await db.SaveChangesAsync();
        }

        var slotTime = DateTimeOffset.UtcNow.Date.AddDays(3).AddHours(16);
        Guid bookingId;

        using (var db = CreateDbContext(userCtx, dbName))
        {
            var uow = new UnitOfWork(db, userCtx);
            var service = new BookingService(uow, new FakeSquarePaymentService(), NullLogger<BookingService>.Instance);

            var b = await service.CreateGuestBookingAsync("target-hub", new CreateBookingRequest(
                GuestFirstName: "Charlie",
                GuestLastName: "Brown",
                GuestEmail: "charlie@peanuts.com",
                GuestPhone: "555-888-9999",
                StartTime: slotTime,
                DurationMinutes: 60,
                PartySize: 4
            ));

            Assert.NotNull(b);
            bookingId = b.Id;
        }

        // Cancel the booking
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var uow = new UnitOfWork(db, userCtx);
            var service = new BookingService(uow, new FakeSquarePaymentService(), NullLogger<BookingService>.Instance);

            var cancelled = await service.UpdateBookingStatusAsync(bookingId, BookingStatus.Cancelled);
            Assert.True(cancelled);

            var booking = await uow.Bookings.GetByIdAsync(bookingId);
            Assert.NotNull(booking);
            Assert.Equal(BookingStatus.Cancelled, booking.Status);
        }

        // Now verify another party can book the exact same slot!
        using (var db = CreateDbContext(userCtx, dbName))
        {
            var uow = new UnitOfWork(db, userCtx);
            var service = new BookingService(uow, new FakeSquarePaymentService(), NullLogger<BookingService>.Instance);

            var newBooking = await service.CreateGuestBookingAsync("target-hub", new CreateBookingRequest(
                GuestFirstName: "Lucy",
                GuestLastName: "Van Pelt",
                GuestEmail: "lucy@peanuts.com",
                GuestPhone: "555-888-0000",
                StartTime: slotTime,
                DurationMinutes: 60,
                PartySize: 4
            ));

            Assert.NotNull(newBooking);
            Assert.Equal(BookingStatus.Confirmed, newBooking.Status);
            Assert.Contains(1, newBooking.AssignedLaneNumbers);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Repositories;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class AdminBookingCreationTests
{
    private class FakeBookingUnitOfWork : IUnitOfWork
    {
        public Venue SeededVenue { get; set; } = null!;
        public BookingConfig SeededConfig { get; set; } = null!;
        public List<Lane> SeededLanes { get; set; } = new();
        public List<Booking> StoredBookings { get; set; } = new();

        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public IVenueRepository Venues => new FakeVenueRepo(SeededVenue);
        public IUserRepository Users => throw new NotImplementedException();
        public ILaneRepository Lanes => new FakeLaneRepo(SeededLanes);
        public IBookingRepository Bookings => new FakeBookingRepo(StoredBookings);
        public IBookingConfigRepository BookingConfigs => new FakeBookingConfigRepo(SeededConfig);
        public IWaiverRepository Waivers => throw new NotImplementedException();
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    private class FakeVenueRepo : IVenueRepository
    {
        private readonly Venue _venue;
        public FakeVenueRepo(Venue venue) => _venue = venue;

        public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(id == _venue.Id ? _venue : null);
        public Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(slug.Equals(_venue.Slug, StringComparison.OrdinalIgnoreCase) ? _venue : null);
        public Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(slug.Equals(_venue.Slug, StringComparison.OrdinalIgnoreCase) ? _venue : null);
        public Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Venue>> FindAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Venue> AddAsync(Venue entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Venue entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Venue>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Venue>> FindInTenantAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Venue?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue);
    }

    private class FakeLaneRepo : ILaneRepository
    {
        private readonly List<Lane> _lanes;
        public FakeLaneRepo(List<Lane> lanes) => _lanes = lanes;

        public Task<Lane?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == id));
        public Task<Lane?> GetByIdIgnoreQueryFiltersAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == id));
        public Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Lane>>(_lanes.Where(l => l.VenueId == venueId && (includeInactive || l.IsActive)).ToList());
        public Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Lane?> GetWithActiveSessionAsync(Guid laneId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Lane>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes.AsQueryable().Where(predicate).ToList());
        public Task<Lane> AddAsync(Lane entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(Lane entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Lane entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Lane>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindInTenantAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Lane?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == id));
    }

    private class FakeBookingRepo : IBookingRepository
    {
        private readonly List<Booking> _bookings;
        public FakeBookingRepo(List<Booking> bookings) => _bookings = bookings;

        public Task<Booking?> GetByIdWithLanesAsync(Guid bookingId, CancellationToken cancellationToken = default)
            => Task.FromResult(_bookings.FirstOrDefault(b => b.Id == bookingId));

        public Task<Booking?> GetByReferenceAsync(string referenceCode, CancellationToken cancellationToken = default)
            => Task.FromResult(_bookings.FirstOrDefault(b => b.BookingReference.Equals(referenceCode, StringComparison.OrdinalIgnoreCase)));
        public Task<IReadOnlyList<Booking>> GetByVenueAndDateRangeAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Booking>>(_bookings.Where(b => b.VenueId == venueId && b.StartTime < end && b.EndTime > start).ToList());
        public Task<int> CountOverlappingBookingsAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
            => Task.FromResult(_bookings.Count(b => b.VenueId == venueId && b.StartTime < end && b.EndTime > start));
        public Task<IReadOnlyList<Booking>> GetOverlappingBookingsWithLanesAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Booking>>(_bookings.Where(b => b.VenueId == venueId && b.StartTime < end && b.EndTime > start).ToList());
        public Task<IReadOnlyList<Booking>> GetUpcomingBookingsByLaneAsync(Guid laneId, DateTimeOffset fromTime, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Booking>>(_bookings.Where(b => b.EndTime >= fromTime && b.BookingLanes.Any(bl => bl.LaneId == laneId)).ToList());
        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_bookings.FirstOrDefault(b => b.Id == id));
        public Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings);
        public Task<IReadOnlyList<Booking>> FindAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings.AsQueryable().Where(predicate).ToList());
        public Task<Booking> AddAsync(Booking entity, CancellationToken cancellationToken = default) { _bookings.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Booking entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Booking entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Booking>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings);
        public Task<IReadOnlyList<Booking>> FindInTenantAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings.AsQueryable().Where(predicate).ToList());
        public Task<Booking?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_bookings.FirstOrDefault(b => b.Id == id));
    }

    private class FakeBookingConfigRepo : IBookingConfigRepository
    {
        private readonly BookingConfig _config;
        public FakeBookingConfigRepo(BookingConfig config) => _config = config;

        public Task<BookingConfig?> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
            => Task.FromResult(venueId == _config.VenueId ? _config : null);
        public Task<BookingConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<BookingConfig?>(_config);
        public Task<IReadOnlyList<BookingConfig>> GetAllAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<BookingConfig>> FindAsync(Expression<Func<BookingConfig, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<BookingConfig> AddAsync(BookingConfig entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(BookingConfig entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(BookingConfig entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<BookingConfig>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(_config != null ? new List<BookingConfig> { _config } : new List<BookingConfig>());
        public Task<IReadOnlyList<BookingConfig>> FindInTenantAsync(Expression<Func<BookingConfig, bool>> predicate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<BookingConfig?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<BookingConfig?>(_config);
    }

    private static (FakeBookingUnitOfWork, BookingService) CreateTestHarness()
    {
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();

        var venue = new Venue
        {
            Id = venueId,
            TenantId = tenantId,
            Name = "Downtown Arena",
            Slug = "downtown",
            Currency = "USD",
            BusinessHoursJson = "{\"monday\":{\"isOpen\":true,\"open\":\"10:00\",\"close\":\"23:00\"}}"
        };

        var config = new BookingConfig
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VenueId = venueId,
            MinPartySize = 1,
            MaxPartySize = 24,
            SlotDurationsMinutes = new[] { 60, 90, 120 },
            PricingModel = PricingModel.PerPerson,
            BasePriceCents = 2500, // $25/person
            PeakPriceCents = 3000,
            DepositType = DepositType.FullPayment,
            DepositAmountCents = 2500
        };

        var lanes = new List<Lane>
        {
            new Lane { Id = Guid.NewGuid(), TenantId = tenantId, VenueId = venueId, LaneNumber = 1, Name = "Lane 01", MaxThrowers = 6, IsActive = true },
            new Lane { Id = Guid.NewGuid(), TenantId = tenantId, VenueId = venueId, LaneNumber = 2, Name = "Lane 02", MaxThrowers = 6, IsActive = true },
            new Lane { Id = Guid.NewGuid(), TenantId = tenantId, VenueId = venueId, LaneNumber = 3, Name = "Lane 03", MaxThrowers = 6, IsActive = true }
        };

        var uow = new FakeBookingUnitOfWork
        {
            SeededVenue = venue,
            SeededConfig = config,
            SeededLanes = lanes
        };

        var service = new BookingService(uow, new FakeSquarePaymentService(), NullLogger<BookingService>.Instance);
        return (uow, service);
    }

    private class FakeSquarePaymentService : ISquarePaymentService
    {
        public Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new SquarePaymentResult(true, "sq-test-pay-id", "sq-test-order-id", null, "COMPLETED", null));

        public Task<bool> VerifyWebhookSignatureAsync(string payload, string signatureHeader, string signatureKey)
            => Task.FromResult(true);

        public string GetApplicationId() => "sandbox-test-app-id";
        public string GetLocationId() => "sandbox-test-loc-id";
    }

    [Fact]
    public async Task CreateAdminBooking_WalkInWithSpecificLane_SucceedsAndSetsPaidInFull()
    {
        var (uow, service) = CreateTestHarness();

        var request = new CreateAdminBookingRequest(
            VenueId: uow.SeededVenue.Id,
            GuestFirstName: "Dave",
            GuestLastName: "Miller",
            GuestEmail: "dave.miller@example.com",
            GuestPhone: "555-4321",
            PartySize: 4,
            StartTime: DateTimeOffset.UtcNow.Date.AddHours(14),
            DurationMinutes: 60,
            SpecificLaneNumbers: new List<int> { 2 },
            PaymentMethod: "Cash",
            PaymentStatus: "PaidInFull",
            Notes: "Walk-in cash group",
            AutoCheckIn: false
        );

        var result = await service.CreateAdminBookingAsync(request);

        Assert.NotNull(result);
        Assert.StartsWith("WA-", result.BookingReference);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.Equal("Dave", result.GuestFirstName);
        Assert.Equal(4, result.PartySize);
        Assert.Single(result.AssignedLaneNumbers);
        Assert.Equal(2, result.AssignedLaneNumbers[0]);
        Assert.Equal("PaidInFull", result.PaymentStatus);
        Assert.Equal(10000, result.TotalAmountCents); // 4 * $25 = $100.00
        Assert.Equal(10000, result.PaidAmountCents);
    }

    [Fact]
    public async Task CreateAdminBooking_WithAutoCheckIn_SetsStatusToCheckedIn()
    {
        var (uow, service) = CreateTestHarness();

        var request = new CreateAdminBookingRequest(
            VenueId: uow.SeededVenue.Id,
            GuestFirstName: "Sarah",
            GuestLastName: "Connor",
            PartySize: 2,
            StartTime: DateTimeOffset.UtcNow.Date.AddHours(15),
            DurationMinutes: 60,
            SpecificLaneNumbers: new List<int> { 1 },
            PaymentMethod: "PosTerminal",
            AutoCheckIn: true
        );

        var result = await service.CreateAdminBookingAsync(request);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.CheckedIn, result.Status);
    }

    [Fact]
    public async Task CreateAdminBooking_CompBooking_HasZeroTotalAndPaidInFull()
    {
        var (uow, service) = CreateTestHarness();

        var request = new CreateAdminBookingRequest(
            VenueId: uow.SeededVenue.Id,
            GuestFirstName: "VIP",
            GuestLastName: "Guest",
            PartySize: 6,
            StartTime: DateTimeOffset.UtcNow.Date.AddHours(16),
            DurationMinutes: 60,
            PaymentMethod: "Comp",
            PaymentStatus: "PaidInFull"
        );

        var result = await service.CreateAdminBookingAsync(request);

        Assert.NotNull(result);
        Assert.Equal(0, result.TotalAmountCents);
        Assert.Equal(0, result.PaidAmountCents);
        Assert.Equal("PaidInFull", result.PaymentStatus);
    }

    [Fact]
    public async Task CreateAdminBooking_SpecificLaneConflict_ReturnsNull()
    {
        var (uow, service) = CreateTestHarness();

        var start = DateTimeOffset.UtcNow.Date.AddHours(18);
        var end = start.AddHours(1);

        // Pre-existing booking on Lane 1
        var existingBooking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = uow.SeededVenue.Id,
            StartTime = start,
            EndTime = end,
            BookingReference = "VA-99999",
            Status = BookingStatus.Confirmed
        };
        existingBooking.BookingLanes.Add(new BookingLane
        {
            BookingId = existingBooking.Id,
            LaneId = uow.SeededLanes[0].Id
        });
        uow.StoredBookings.Add(existingBooking);

        // Attempting to book Lane 1 during overlapping time
        var conflictingRequest = new CreateAdminBookingRequest(
            VenueId: uow.SeededVenue.Id,
            GuestFirstName: "Late",
            GuestLastName: "Thrower",
            PartySize: 2,
            StartTime: start.AddMinutes(15),
            DurationMinutes: 60,
            SpecificLaneNumbers: new List<int> { 1 }
        );

        var result = await service.CreateAdminBookingAsync(conflictingRequest);

        Assert.Null(result); // Rejected due to conflict
    }

    [Fact]
    public async Task CreateAdminBooking_AutoAllocateContiguous_AllocatesFreeLanes()
    {
        var (uow, service) = CreateTestHarness();

        // 12 throwers on 6-person lanes requires 2 contiguous lanes (Lanes 1 & 2)
        var request = new CreateAdminBookingRequest(
            VenueId: uow.SeededVenue.Id,
            GuestFirstName: "Corporate",
            GuestLastName: "Outing",
            PartySize: 12,
            StartTime: DateTimeOffset.UtcNow.Date.AddHours(19),
            DurationMinutes: 60
        );

        var result = await service.CreateAdminBookingAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.AssignedLaneNumbers.Count);
        Assert.Contains(1, result.AssignedLaneNumbers);
        Assert.Contains(2, result.AssignedLaneNumbers);
    }

    [Fact]
    public async Task CreateAdminBooking_DeactivatedLaneSpecified_RejectsBooking()
    {
        var (uow, service) = CreateTestHarness();

        // Deactivate Lane 2
        var lane2 = uow.SeededLanes.First(l => l.LaneNumber == 2);
        lane2.IsActive = false;

        var request = new CreateAdminBookingRequest(
            VenueId: uow.SeededVenue.Id,
            GuestFirstName: "Test",
            GuestLastName: "Guest",
            PartySize: 2,
            StartTime: DateTimeOffset.UtcNow.Date.AddHours(14),
            DurationMinutes: 60,
            SpecificLaneNumbers: new List<int> { 2 }
        );

        var result = await service.CreateAdminBookingAsync(request);

        Assert.Null(result); // Rejected because Lane 2 is deactivated
    }

    [Fact]
    public async Task CreateAdminBooking_AutoAllocateContiguous_SkipsDeactivatedLane()
    {
        var (uow, service) = CreateTestHarness();

        // Add Lane 4 so that (3, 4) can form a valid contiguous pair when Lane 2 is inactive
        uow.SeededLanes.Add(new Lane
        {
            Id = Guid.NewGuid(),
            TenantId = uow.SeededVenue.TenantId,
            VenueId = uow.SeededVenue.Id,
            LaneNumber = 4,
            Name = "Lane 04",
            MaxThrowers = 6,
            IsActive = true
        });

        // Deactivate Lane 2, leaving Lane 1, Lane 3, Lane 4 active
        var lane2 = uow.SeededLanes.First(l => l.LaneNumber == 2);
        lane2.IsActive = false;

        // 12 throwers requires 2 contiguous lanes.
        // Lanes 1 & 2 is invalid because Lane 2 is deactivated.
        // Lanes 2 & 3 is invalid because Lane 2 is deactivated.
        // Lanes 3 & 4 must be allocated.
        var request = new CreateAdminBookingRequest(
            VenueId: uow.SeededVenue.Id,
            GuestFirstName: "Corporate",
            GuestLastName: "Outing",
            PartySize: 12,
            StartTime: DateTimeOffset.UtcNow.Date.AddHours(19),
            DurationMinutes: 60
        );

        var result = await service.CreateAdminBookingAsync(request);

        Assert.NotNull(result);
        Assert.Equal(2, result.AssignedLaneNumbers.Count);
        Assert.Contains(3, result.AssignedLaneNumbers);
        Assert.Contains(4, result.AssignedLaneNumbers);
        Assert.DoesNotContain(2, result.AssignedLaneNumbers);
    }
}


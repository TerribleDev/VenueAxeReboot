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

public class OperatingHoursAndScheduleTests
{
    private class FakeSquareService : ISquarePaymentService
    {
        public Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new SquarePaymentResult(true, "sq_test", "sq_ord", null, "COMPLETED"));

        public Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signatureHeader, string webhookUrl)
            => Task.FromResult(true);
    }

    private class FakeUnitOfWork : IUnitOfWork
    {
        public Venue SeededVenue { get; set; } = null!;
        public List<Lane> SeededLanes { get; set; } = new();

        public IVenueRepository Venues => new FakeVenueRepo(SeededVenue);
        public ILaneRepository Lanes => new FakeLaneRepo(SeededLanes);
        public IBookingRepository Bookings => new FakeBookingRepo();
        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public IUserRepository Users => throw new NotImplementedException();
        public IBookingConfigRepository BookingConfigs => throw new NotImplementedException();
        public IWaiverRepository Waivers => throw new NotImplementedException();
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class FakeVenueRepo : IVenueRepository
    {
        private readonly Venue _venue;
        public FakeVenueRepo(Venue v) => _venue = v;
        public Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue.Slug == slug ? _venue : null);
        public Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue.Slug == slug ? _venue : null);
        public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue.Id == id ? _venue : null);
        public Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue);
        public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new[] { _venue });
        public Task<IReadOnlyList<Venue>> FindAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new[] { _venue });
        public Task<Venue> AddAsync(Venue entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task UpdateAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Venue>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new[] { _venue });
        public Task<IReadOnlyList<Venue>> FindInTenantAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new[] { _venue });
        public Task<Venue?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue);
    }

    private class FakeLaneRepo : ILaneRepository
    {
        private readonly List<Lane> _lanes;
        public FakeLaneRepo(List<Lane> lanes) => _lanes = lanes;
        public Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, CancellationToken cancellationToken = default) => Task.FromResult<Lane?>(null);
        public Task<Lane?> GetWithActiveSessionAsync(Guid laneId, CancellationToken cancellationToken = default) => Task.FromResult<Lane?>(null);
        public Task<Lane?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == id));
        public Task<IReadOnlyList<Lane>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<Lane> AddAsync(Lane entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task UpdateAsync(Lane entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Lane entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Lane>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindInTenantAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<Lane?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == id));
    }

    private class FakeBookingRepo : IBookingRepository
    {
        public Task<Booking?> GetByReferenceAsync(string referenceCode, CancellationToken cancellationToken = default) => Task.FromResult<Booking?>(null);
        public Task<IReadOnlyList<Booking>> GetByVenueAndDateRangeAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(new List<Booking>());
        public Task<int> CountOverlappingBookingsAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<IReadOnlyList<Booking>> GetOverlappingBookingsWithLanesAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(new List<Booking>());
        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Booking?>(null);
        public Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(new List<Booking>());
        public Task<IReadOnlyList<Booking>> FindAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(new List<Booking>());
        public Task<Booking> AddAsync(Booking entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task UpdateAsync(Booking entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Booking entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Booking>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(new List<Booking>());
        public Task<IReadOnlyList<Booking>> FindInTenantAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(new List<Booking>());
        public Task<Booking?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Booking?>(null);
    }

    [Fact]
    public async Task CheckAvailability_ClosedDay_StandardBookingDenied_CorporateOverrideAllowed()
    {
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();

        // Monday is closed: isOpen: false
        var businessHours = "{\"monday\":{\"isOpen\":false,\"open\":\"12:00\",\"close\":\"22:00\"},\"tuesday\":{\"isOpen\":true,\"open\":\"12:00\",\"close\":\"22:00\"}}";
        
        var bookingTypesJson = @"[
            { ""id"": ""standard"", ""name"": ""Standard Throw"", ""allowAfterHoursBooking"": false, ""allowOffDaysBooking"": false },
            { ""id"": ""corporate"", ""name"": ""Corporate Buyout"", ""allowAfterHoursBooking"": true, ""allowOffDaysBooking"": true }
        ]";

        var config = new BookingConfig
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VenueId = venueId,
            BookingTypesJson = bookingTypesJson,
            MinPartySize = 1,
            MaxPartySize = 20,
            BasePriceCents = 3500
        };

        var venue = new Venue
        {
            Id = venueId,
            TenantId = tenantId,
            Name = "Downtown Arena",
            Slug = "downtown",
            BusinessHoursJson = businessHours,
            BookingConfig = config
        };

        var lane1 = new Lane
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VenueId = venueId,
            LaneNumber = 1,
            Name = "Lane 1",
            MaxThrowers = 6,
            CurrentStatus = LaneStatus.Available
        };

        var uow = new FakeUnitOfWork
        {
            SeededVenue = venue,
            SeededLanes = new List<Lane> { lane1 }
        };

        var service = new BookingService(uow, new FakeSquareService(), NullLogger<BookingService>.Instance);

        // Monday Date
        var monday = new DateOnly(2026, 9, 7); // Monday
        Assert.Equal(DayOfWeek.Monday, monday.DayOfWeek);

        // Standard booking on Monday (Closed day) -> Should return empty slots
        var standardSlots = await service.CheckAvailabilityAsync(
            "downtown",
            new AvailabilityQuery(monday, 4, 60, "standard"));

        Assert.Empty(standardSlots);

        // Corporate buyout on Monday -> Should allow slots because allowOffDaysBooking is true
        var corporateSlots = await service.CheckAvailabilityAsync(
            "downtown",
            new AvailabilityQuery(monday, 4, 60, "corporate"));

        Assert.NotEmpty(corporateSlots);
        Assert.All(corporateSlots, s => Assert.True(s.IsAvailable));
    }

    [Fact]
    public async Task CheckAvailability_StandardHours_AfterHoursOverride_ExpandsTimeWindow()
    {
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();

        // Tuesday open from 14:00 to 20:00 (2pm - 8pm)
        var businessHours = "{\"tuesday\":{\"isOpen\":true,\"open\":\"14:00\",\"close\":\"20:00\"}}";
        
        var bookingTypesJson = @"[
            { ""id"": ""standard"", ""name"": ""Standard"", ""allowAfterHoursBooking"": false },
            { ""id"": ""vip_late"", ""name"": ""VIP Late Night"", ""allowAfterHoursBooking"": true }
        ]";

        var config = new BookingConfig
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VenueId = venueId,
            BookingTypesJson = bookingTypesJson,
            MinPartySize = 1,
            MaxPartySize = 20,
            BasePriceCents = 3500
        };

        var venue = new Venue
        {
            Id = venueId,
            TenantId = tenantId,
            Name = "Downtown Arena",
            Slug = "downtown",
            BusinessHoursJson = businessHours,
            BookingConfig = config
        };

        var lane = new Lane
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VenueId = venueId,
            LaneNumber = 1,
            Name = "Lane 1",
            MaxThrowers = 6,
            CurrentStatus = LaneStatus.Available
        };

        var uow = new FakeUnitOfWork
        {
            SeededVenue = venue,
            SeededLanes = new List<Lane> { lane }
        };

        var service = new BookingService(uow, new FakeSquareService(), NullLogger<BookingService>.Instance);

        var tuesday = new DateOnly(2026, 9, 8); // Tuesday

        var standardSlots = await service.CheckAvailabilityAsync(
            "downtown",
            new AvailabilityQuery(tuesday, 4, 60, "standard"));

        var vipSlots = await service.CheckAvailabilityAsync(
            "downtown",
            new AvailabilityQuery(tuesday, 4, 60, "vip_late"));

        // VIP Late Night has expanded hours (10:00 to 02:00 next day) -> More slots than standard (14:00 to 20:00)
        Assert.True(vipSlots.Count > standardSlots.Count);
        Assert.Contains(vipSlots, s => s.StartTime.Hour < 14 || s.StartTime.Hour >= 20);
        Assert.DoesNotContain(standardSlots, s => s.StartTime.Hour < 14 || s.StartTime.Hour >= 20);
    }
}

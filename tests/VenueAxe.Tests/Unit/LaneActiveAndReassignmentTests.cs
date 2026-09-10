using System.Linq.Expressions;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Repositories;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class LaneActiveAndReassignmentTests
{
    private class FakeLaneRepo : ILaneRepository
    {
        private readonly List<Lane> _lanes;
        public FakeLaneRepo(List<Lane> lanes) => _lanes = lanes;

        public Task<Lane?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == id));

        public Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Lane>>(_lanes.Where(l => l.VenueId == venueId && (includeInactive || l.IsActive)).ToList());

        public Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, CancellationToken cancellationToken = default) => Task.FromResult<Lane?>(null);
        public Task<Lane?> GetWithActiveSessionAsync(Guid laneId, CancellationToken cancellationToken = default) => Task.FromResult<Lane?>(null);
        public Task<Lane?> GetByIdIgnoreQueryFiltersAsync(Guid laneId, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == laneId));
        public Task<IReadOnlyList<Lane>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes.AsQueryable().Where(predicate).ToList());
        public Task<Lane> AddAsync(Lane entity, CancellationToken cancellationToken = default) { _lanes.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Lane entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Lane entity, CancellationToken cancellationToken = default) { _lanes.Remove(entity); return Task.CompletedTask; }
        public Task<IReadOnlyList<Lane>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindInTenantAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes.AsQueryable().Where(predicate).ToList());
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
            => Task.FromResult<IReadOnlyList<Booking>>(_bookings.Where(b => b.VenueId == venueId && b.Status != BookingStatus.Cancelled && b.StartTime < end && b.EndTime > start).ToList());

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
        private readonly BookingConfig? _config;
        public FakeBookingConfigRepo(BookingConfig? config) => _config = config;
        public Task<BookingConfig?> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult(_config);
        public Task<BookingConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_config);
        public Task<IReadOnlyList<BookingConfig>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(_config != null ? new[] { _config } : Array.Empty<BookingConfig>());
        public Task<IReadOnlyList<BookingConfig>> FindAsync(Expression<Func<BookingConfig, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(_config != null ? new[] { _config } : Array.Empty<BookingConfig>());
        public Task<BookingConfig> AddAsync(BookingConfig entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task UpdateAsync(BookingConfig entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(BookingConfig entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<BookingConfig>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(_config != null ? new[] { _config } : Array.Empty<BookingConfig>());
        public Task<IReadOnlyList<BookingConfig>> FindInTenantAsync(Expression<Func<BookingConfig, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(_config != null ? new[] { _config } : Array.Empty<BookingConfig>());
        public Task<BookingConfig?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_config);
    }

    private class FakeVenueRepo : IVenueRepository
    {
        private readonly Venue? _venue;
        public FakeVenueRepo(Venue? venue = null) => _venue = venue;
        public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) 
            => Task.FromResult<Venue?>(_venue ?? new Venue { Id = id, Timezone = "America/New_York" });
        public Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(null);
        public Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(null);
        public Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(null);
        public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new List<Venue>());
        public Task<IReadOnlyList<Venue>> FindAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new List<Venue>());
        public Task<Venue> AddAsync(Venue entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task UpdateAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Venue>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new List<Venue>());
        public Task<IReadOnlyList<Venue>> FindInTenantAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new List<Venue>());
        public Task<Venue?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_venue);
    }

    private class FakeUow : IUnitOfWork
    {
        public FakeUow(List<Lane> lanes, List<Booking> bookings, BookingConfig? config = null)
        {
            Lanes = new FakeLaneRepo(lanes);
            Bookings = new FakeBookingRepo(bookings);
            BookingConfigs = new FakeBookingConfigRepo(config);
        }

        public IVenueRepository Venues { get; } = new FakeVenueRepo();
        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public IUserRepository Users => throw new NotImplementedException();
        public ILaneRepository Lanes { get; }
        public IBookingRepository Bookings { get; }
        public IBookingConfigRepository BookingConfigs { get; }
        public IWaiverRepository Waivers => throw new NotImplementedException();
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    [Fact]
    public async Task ToggleLaneActiveAsync_TogglesLaneStatusAndReturnsUpdatedState()
    {
        var laneId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var lane = new Lane
        {
            Id = laneId,
            VenueId = venueId,
            LaneNumber = 1,
            Name = "Lane 01",
            IsActive = true
        };

        var uow = new FakeUow(new List<Lane> { lane }, new List<Booking>());
        var laneService = new LaneService(uow);

        // Toggle to false
        var updated = await laneService.ToggleLaneActiveAsync(laneId);
        Assert.NotNull(updated);
        Assert.False(updated.IsActive);
        Assert.False(lane.IsActive);

        // Toggle back to true
        var updated2 = await laneService.ToggleLaneActiveAsync(laneId, true);
        Assert.NotNull(updated2);
        Assert.True(updated2.IsActive);
        Assert.True(lane.IsActive);
    }

    [Fact]
    public async Task GetUpcomingBookingsForLaneAsync_ReturnsFutureBookings()
    {
        var laneId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var futureBooking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "WA-FUTURE",
            GuestFirstName = "Alice",
            GuestLastName = "Smith",
            StartTime = now.AddHours(2),
            EndTime = now.AddHours(3),
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane> { new() { LaneId = laneId } }
        };

        var uow = new FakeUow(new List<Lane>(), new List<Booking> { futureBooking });
        var laneService = new LaneService(uow);

        var upcoming = await laneService.GetUpcomingBookingsForLaneAsync(laneId);
        Assert.Single(upcoming);
        Assert.Equal("WA-FUTURE", upcoming[0].BookingReference);
    }

    [Fact]
    public async Task GetAvailableLanesForTimeslotAsync_IdentifiesActiveAndReservedLanes()
    {
        var venueId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var lane1 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 1, Name = "Lane 01", IsActive = true };
        var lane2 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 2, Name = "Lane 02", IsActive = false }; // Deactivated
        var lane3 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 3, Name = "Lane 03", IsActive = true };

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "WA-BUSY",
            StartTime = now.AddMinutes(30),
            EndTime = now.AddMinutes(90),
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id } }
        };

        var cfg = new BookingConfig { VenueId = venueId, TurnaroundBufferMinutes = 0 };
        var uow = new FakeUow(new List<Lane> { lane1, lane2, lane3 }, new List<Booking> { booking }, cfg);
        var laneService = new LaneService(uow);

        var slots = await laneService.GetAvailableLanesForTimeslotAsync(venueId, now.AddMinutes(30), 60);

        Assert.Equal(3, slots.Count);

        var slot1 = slots.First(s => s.LaneNumber == 1);
        Assert.False(slot1.IsAvailable);
        Assert.Equal("Already reserved", slot1.ConflictReason);

        var slot2 = slots.First(s => s.LaneNumber == 2);
        Assert.False(slot2.IsAvailable);
        Assert.Equal("Lane is deactivated", slot2.ConflictReason);

        var slot3 = slots.First(s => s.LaneNumber == 3);
        Assert.True(slot3.IsAvailable);
        Assert.Null(slot3.ConflictReason);
    }

    [Fact]
    public async Task ReassignBookingLaneAsync_ReassignsWhenNoConflict_ThrowsOnConflict()
    {
        var venueId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var lane1 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 1, Name = "Lane 01", IsActive = true };
        var lane2 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 2, Name = "Lane 02", IsActive = true };

        var bookingA = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "WA-AAA",
            GuestFirstName = "Bob",
            GuestLastName = "Jones",
            StartTime = now.AddHours(1),
            EndTime = now.AddHours(2),
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id, Lane = lane1 } }
        };

        var bookingB = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "WA-BBB",
            GuestFirstName = "Charlie",
            GuestLastName = "Brown",
            StartTime = now.AddHours(1),
            EndTime = now.AddHours(2),
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane2.Id, Lane = lane2 } }
        };

        var cfg = new BookingConfig { VenueId = venueId, TurnaroundBufferMinutes = 0 };
        var uow = new FakeUow(new List<Lane> { lane1, lane2 }, new List<Booking> { bookingA, bookingB }, cfg);
        var bookingService = new BookingService(uow, null!, null!);

        // Reassigning bookingA to lane2 should fail because bookingB occupies lane2
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bookingService.ReassignBookingLaneAsync(bookingA.Id, lane2.Id)
        );

        // Remove bookingB conflict
        bookingB.Status = BookingStatus.Cancelled;

        // Now reassignment succeeds
        var reassigned = await bookingService.ReassignBookingLaneAsync(bookingA.Id, lane2.Id);
        Assert.NotNull(reassigned);
        Assert.Contains(2, reassigned.AssignedLaneNumbers);
    }

    [Fact]
    public async Task ReassignBookingLaneAsync_ToDeactivatedLane_ThrowsInvalidOperationException()
    {
        var venueId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var lane1 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 1, Name = "Lane 01", IsActive = true };
        var lane2 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 2, Name = "Lane 02", IsActive = false }; // Deactivated

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "WA-ACTIVE",
            GuestFirstName = "Alice",
            GuestLastName = "Smith",
            StartTime = now.AddHours(1),
            EndTime = now.AddHours(2),
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id, Lane = lane1 } }
        };

        var cfg = new BookingConfig { VenueId = venueId, TurnaroundBufferMinutes = 0 };
        var uow = new FakeUow(new List<Lane> { lane1, lane2 }, new List<Booking> { booking }, cfg);
        var bookingService = new BookingService(uow, null!, null!);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            bookingService.ReassignBookingLaneAsync(booking.Id, lane2.Id)
        );

        Assert.Contains("deactivated", ex.Message);
    }

    [Fact]
    public async Task GetLanesForVenueAsync_PopulatesNextBookingTodayWhenFutureBookingScheduledToday()
    {
        var venueId = Guid.NewGuid();
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var nowInTz = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var startOfTodayUtc = new DateTimeOffset(nowInTz.Year, nowInTz.Month, nowInTz.Day, 0, 0, 0, nowInTz.Offset).ToUniversalTime();
        var endOfTodayUtc = startOfTodayUtc.AddDays(1);
        var nowUtc = DateTimeOffset.UtcNow;

        var futureStart = nowUtc + (endOfTodayUtc - nowUtc) / 2;
        var futureEnd = futureStart.AddMinutes(30);

        var pastStart = startOfTodayUtc + (nowUtc - startOfTodayUtc) / 4;
        var pastEnd = startOfTodayUtc + (nowUtc - startOfTodayUtc) / 2;

        var lane1 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 1, Name = "Lane 01", IsActive = true };
        var lane2 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 2, Name = "Lane 02", IsActive = true };

        // Booking 1: Today, in the future (between now and end of today) on Lane 1
        var futureBookingToday = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "VA-NEXT1",
            GuestFirstName = "Sarah",
            GuestLastName = "Connor",
            StartTime = futureStart,
            EndTime = futureEnd,
            PartySize = 4,
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id, Lane = lane1 } }
        };

        // Booking 2: Today, in the past on Lane 1
        var pastBookingToday = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "VA-PAST1",
            GuestFirstName = "John",
            GuestLastName = "Doe",
            StartTime = pastStart,
            EndTime = pastEnd,
            PartySize = 2,
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id, Lane = lane1 } }
        };

        // Booking 3: Tomorrow on Lane 1
        var tomorrowBooking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "VA-TOMORROW",
            GuestFirstName = "Kyle",
            GuestLastName = "Reese",
            StartTime = endOfTodayUtc.AddHours(2),
            EndTime = endOfTodayUtc.AddHours(3),
            PartySize = 6,
            Status = BookingStatus.Confirmed,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id, Lane = lane1 } }
        };

        var uow = new FakeUow(new List<Lane> { lane1, lane2 }, new List<Booking> { futureBookingToday, pastBookingToday, tomorrowBooking });
        var laneService = new LaneService(uow);

        var result = await laneService.GetLanesForVenueAsync(venueId);

        var lane1Dto = result.FirstOrDefault(l => l.Id == lane1.Id);
        var lane2Dto = result.FirstOrDefault(l => l.Id == lane2.Id);

        Assert.NotNull(lane1Dto);
        Assert.NotNull(lane1Dto.NextBookingToday);
        Assert.Equal("VA-NEXT1", lane1Dto.NextBookingToday.BookingReference);
        Assert.Equal("Sarah Connor", lane1Dto.NextBookingToday.GuestName);
        Assert.Equal(4, lane1Dto.NextBookingToday.PartySize);

        // Lane 2 has no bookings
        Assert.NotNull(lane2Dto);
        Assert.Null(lane2Dto.NextBookingToday);
    }
}

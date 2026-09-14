using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class ReportsAndVenueFeaturesTests
{
    private class FakeLaneRepo : ILaneRepository
    {
        private readonly List<Lane> _lanes;
        public FakeLaneRepo(List<Lane> lanes) => _lanes = lanes;
        public Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Lane>>(_lanes.Where(l => l.VenueId == venueId && (includeInactive || l.IsActive)).ToList());
        public Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, CancellationToken cancellationToken = default)
            => Task.FromResult(_lanes.FirstOrDefault(l => isScreen ? l.ScreenPairingCode == code : l.TabletPairingCode == code));
        public Task<Lane?> GetWithSessionsAsync(Guid laneId, CancellationToken cancellationToken = default)
            => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == laneId));
        public Task<Lane?> GetWithActiveSessionAsync(Guid laneId, CancellationToken cancellationToken = default)
            => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == laneId));
        public Task<Lane?> GetByIdIgnoreQueryFiltersAsync(Guid laneId, CancellationToken cancellationToken = default)
            => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == laneId));
        public Task<Lane?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_lanes.FirstOrDefault(l => l.Id == id));
        public Task<IReadOnlyList<Lane>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Lane>>(_lanes.AsQueryable().Where(predicate).ToList());
        public Task<Lane> AddAsync(Lane entity, CancellationToken cancellationToken = default) { _lanes.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Lane entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Lane entity, CancellationToken cancellationToken = default) { _lanes.Remove(entity); return Task.CompletedTask; }
        public Task<IReadOnlyList<Lane>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindInTenantAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Lane>>(_lanes.AsQueryable().Where(predicate).ToList());
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
        public Task<IReadOnlyList<Booking>> FindAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Booking>>(_bookings.AsQueryable().Where(predicate).ToList());
        public Task<Booking> AddAsync(Booking entity, CancellationToken cancellationToken = default) { _bookings.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Booking entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Booking entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Booking>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings);
        public Task<IReadOnlyList<Booking>> FindInTenantAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Booking>>(_bookings.AsQueryable().Where(predicate).ToList());
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
        public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_venue);
        public Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(_venue);
        public Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(_venue);
        public Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult(_venue);
        public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_venue != null ? new[] { _venue } : Array.Empty<Venue>());
        public Task<IReadOnlyList<Venue>> FindAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_venue != null ? new[] { _venue } : Array.Empty<Venue>());
        public Task<Venue> AddAsync(Venue entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task UpdateAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Venue>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_venue != null ? new[] { _venue } : Array.Empty<Venue>());
        public Task<IReadOnlyList<Venue>> FindInTenantAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_venue != null ? new[] { _venue } : Array.Empty<Venue>());
        public Task<Venue?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_venue);
    }

    private class FakeUow : IUnitOfWork
    {
        public FakeUow(Venue venue, List<Lane> lanes, List<Booking> bookings, BookingConfig? config = null)
        {
            Venues = new FakeVenueRepo(venue);
            Lanes = new FakeLaneRepo(lanes);
            Bookings = new FakeBookingRepo(bookings);
            BookingConfigs = new FakeBookingConfigRepo(config);
        }

        public IVenueRepository Venues { get; }
        public ILaneRepository Lanes { get; }
        public IBookingRepository Bookings { get; }
        public IBookingConfigRepository BookingConfigs { get; }
        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public IUserRepository Users => throw new NotImplementedException();
        public IWaiverRepository Waivers => throw new NotImplementedException();
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    [Fact]
    public async Task ReportingService_GetDailyReportAsync_CalculatesCorrectMetrics()
    {
        var venueId = Guid.NewGuid();
        var venue = new Venue { Id = venueId, Name = "Downtown Target Club", Timezone = "America/New_York" };
        var lane1 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 1, Name = "Lane 1", IsActive = true };
        var lane2 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 2, Name = "Lane 2", IsActive = true };

        var day = new DateOnly(2026, 6, 15);
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var (startUtc, _) = VenueTimeZoneHelper.GetUtcDayRange(day, tz);

        var booking1 = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "VA-101",
            GuestFirstName = "Alice",
            GuestLastName = "Smith",
            PartySize = 4,
            StartTime = startUtc.AddHours(14), // 2 PM EDT
            EndTime = startUtc.AddHours(15),
            Status = BookingStatus.Completed,
            TotalAmountCents = 16000,
            PaidAmountCents = 16000,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id, Lane = lane1 } }
        };

        var booking2 = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "VA-102",
            GuestFirstName = "Bob",
            GuestLastName = "Jones",
            PartySize = 6,
            StartTime = startUtc.AddHours(14), // 2 PM EDT
            EndTime = startUtc.AddHours(16),
            Status = BookingStatus.Confirmed,
            TotalAmountCents = 30000,
            PaidAmountCents = 30000,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane2.Id, Lane = lane2 } }
        };

        var bookingCancelled = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "VA-CAN",
            GuestFirstName = "Charlie",
            GuestLastName = "Brown",
            PartySize = 2,
            StartTime = startUtc.AddHours(18),
            EndTime = startUtc.AddHours(19),
            Status = BookingStatus.Cancelled,
            TotalAmountCents = 8000,
            PaidAmountCents = 0,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id, Lane = lane1 } }
        };

        var uow = new FakeUow(venue, new List<Lane> { lane1, lane2 }, new List<Booking> { booking1, booking2, bookingCancelled });
        var reportingService = new ReportingService(uow);

        var report = await reportingService.GetDailyReportAsync(venueId, day);

        Assert.NotNull(report);
        Assert.Equal(day, report.Date);
        Assert.Equal(2, report.TotalBookings); // valid (non-cancelled) bookings
        Assert.Equal(1, report.CompletedSessions);
        Assert.Equal(1, report.CancelledBookings);
        Assert.Equal(10, report.TotalThrowers); // 4 + 6 (excluding cancelled)
        Assert.Equal(46000, report.GrossRevenueCents); // 16000 + 30000
        Assert.NotEmpty(report.HourlyDistribution);
        Assert.Equal(3, report.Bookings.Count);
    }

    [Fact]
    public async Task ReportingService_GetWeeklyReportAsync_ReturnsSevenDaysAndAggregates()
    {
        var venueId = Guid.NewGuid();
        var venue = new Venue { Id = venueId, Name = "Downtown Target Club", Timezone = "America/New_York" };
        var lane1 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 1, Name = "Lane 1", IsActive = true };

        var startWeek = new DateOnly(2026, 6, 15);
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var (startUtc, _) = VenueTimeZoneHelper.GetUtcDayRange(startWeek, tz);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BookingReference = "VA-W1",
            GuestFirstName = "Diana",
            GuestLastName = "Prince",
            PartySize = 5,
            StartTime = startUtc.AddHours(16),
            EndTime = startUtc.AddHours(17),
            Status = BookingStatus.Confirmed,
            TotalAmountCents = 20000,
            PaidAmountCents = 20000,
            BookingLanes = new List<BookingLane> { new() { LaneId = lane1.Id, Lane = lane1 } }
        };

        var uow = new FakeUow(venue, new List<Lane> { lane1 }, new List<Booking> { booking });
        var reportingService = new ReportingService(uow);

        var weekly = await reportingService.GetWeeklyReportAsync(venueId, startWeek);

        Assert.NotNull(weekly);
        Assert.Equal(7, weekly.DailyBreakdown.Count);
        Assert.Equal(1, weekly.TotalBookings);
        Assert.Equal(20000, weekly.GrossRevenueCents);
        Assert.Equal(5, weekly.TotalThrowers);
    }

    [Fact]
    public async Task ReportingService_ExportReportCsvAsync_ProducesValidRfc4180Csv()
    {
        var venueId = Guid.NewGuid();
        var venue = new Venue { Id = venueId, Name = "Downtown Target Club", Timezone = "America/New_York" };
        var lane1 = new Lane { Id = Guid.NewGuid(), VenueId = venueId, LaneNumber = 1, Name = "Lane 1", IsActive = true };
        var day = new DateOnly(2026, 6, 15);

        var uow = new FakeUow(venue, new List<Lane> { lane1 }, new List<Booking>());
        var reportingService = new ReportingService(uow);

        var csvBytes = await reportingService.ExportReportCsvAsync(venueId, "daily", day, day);

        Assert.NotNull(csvBytes);
        Assert.True(csvBytes.Length > 0);

        var csvText = Encoding.UTF8.GetString(csvBytes);
        Assert.Contains("Booking Reference,Customer Name,Customer Email", csvText);
    }

    [Fact]
    public void ClosedDates_VenueBusinessHoursJson_ParsesAndEnforcesClosure()
    {
        var businessHoursJson = JsonSerializer.Serialize(new
        {
            monday = new { open = "12:00", close = "22:00", isOpen = true },
            closedDates = new object[]
            {
                new { date = "2026-12-25", reason = "Christmas Day" },
                new { date = "2026-09-07", reason = "Labor Day" }
            }
        });

        var christmasDate = new DateOnly(2026, 12, 25);
        var laborDay = new DateOnly(2026, 9, 7);
        var regularMonday = new DateOnly(2026, 6, 15); // Monday

        var christmasWindow = BookingService.ResolveOperatingWindow(businessHoursJson, "[]", christmasDate, null);
        Assert.False(christmasWindow.IsDayAllowed);

        var laborDayWindow = BookingService.ResolveOperatingWindow(businessHoursJson, "[]", laborDay, null);
        Assert.False(laborDayWindow.IsDayAllowed);

        var regularWindow = BookingService.ResolveOperatingWindow(businessHoursJson, "[]", regularMonday, null);
        Assert.True(regularWindow.IsDayAllowed);

        // When package allows off-days booking, closure is bypassed
        var packageWithOffDays = JsonSerializer.Serialize(new[]
        {
            new { id = "pkg-vip", allowOffDaysBooking = true }
        });
        var christmasAllowed = BookingService.ResolveOperatingWindow(businessHoursJson, packageWithOffDays, christmasDate, "pkg-vip");
        Assert.True(christmasAllowed.IsDayAllowed);
    }

    [Fact]
    public void BookingConfig_ShowAddress_PersistsAndMapsCorrectly()
    {
        var config = new BookingConfig
        {
            VenueId = Guid.NewGuid()
        };

        // Default is true
        Assert.True(config.ShowAddress);

        // Toggle to false
        config.ShowAddress = false;
        Assert.False(config.ShowAddress);
        Assert.Contains("\"showAddress\":false", config.EditorThemeJson);

        // Toggle back to true
        config.ShowAddress = true;
        Assert.True(config.ShowAddress);
        Assert.Contains("\"showAddress\":true", config.EditorThemeJson);
    }

    [Fact]
    public void GameEngines_ExposeComprehensiveRulesAndObjectives()
    {
        var engineTypes = new[]
        {
            "watl_standard",
            "kill_hunter",
            "around_the_world",
            "axe_tictactoe",
            "blackjack_21",
            "countdown_301"
        };

        foreach (var typeId in engineTypes)
        {
            var engine = GameEngineRegistry.GetEngine(typeId);
            Assert.NotNull(engine);
            Assert.False(string.IsNullOrWhiteSpace(engine.Objective), $"Engine {typeId} must have an Objective");
            Assert.NotNull(engine.ScoringRules);
            Assert.NotEmpty(engine.ScoringRules);
            Assert.NotNull(engine.SpecialRules);
            Assert.NotEmpty(engine.SpecialRules);
        }
    }
}

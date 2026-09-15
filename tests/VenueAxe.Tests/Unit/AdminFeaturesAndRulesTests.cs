using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class AdminFeaturesAndRulesTests
{
    [Fact]
    public async Task ExportWaiversCsvAsync_ProducesRfcCompliantCsvWithMarketingOptIn()
    {
        var venueId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var template = new WaiverTemplate { Id = templateId, VenueId = venueId, Title = "Standard Waiver" };

        var waivers = new List<Waiver>
        {
            new()
            {
                Id = Guid.NewGuid(),
                VenueId = venueId,
                TemplateId = templateId,
                SignerFirstName = "John",
                SignerLastName = "Doe, Jr.",
                SignerEmail = "john@example.com",
                SignerPhone = "555-1234",
                DateOfBirth = new DateOnly(1990, 5, 12),
                IsGuardianSigning = true,
                MinorsCoveredJson = "[{\"name\":\"Timmy Doe\"}]",
                SignedAtUtc = new DateTimeOffset(2026, 9, 14, 18, 30, 0, TimeSpan.Zero),
                ExpiresAtUtc = new DateTimeOffset(2027, 9, 14, 18, 30, 0, TimeSpan.Zero),
                EmailMarketingOptIn = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                VenueId = venueId,
                TemplateId = templateId,
                SignerFirstName = "Jane",
                SignerLastName = "Smith",
                SignerEmail = "jane@example.com",
                SignerPhone = "555-5678",
                DateOfBirth = new DateOnly(1995, 8, 20),
                IsGuardianSigning = false,
                SignedAtUtc = new DateTimeOffset(2026, 9, 14, 19, 0, 0, TimeSpan.Zero),
                ExpiresAtUtc = new DateTimeOffset(2027, 9, 14, 19, 0, 0, TimeSpan.Zero),
                EmailMarketingOptIn = false
            }
        };

        var fakeWaiverRepo = new FakeWaiverRepo(template, waivers);
        var fakeUow = new FakeUnitOfWork(fakeWaiverRepo);
        var waiverService = new WaiverService(fakeUow);

        var csvBytes = await waiverService.ExportWaiversCsvAsync(venueId);
        var csvText = Encoding.UTF8.GetString(csvBytes);

        Assert.NotNull(csvText);
        Assert.Contains("WaiverId,BookingReference,SignerFirstName,SignerLastName,Email,Phone,DateOfBirth,IsGuardian,MinorsCovered,SignedAtUtc,ExpiresAtUtc,MarketingOptIn", csvText);
        Assert.Contains("\"Doe, Jr.\"", csvText); // Quotes around comma
        Assert.Contains("john@example.com", csvText);
        Assert.Contains("Timmy Doe", csvText);
        Assert.Contains("Yes", csvText); // MarketingOptIn for John
        Assert.Contains("No", csvText);  // MarketingOptIn for Jane
    }

    [Fact]
    public void FirstTo21Engine_BustsTo13_AndDoesNotResetTo11()
    {
        var engine = new FirstTo21Engine();
        var matchId = Guid.NewGuid();
        var players = new List<GamePlayer> { new() { Id = "p1", Name = "Player 1", Score = 19 } };
        var state = engine.Initialize(matchId, players);

        // Build score: 6 + 6 + 5 + 2 = 19
        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false); // 6
        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false); // 12
        state = engine.RecordThrow(state, null, null, TargetZone.Ring5, false);    // 17
        state = engine.RecordThrow(state, null, null, TargetZone.Ring2, false);    // 19
        Assert.Equal(19, state.Players[0].Score);

        // Player has 19. Throws 5 (Ring 5) -> 19 + 5 = 24 > 21 -> BUST! Resets to 13
        state = engine.RecordThrow(state, null, null, TargetZone.Ring5, false);

        Assert.Equal(13, state.Players[0].Score);
        Assert.Equal(MatchStatus.InProgress, state.Status);

        // Undo the bust throw -> should restore score back to 19
        state = engine.UndoLastThrow(state);
        Assert.Equal(19, state.Players[0].Score);

        // Hit Ring 2 (2 pts) -> 19 + 2 = 21 -> WIN!
        state = engine.RecordThrow(state, null, null, TargetZone.Ring2, false);
        Assert.Equal(21, state.Players[0].Score);
        Assert.Equal(MatchStatus.Finished, state.Status);
        Assert.Equal("p1", state.WinnerPlayerId);
    }

    [Fact]
    public void WatlTargetMath_CalledKillshot_AwardsEightOrZeroOnly()
    {
        // When Killshot is called:
        // Left Kill (-0.380, 0.460) -> 8 pts
        var leftHit = WatlTargetMath.Evaluate(-0.380, 0.460, isClutchCalled: true);
        Assert.Equal(8, leftHit.Points);
        Assert.True(leftHit.IsKillshotHit);

        // Center Bullseye (0, 0) -> Miss (0 pts)
        var bullseyeHit = WatlTargetMath.Evaluate(0.0, 0.0, isClutchCalled: true);
        Assert.Equal(0, bullseyeHit.Points);
        Assert.Equal(TargetZone.Miss, bullseyeHit.Zone);

        // Ring 5 (0.15, 0.0) -> Miss (0 pts)
        var ring5Hit = WatlTargetMath.Evaluate(0.15, 0.0, isClutchCalled: true);
        Assert.Equal(0, ring5Hit.Points);
        Assert.Equal(TargetZone.Miss, ring5Hit.Zone);

        // Ring 1 (-0.48, 0.0) -> Miss (0 pts)
        var ring1Hit = WatlTargetMath.Evaluate(-0.48, 0.0, isClutchCalled: true);
        Assert.Equal(0, ring1Hit.Points);
        Assert.Equal(TargetZone.Miss, ring1Hit.Zone);
    }

    [Fact]
    public void CountdownGameEngine_DefaultsTo603StartingScore()
    {
        var engine = new CountdownGameEngine();
        var players = new List<GamePlayer> { new() { Id = "p1", Name = "Player 1" } };
        var state = engine.Initialize(Guid.NewGuid(), players);

        Assert.Equal("countdown_603", engine.GameTypeId);
        Assert.Equal("Countdown 603", engine.DisplayName);
        Assert.Equal(603, state.Players[0].Score);

        // Throw bullseye (6 pts) -> 603 - 6 = 597
        state = engine.RecordThrow(state, null, null, TargetZone.Bullseye, false);
        Assert.Equal(597, state.Players[0].Score);
    }

    private class FakeWaiverRepo : IWaiverRepository
    {
        private readonly WaiverTemplate _template;
        private readonly List<Waiver> _waivers;

        public FakeWaiverRepo(WaiverTemplate template, List<Waiver> waivers)
        {
            _template = template;
            _waivers = waivers;
        }

        public Task<WaiverTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
            => Task.FromResult<WaiverTemplate?>(_template.Id == templateId ? _template : null);
        public Task<WaiverTemplate?> GetActiveTemplateByVenueSlugAsync(string venueSlug, CancellationToken cancellationToken = default)
            => Task.FromResult<WaiverTemplate?>(_template);
        public Task<IReadOnlyList<WaiverTemplate>> GetTemplatesByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<WaiverTemplate>>(new List<WaiverTemplate> { _template });
        public Task<Waiver?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_waivers.Find(w => w.Id == id));
        public Task<IReadOnlyList<Waiver>> SearchAsync(Guid venueId, string? searchTerm, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<(IReadOnlyList<Waiver> Items, int TotalCount)> SearchPagedAsync(Guid venueId, string? searchTerm, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<Waiver> Items, int TotalCount)>((_waivers, _waivers.Count));
        public Task<int> CountSignedForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
            => Task.FromResult(_waivers.Count);
        public Task<IReadOnlyList<Waiver>> GetAllForVenueAsync(Guid venueId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);

        public Task<Waiver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_waivers.Find(w => w.Id == id));
        public Task<IReadOnlyList<Waiver>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<IReadOnlyList<Waiver>> FindAsync(System.Linq.Expressions.Expression<Func<Waiver, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<Waiver> AddAsync(Waiver entity, CancellationToken cancellationToken = default) { _waivers.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Waiver entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Waiver entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Waiver>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<IReadOnlyList<Waiver>> FindInTenantAsync(System.Linq.Expressions.Expression<Func<Waiver, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<Waiver?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_waivers.Find(w => w.Id == id));
    }

    private class FakeUnitOfWork : IUnitOfWork
    {
        public FakeUnitOfWork(IWaiverRepository waivers)
        {
            Waivers = waivers;
        }

        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public IVenueRepository Venues => throw new NotImplementedException();
        public IUserRepository Users => throw new NotImplementedException();
        public ILaneRepository Lanes => throw new NotImplementedException();
        public IBookingRepository Bookings => throw new NotImplementedException();
        public IBookingConfigRepository BookingConfigs => throw new NotImplementedException();
        public IWaiverRepository Waivers { get; }
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.Repositories;
using VenueAxe.Services;
using VenueAxe.Infrastructure.Pdf;

namespace VenueAxe.Tests.Unit;

public class WaiverPdfAndLaneOperationsTests
{
    [Fact]
    public void WaiverPdfService_GeneratesValidPdfBytes()
    {
        var service = new WaiverPdfService();
        var waiver = new Waiver
        {
            Id = Guid.NewGuid(),
            TenantId = Guid.NewGuid(),
            VenueId = Guid.NewGuid(),
            SignerFirstName = "Jane",
            SignerLastName = "Doe",
            SignerEmail = "jane.doe@example.com",
            SignerPhone = "555-0199",
            DateOfBirth = new DateOnly(1995, 4, 12),
            SignedAtUtc = DateTimeOffset.UtcNow,
            IpAddress = "192.168.1.100",
            UserAgent = "Mozilla/5.0 Chrome/120.0",
            SignatureVectorSvg = "<svg><path d=\"M10 10 L50 50\" /></svg>",
            Template = new WaiverTemplate
            {
                Id = Guid.NewGuid(),
                Title = "General Liability Release",
                BodyTextMarkdown = "I hereby release and hold harmless VenueAxe and its affiliates...",
                VersionNumber = 1
            },
            Venue = new Venue
            {
                Id = Guid.NewGuid(),
                Name = "Downtown Arena",
                Slug = "downtown",
                AddressLine1 = "100 Main St",
                City = "Austin",
                State = "TX",
                PostalCode = "78701"
            }
        };

        var venue = waiver.Venue!;
        var pdfBytes = service.GeneratePdf(waiver, venue, waiver.Template);

        Assert.NotNull(pdfBytes);
        Assert.NotEmpty(pdfBytes);
        // PDF files start with "%PDF-" header
        Assert.True(pdfBytes.Length > 200);
        Assert.Equal((byte)'%', pdfBytes[0]);
        Assert.Equal((byte)'P', pdfBytes[1]);
        Assert.Equal((byte)'D', pdfBytes[2]);
        Assert.Equal((byte)'F', pdfBytes[3]);
    }

    private class FakeLaneSessionRepo : ILaneSessionRepository
    {
        public LaneSession? ActiveSession { get; set; }

        public Task<LaneSession?> GetActiveSessionForLaneAsync(Guid laneId, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveSession != null && ActiveSession.LaneId == laneId && ActiveSession.Status == SessionStatus.Active ? ActiveSession : null);

        public Task<GameMatch?> GetActiveMatchWithThrowsAsync(Guid sessionId, System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(ActiveSession?.Matches.FirstOrDefault(m => m.Status == MatchStatus.InProgress));

        public Task AddMatchAsync(GameMatch match, System.Threading.CancellationToken cancellationToken = default)
        {
            ActiveSession?.Matches.Add(match);
            return Task.CompletedTask;
        }

        public Task AddMatchThrowAsync(MatchThrow matchThrow, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveMatchThrowAsync(MatchThrow matchThrow, System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<LaneSession?> GetByIdAsync(Guid id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(ActiveSession?.Id == id ? ActiveSession : null);
        public Task<IReadOnlyList<LaneSession>> GetAllAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<LaneSession>>(ActiveSession != null ? new[] { ActiveSession } : Array.Empty<LaneSession>());
        public Task<IReadOnlyList<LaneSession>> FindAsync(System.Linq.Expressions.Expression<Func<LaneSession, bool>> predicate, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LaneSession> AddAsync(LaneSession entity, System.Threading.CancellationToken cancellationToken = default) { ActiveSession = entity; return Task.FromResult(entity); }
        public Task UpdateAsync(LaneSession entity, System.Threading.CancellationToken cancellationToken = default) { ActiveSession = entity; return Task.CompletedTask; }
        public Task DeleteAsync(LaneSession entity, System.Threading.CancellationToken cancellationToken = default) { ActiveSession = null; return Task.CompletedTask; }
        public Task<IReadOnlyList<LaneSession>> GetForCurrentTenantAsync(System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<LaneSession>> FindInTenantAsync(System.Linq.Expressions.Expression<Func<LaneSession, bool>> predicate, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LaneSession?> GetByIdInTenantAsync(Guid id, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeLaneRepo : ILaneRepository
    {
        public Lane? SeededLane { get; set; }

        public Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, bool includeInactive = false, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(SeededLane != null ? new[] { SeededLane } : Array.Empty<Lane>());
        public Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(SeededLane);
        public Task<Lane?> GetWithActiveSessionAsync(Guid laneId, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(SeededLane?.Id == laneId ? SeededLane : null);

        public Task<Lane?> GetByIdAsync(Guid id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(SeededLane?.Id == id ? SeededLane : null);
        public Task<Lane?> GetByIdIgnoreQueryFiltersAsync(Guid id, System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(SeededLane?.Id == id ? SeededLane : null);
        public Task<IReadOnlyList<Lane>> GetAllAsync(System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Lane>> FindAsync(System.Linq.Expressions.Expression<Func<Lane, bool>> predicate, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Lane> AddAsync(Lane entity, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task UpdateAsync(Lane entity, System.Threading.CancellationToken cancellationToken = default) { SeededLane = entity; return Task.CompletedTask; }
        public Task DeleteAsync(Lane entity, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Lane>> GetForCurrentTenantAsync(System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<Lane>> FindInTenantAsync(System.Linq.Expressions.Expression<Func<Lane, bool>> predicate, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Lane?> GetByIdInTenantAsync(Guid id, System.Threading.CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private class FakeLaneGameUow : Repositories.IUnitOfWork
    {
        public FakeLaneRepo LanesRepo { get; } = new();
        public FakeLaneSessionRepo SessionsRepo { get; } = new();

        public Repositories.IVenueRepository Venues => throw new NotImplementedException();
        public Repositories.IRepository<Tenant> Tenants => throw new NotImplementedException();
        public Repositories.ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public Repositories.IUserRepository Users => throw new NotImplementedException();
        public Repositories.ILaneRepository Lanes => LanesRepo;
        public Repositories.IBookingRepository Bookings => throw new NotImplementedException();
        public Repositories.IBookingConfigRepository BookingConfigs => throw new NotImplementedException();
        public Repositories.IWaiverRepository Waivers => throw new NotImplementedException();
        public Repositories.ILaneSessionRepository LaneSessions => SessionsRepo;
        public Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(System.Threading.CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task LaneGameService_SubstitutePlayer_UpdatesPlayerNameInRoster()
    {
        var uow = new FakeLaneGameUow();
        var laneId = Guid.NewGuid();
        uow.LanesRepo.SeededLane = new Lane { Id = laneId, Name = "Lane 01" };
        uow.SessionsRepo.ActiveSession = new LaneSession
        {
            Id = Guid.NewGuid(),
            LaneId = laneId,
            Status = SessionStatus.Active,
            ActiveRosterJson = System.Text.Json.JsonSerializer.Serialize(new List<GameEngine.GamePlayer>
            {
                new() { Id = "p1", Name = "Original Name", AvatarColor = "#ef4444" }
            })
        };

        var service = new LaneGameService(uow);
        var result = await service.SubstitutePlayerAsync(laneId, "p1", "Substituted Hero", "#3b82f6");

        Assert.True(result);
        var updatedRoster = System.Text.Json.JsonSerializer.Deserialize<List<GameEngine.GamePlayer>>(uow.SessionsRepo.ActiveSession.ActiveRosterJson);
        Assert.NotNull(updatedRoster);
        Assert.Equal("Substituted Hero", updatedRoster[0].Name);
        Assert.Equal("#3b82f6", updatedRoster[0].AvatarColor);
    }

    [Fact]
    public async Task LaneGameService_StartRematch_ResetsPlayerScoresAndTwoKills()
    {
        var uow = new FakeLaneGameUow();
        var laneId = Guid.NewGuid();
        uow.LanesRepo.SeededLane = new Lane { Id = laneId, Name = "Lane 01" };
        var activeMatch = new GameMatch
        {
            Id = Guid.NewGuid(),
            GameTypeId = "watl_standard",
            Status = MatchStatus.InProgress
        };
        uow.SessionsRepo.ActiveSession = new LaneSession
        {
            Id = Guid.NewGuid(),
            LaneId = laneId,
            Status = SessionStatus.Active,
            Matches = new List<GameMatch> { activeMatch },
            ActiveRosterJson = System.Text.Json.JsonSerializer.Serialize(new List<GameEngine.GamePlayer>
            {
                new() { Id = "p1", Name = "Sarah", Score = 45, KillsRemaining = 0, KillsCalled = 2 }
            })
        };

        var service = new LaneGameService(uow);
        var newMatchSnapshot = await service.StartRematchAsync(laneId);

        Assert.NotNull(newMatchSnapshot);
        Assert.Equal(MatchStatus.Finished, activeMatch.Status);
        Assert.Equal(0, newMatchSnapshot.Players[0].Score);
        Assert.Equal(2, newMatchSnapshot.Players[0].KillsRemaining);
        Assert.Equal(0, newMatchSnapshot.Players[0].KillsCalled);
    }

    [Fact]
    public async Task LaneGameService_EndSession_SetsLaneToTurnaroundAndCompletesSession()
    {
        var uow = new FakeLaneGameUow();
        var laneId = Guid.NewGuid();
        var lane = new Lane { Id = laneId, Name = "Lane 01", CurrentStatus = LaneStatus.Active };
        uow.LanesRepo.SeededLane = lane;
        var session = new LaneSession
        {
            Id = Guid.NewGuid(),
            LaneId = laneId,
            Status = SessionStatus.Active
        };
        uow.SessionsRepo.ActiveSession = session;

        var service = new LaneGameService(uow);
        var result = await service.EndSessionAsync(laneId);

        Assert.True(result);
        Assert.Equal(LaneStatus.Turnaround, lane.CurrentStatus);
        Assert.Equal(SessionStatus.Completed, session.Status);
        Assert.NotNull(session.EndedAt);
    }

    [Fact]
    public async Task LaneGameService_SwitchGame_PreservesSessionAndLaunchesNewEngine()
    {
        var uow = new FakeLaneGameUow();
        var laneId = Guid.NewGuid();
        var lane = new Lane { Id = laneId, Name = "Lane 01", CurrentStatus = LaneStatus.Active };
        uow.LanesRepo.SeededLane = lane;

        var activeMatch = new GameMatch
        {
            Id = Guid.NewGuid(),
            GameTypeId = "watl_standard",
            Status = MatchStatus.InProgress
        };

        var session = new LaneSession
        {
            Id = Guid.NewGuid(),
            LaneId = laneId,
            Status = SessionStatus.Active,
            Matches = new List<GameMatch> { activeMatch },
            ActiveRosterJson = System.Text.Json.JsonSerializer.Serialize(new List<GameEngine.GamePlayer>
            {
                new() { Id = "p1", Name = "Marcus", Score = 36 }
            })
        };
        uow.SessionsRepo.ActiveSession = session;

        var service = new LaneGameService(uow);
        var switchedState = await service.SwitchGameAsync(laneId, "axe_tictactoe");

        Assert.NotNull(switchedState);
        Assert.Equal("axe_tictactoe", switchedState.GameTypeId);
        Assert.Equal(MatchStatus.Finished, activeMatch.Status);
        Assert.Equal(0, switchedState.Players[0].Score);
        Assert.Equal(1, session.Matches.Count(m => m.Status == MatchStatus.InProgress));
    }
}

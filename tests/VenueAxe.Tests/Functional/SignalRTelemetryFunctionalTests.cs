using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using VenueAxe.Domain.Entities;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;
using VenueAxe.Web.Hubs;
using Xunit;

namespace VenueAxe.Tests.Functional;

public class SignalRTelemetryFunctionalTests
{
    private class FakeGroupManager : IGroupManager
    {
        public List<(string ConnectionId, string GroupName)> Added = new();
        public List<(string ConnectionId, string GroupName)> Removed = new();

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Added.Add((connectionId, groupName));
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            Removed.Add((connectionId, groupName));
            return Task.CompletedTask;
        }
    }

    private class FakeLaneClient : ILaneClient
    {
        public List<(Guid LaneId, string Status)> LaneStateChanges = new();
        public List<GameStateSnapshot> ThrowsRecorded = new();
        public List<(string PlayerId, string Side)> ClutchCalls = new();
        public List<(string PlayerId, string Side)> KillCalls = new();
        public List<string> SafetyStops = new();
        public List<int> SessionExtensions = new();
        public List<(Guid LaneId, string TerminalType, int? BatteryLevel, DateTimeOffset Timestamp)> Heartbeats = new();

        public Task OnLaneStateChanged(Guid laneId, string status)
        {
            LaneStateChanges.Add((laneId, status));
            return Task.CompletedTask;
        }

        public Task OnThrowRecorded(GameStateSnapshot gameState)
        {
            ThrowsRecorded.Add(gameState);
            return Task.CompletedTask;
        }

        public Task OnClutchCalled(string playerId, string side)
        {
            ClutchCalls.Add((playerId, side));
            return Task.CompletedTask;
        }

        public Task OnKillCalled(string playerId, string side)
        {
            KillCalls.Add((playerId, side));
            return Task.CompletedTask;
        }

        public Task OnSafetyStopActivated(string reason)
        {
            SafetyStops.Add(reason);
            return Task.CompletedTask;
        }

        public Task OnSessionExtended(int newRemainingMinutes)
        {
            SessionExtensions.Add(newRemainingMinutes);
            return Task.CompletedTask;
        }

        public Task OnHeartbeatReceived(Guid laneId, string terminalType, int? batteryLevel, DateTimeOffset timestamp)
        {
            Heartbeats.Add((laneId, terminalType, batteryLevel, timestamp));
            return Task.CompletedTask;
        }
    }

    private class FakeHubCallerClients : IHubCallerClients<ILaneClient>
    {
        public FakeLaneClient ClientInstance { get; } = new();

        public ILaneClient Caller => ClientInstance;
        public ILaneClient Others => ClientInstance;
        public ILaneClient All => ClientInstance;
        public ILaneClient AllExcept(IReadOnlyList<string> excludedConnectionIds) => ClientInstance;
        public ILaneClient Client(string connectionId) => ClientInstance;
        public ILaneClient Clients(IReadOnlyList<string> connectionIds) => ClientInstance;
        public ILaneClient Group(string groupName) => ClientInstance;
        public ILaneClient Groups(IReadOnlyList<string> groupNames) => ClientInstance;
        public ILaneClient GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => ClientInstance;
        public ILaneClient OthersInGroup(string groupName) => ClientInstance;
        public ILaneClient User(string userId) => ClientInstance;
        public ILaneClient Users(IReadOnlyList<string> userIds) => ClientInstance;
    }

    private class FakeHubCallerContext : HubCallerContext
    {
        public override string ConnectionId { get; } = "conn-9988";
        public override string? UserIdentifier => "user-123";
        public override System.Security.Claims.ClaimsPrincipal? User => null;
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features { get; } = new FeatureCollection();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }

    private class FakeLaneRepo : ILaneRepository
    {
        private readonly List<Lane> _lanes;
        public FakeLaneRepo(List<Lane> lanes) => _lanes = lanes;

        public Task<Lane?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.Find(l => l.Id == id));
        public Task<Lane?> GetByIdIgnoreQueryFiltersAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.Find(l => l.Id == id));
        public Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes.FindAll(l => l.VenueId == venueId && (includeInactive || l.IsActive)));
        public Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.Find(l => (isScreen ? l.ScreenPairingCode : l.TabletPairingCode) == code));
        public Task<Lane?> GetWithActiveSessionAsync(Guid laneId, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.Find(l => l.Id == laneId));
        public Task<IReadOnlyList<Lane>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes.AsQueryable().Where(predicate).ToList());
        public Task<Lane> AddAsync(Lane entity, CancellationToken cancellationToken = default) { _lanes.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Lane entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Lane entity, CancellationToken cancellationToken = default) { _lanes.Remove(entity); return Task.CompletedTask; }
        public Task<IReadOnlyList<Lane>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes);
        public Task<IReadOnlyList<Lane>> FindInTenantAsync(Expression<Func<Lane, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Lane>>(_lanes.AsQueryable().Where(predicate).ToList());
        public Task<Lane?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_lanes.Find(l => l.Id == id));
    }

    private class FakeUnitOfWork : IUnitOfWork
    {
        public List<Lane> LanesList { get; } = new();
        public ILaneRepository Lanes { get; }

        public FakeUnitOfWork()
        {
            Lanes = new FakeLaneRepo(LanesList);
        }

        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public IVenueRepository Venues => throw new NotImplementedException();
        public IUserRepository Users => throw new NotImplementedException();
        public IBookingRepository Bookings => throw new NotImplementedException();
        public IBookingConfigRepository BookingConfigs => throw new NotImplementedException();
        public IWaiverRepository Waivers => throw new NotImplementedException();
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [Fact]
    public async Task JoinAndLeaveLaneGroup_ManagesSignalRConnectionGroups()
    {
        var laneId = Guid.NewGuid();
        var uow = new FakeUnitOfWork();
        var hub = new LaneHub(uow);
        var groups = new FakeGroupManager();

        hub.Context = new FakeHubCallerContext();
        hub.Groups = groups;

        await hub.JoinLaneGroup(laneId);
        Assert.Single(groups.Added);
        Assert.Equal("conn-9988", groups.Added[0].ConnectionId);
        Assert.Equal($"lane_{laneId}", groups.Added[0].GroupName);

        await hub.LeaveLaneGroup(laneId);
        Assert.Single(groups.Removed);
        Assert.Equal($"lane_{laneId}", groups.Removed[0].GroupName);
    }

    [Fact]
    public async Task CallKillshot_BroadcastsToBothKillAndClutchClients()
    {
        var laneId = Guid.NewGuid();
        var uow = new FakeUnitOfWork();
        var hub = new LaneHub(uow);
        var clients = new FakeHubCallerClients();

        hub.Context = new FakeHubCallerContext();
        hub.Clients = clients;

        await hub.CallKillshot(laneId, "player-1", "Left");

        Assert.Single(clients.ClientInstance.KillCalls);
        Assert.Equal("player-1", clients.ClientInstance.KillCalls[0].PlayerId);
        Assert.Equal("Left", clients.ClientInstance.KillCalls[0].Side);

        // Verify backwards-compatible clutch dispatch
        Assert.Single(clients.ClientInstance.ClutchCalls);
        Assert.Equal("player-1", clients.ClientInstance.ClutchCalls[0].PlayerId);
    }

    [Fact]
    public async Task SendHeartbeat_UpdatesLaneTimestamp_AndBroadcastsToAdminGroup()
    {
        var laneId = Guid.NewGuid();
        var lane = new Lane
        {
            Id = laneId,
            Name = "Lane 01",
            LaneNumber = 1,
            MaxThrowers = 6
        };

        var uow = new FakeUnitOfWork();
        uow.LanesList.Add(lane);

        var hub = new LaneHub(uow);
        var clients = new FakeHubCallerClients();

        hub.Context = new FakeHubCallerContext();
        hub.Clients = clients;

        await hub.SendHeartbeat(laneId, "tablet", 94);

        Assert.NotNull(lane.LastHeartbeatAt);
        Assert.Single(clients.ClientInstance.Heartbeats);
        Assert.Equal(laneId, clients.ClientInstance.Heartbeats[0].LaneId);
        Assert.Equal("tablet", clients.ClientInstance.Heartbeats[0].TerminalType);
        Assert.Equal(94, clients.ClientInstance.Heartbeats[0].BatteryLevel);
    }
}

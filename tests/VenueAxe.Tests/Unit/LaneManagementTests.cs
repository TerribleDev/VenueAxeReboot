using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Repositories;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class LaneManagementTests
{
    private class FakeUnitOfWork : IUnitOfWork
    {
        public List<Lane> LanesList { get; } = new();
        public List<Venue> VenuesList { get; } = new();

        public ILaneRepository Lanes { get; }
        public IVenueRepository Venues { get; }
        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public IUserRepository Users => throw new NotImplementedException();
        public IBookingRepository Bookings => throw new NotImplementedException();
        public IBookingConfigRepository BookingConfigs => throw new NotImplementedException();
        public IWaiverRepository Waivers => throw new NotImplementedException();
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();

        public FakeUnitOfWork()
        {
            Lanes = new FakeLaneRepo(LanesList);
            Venues = new FakeVenueRepo(VenuesList);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Dispose() { }
    }

    private class FakeVenueRepo : IVenueRepository
    {
        private readonly List<Venue> _venues;
        public FakeVenueRepo(List<Venue> venues) => _venues = venues;

        public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_venues.Find(v => v.Id == id));
        public Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(_venues.Find(v => v.Slug == slug));
        public Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(_venues.Find(v => v.Slug == slug));
        public Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult(_venues.Find(v => v.Id == venueId));
        public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_venues);
        public Task<IReadOnlyList<Venue>> FindAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_venues.AsQueryable().Where(predicate).ToList());
        public Task<Venue> AddAsync(Venue entity, CancellationToken cancellationToken = default) { _venues.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Venue entity, CancellationToken cancellationToken = default) { _venues.Remove(entity); return Task.CompletedTask; }
        public Task<IReadOnlyList<Venue>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_venues);
        public Task<IReadOnlyList<Venue>> FindInTenantAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(_venues.AsQueryable().Where(predicate).ToList());
        public Task<Venue?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_venues.Find(v => v.Id == id));
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

    [Fact]
    public async Task CreateLaneAsync_ValidRequest_CreatesLaneWithPairingCodes()
    {
        var uow = new FakeUnitOfWork();
        var venueId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        uow.VenuesList.Add(new Venue { Id = venueId, TenantId = tenantId, Name = "Test Venue", Slug = "test" });

        var laneService = new LaneService(uow);
        var created = await laneService.CreateLaneAsync(venueId, new CreateLaneRequest(LaneNumber: 9, Name: "Lane 09 - VIP", MaxThrowers: 8));

        Assert.NotNull(created);
        Assert.Equal(9, created.LaneNumber);
        Assert.Equal("Lane 09 - VIP", created.Name);
        Assert.Equal(8, created.MaxThrowers);
        Assert.Equal(LaneStatus.Available, created.CurrentStatus);
        Assert.StartsWith("AX", created.TabletPairingCode);
        Assert.StartsWith("TV", created.ScreenPairingCode);
    }

    [Fact]
    public async Task UpdateLaneAsync_ModifiesPropertiesSuccessfully()
    {
        var uow = new FakeUnitOfWork();
        var laneId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        uow.LanesList.Add(new Lane
        {
            Id = laneId,
            VenueId = venueId,
            LaneNumber = 1,
            Name = "Lane 01",
            MaxThrowers = 6,
            CurrentStatus = LaneStatus.Available
        });

        var laneService = new LaneService(uow);
        var updated = await laneService.UpdateLaneAsync(laneId, new UpdateLaneRequest(LaneNumber: 1, Name: "Lane 01 - Championship Bay", MaxThrowers: 10, Status: LaneStatus.Maintenance));

        Assert.NotNull(updated);
        Assert.Equal("Lane 01 - Championship Bay", updated.Name);
        Assert.Equal(10, updated.MaxThrowers);
        Assert.Equal(LaneStatus.Maintenance, updated.CurrentStatus);
    }

    [Fact]
    public async Task DeleteLaneAsync_RemovesLaneFromRepository()
    {
        var uow = new FakeUnitOfWork();
        var laneId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        uow.LanesList.Add(new Lane { Id = laneId, VenueId = venueId, LaneNumber = 5, Name = "Lane 05", MaxThrowers = 6 });

        var laneService = new LaneService(uow);
        var deleted = await laneService.DeleteLaneAsync(laneId);

        Assert.True(deleted);
        Assert.Empty(uow.LanesList);
    }

    [Fact]
    public async Task RegeneratePairingCodesAsync_AssignsNewCodes()
    {
        var uow = new FakeUnitOfWork();
        var laneId = Guid.NewGuid();
        uow.LanesList.Add(new Lane
        {
            Id = laneId,
            LaneNumber = 2,
            Name = "Lane 02",
            TabletPairingCode = "AX100",
            ScreenPairingCode = "TV100"
        });

        var laneService = new LaneService(uow);
        var regenerated = await laneService.RegeneratePairingCodesAsync(laneId);

        Assert.NotNull(regenerated);
        Assert.StartsWith("AX", regenerated.TabletPairingCode);
        Assert.StartsWith("TV", regenerated.ScreenPairingCode);
    }
}

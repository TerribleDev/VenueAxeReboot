using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VenueAxe.Data;
using VenueAxe.Data.Repositories;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using Xunit;

namespace VenueAxe.Tests.Functional;

public class MultiTenantIsolationFunctionalTests
{
    private static UserContext CreateUserCtx(Guid? tenantId = null, Guid? userId = null, Guid? venueId = null)
    {
        var ctx = new UserContext();
        if (tenantId.HasValue)
        {
            ctx.SetManualContext(tenantId.Value, userId ?? Guid.NewGuid(), venueId);
        }
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
    public async Task TenantRepository_AutomaticallyAssignsCurrentTenantIdOnAdd()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantId = Guid.NewGuid();
        var userCtx = CreateUserCtx(tenantId);

        using (var db = CreateDbContext(userCtx, dbName))
        {
            var laneRepo = new LaneRepository(db, userCtx);
            var lane = new Lane
            {
                Id = Guid.NewGuid(),
                VenueId = Guid.NewGuid(),
                LaneNumber = 1,
                Name = "Lane 01",
                MaxThrowers = 6,
                IsActive = true
            };

            await laneRepo.AddAsync(lane);
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(userCtx, dbName))
        {
            var saved = await db.Lanes.FirstOrDefaultAsync();
            Assert.NotNull(saved);
            Assert.Equal(tenantId, saved.TenantId);
        }
    }

    [Fact]
    public async Task GlobalQueryFilter_StrictlyPartitionsDataAcrossTenants()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // 1. Seed Tenant A records
        var userCtxA = CreateUserCtx(tenantA);
        using (var db = CreateDbContext(userCtxA, dbName))
        {
            var laneA = new Lane
            {
                Id = Guid.NewGuid(),
                TenantId = tenantA,
                VenueId = Guid.NewGuid(),
                LaneNumber = 1,
                Name = "Alpha 1",
                MaxThrowers = 6,
                IsActive = true
            };
            db.Lanes.Add(laneA);
            await db.SaveChangesAsync();
        }

        // 2. Seed Tenant B records
        var userCtxB = CreateUserCtx(tenantB);
        using (var db = CreateDbContext(userCtxB, dbName))
        {
            var laneB = new Lane
            {
                Id = Guid.NewGuid(),
                TenantId = tenantB,
                VenueId = Guid.NewGuid(),
                LaneNumber = 2,
                Name = "Beta 2",
                MaxThrowers = 8,
                IsActive = true
            };
            db.Lanes.Add(laneB);
            await db.SaveChangesAsync();
        }

        // 3. Query under Tenant A context -> only Lane A returned
        using (var db = CreateDbContext(userCtxA, dbName))
        {
            var lanes = await db.Lanes.ToListAsync();
            Assert.Single(lanes);
            Assert.Equal("Alpha 1", lanes[0].Name);
            Assert.Equal(tenantA, lanes[0].TenantId);
        }

        // 4. Query under Tenant B context -> only Lane B returned
        using (var db = CreateDbContext(userCtxB, dbName))
        {
            var lanes = await db.Lanes.ToListAsync();
            Assert.Single(lanes);
            Assert.Equal("Beta 2", lanes[0].Name);
            Assert.Equal(tenantB, lanes[0].TenantId);
        }
    }

    [Fact]
    public async Task CrossTenantQuery_CannotAccessOtherTenantById()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var laneBId = Guid.NewGuid();

        var userCtxB = CreateUserCtx(tenantB);
        using (var db = CreateDbContext(userCtxB, dbName))
        {
            db.Lanes.Add(new Lane
            {
                Id = laneBId,
                TenantId = tenantB,
                VenueId = Guid.NewGuid(),
                LaneNumber = 5,
                Name = "Secret Lane B",
                MaxThrowers = 6,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Tenant A tries to find Lane B by ID
        var userCtxA = CreateUserCtx(tenantA);
        using (var db = CreateDbContext(userCtxA, dbName))
        {
            var laneRepo = new LaneRepository(db, userCtxA);
            var result = await laneRepo.GetByIdAsync(laneBId);

            // Must be null due to tenant filter!
            Assert.Null(result);
        }
    }

    [Fact]
    public async Task IgnoreQueryFilters_AllowsAuditedPublicAccessWhenTenantNotAuthenticated()
    {
        var dbName = Guid.NewGuid().ToString();
        var tenant = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var laneId = Guid.NewGuid();

        var authCtx = CreateUserCtx(tenant);
        using (var db = CreateDbContext(authCtx, dbName))
        {
            db.Lanes.Add(new Lane
            {
                Id = laneId,
                TenantId = tenant,
                VenueId = venueId,
                LaneNumber = 1,
                Name = "Target Alpha",
                TabletPairingCode = "AX101",
                MaxThrowers = 6,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Anonymous context (e.g. tablet hardware pairing)
        var anonCtx = CreateUserCtx();
        using (var db = CreateDbContext(anonCtx, dbName))
        {
            // By default with no tenant in context, global query filter blocks
            var defaultQuery = await db.Lanes.FirstOrDefaultAsync(l => l.TabletPairingCode == "AX101");
            Assert.Null(defaultQuery);

            // With vetted IgnoreQueryFilters() and specific PIN check:
            var auditedQuery = await db.Lanes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.TabletPairingCode == "AX101");

            Assert.NotNull(auditedQuery);
            Assert.Equal("Target Alpha", auditedQuery.Name);
            Assert.Equal(tenant, auditedQuery.TenantId);
        }
    }
}

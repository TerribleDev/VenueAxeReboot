using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Reqnroll;
using VenueAxe.Data;
using VenueAxe.Data.Repositories;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class MultiTenancySteps
{
    private readonly Dictionary<string, Guid> _tenantIds = new();
    private readonly string _dbName = Guid.NewGuid().ToString();
    private List<Lane> _queryResults = new();
    private Lane? _createdLane;

    private VenueAxeDbContext CreateDb(Guid? tenantId)
    {
        var ctx = new UserContext();
        if (tenantId.HasValue)
        {
            ctx.SetManualContext(tenantId.Value, Guid.NewGuid());
        }

        var options = new DbContextOptionsBuilder<VenueAxeDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        return new VenueAxeDbContext(options, ctx);
    }

    [Given(@"two distinct tenants ""(.*)"" and ""(.*)"" exist")]
    public void GivenTwoDistinctTenantsExist(string t1, string t2)
    {
        _tenantIds[t1] = Guid.NewGuid();
        _tenantIds[t2] = Guid.NewGuid();
    }

    [Given(@"""(.*)"" operates a lane named ""(.*)""")]
    public async Task GivenTenantOperatesALane(string tenantName, string laneName)
    {
        var tenantId = _tenantIds[tenantName];
        using var db = CreateDb(tenantId);

        db.Lanes.Add(new Lane
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            VenueId = Guid.NewGuid(),
            LaneNumber = 1,
            Name = laneName,
            MaxThrowers = 6,
            IsActive = true
        });

        await db.SaveChangesAsync();
    }

    [When(@"an operator queries lanes under ""(.*)"" context")]
    public async Task WhenAnOperatorQueriesLanesUnderContext(string tenantName)
    {
        var tenantId = _tenantIds[tenantName];
        using var db = CreateDb(tenantId);

        _queryResults = await db.Lanes.ToListAsync();
    }

    [Then(@"only ""(.*)"" should be returned")]
    public void ThenOnlyLaneShouldBeReturned(string laneName)
    {
        _queryResults.Should().ContainSingle();
        _queryResults[0].Name.Should().Be(laneName);
    }

    [Then(@"""(.*)"" should not be visible")]
    public void ThenLaneShouldNotBeVisible(string laneName)
    {
        _queryResults.Any(l => l.Name == laneName).Should().BeFalse();
    }

    [Given(@"an authenticated operator for tenant ""(.*)""")]
    public void GivenAnAuthenticatedOperatorForTenant(string tenantName)
    {
        if (!_tenantIds.ContainsKey(tenantName))
        {
            _tenantIds[tenantName] = Guid.NewGuid();
        }
    }

    [When(@"the operator creates a new lane named ""(.*)""")]
    public async Task WhenTheOperatorCreatesANewLane(string laneName)
    {
        var tenantId = _tenantIds.Values.First();
        var ctx = new UserContext();
        ctx.SetManualContext(tenantId, Guid.NewGuid());

        var options = new DbContextOptionsBuilder<VenueAxeDbContext>()
            .UseInMemoryDatabase(databaseName: _dbName)
            .Options;

        using var db = new VenueAxeDbContext(options, ctx);
        var repo = new LaneRepository(db, ctx);

        _createdLane = new Lane
        {
            Id = Guid.NewGuid(),
            VenueId = Guid.NewGuid(),
            LaneNumber = 9,
            Name = laneName,
            MaxThrowers = 6,
            IsActive = true
        };

        await repo.AddAsync(_createdLane);
        await db.SaveChangesAsync();
    }

    [Then(@"the lane should automatically be assigned the tenant ID of ""(.*)""")]
    public void ThenTheLaneShouldAutomaticallyBeAssignedTenantId(string tenantName)
    {
        var expectedTenantId = _tenantIds[tenantName];
        _createdLane.Should().NotBeNull();
        _createdLane!.TenantId.Should().Be(expectedTenantId);
    }
}

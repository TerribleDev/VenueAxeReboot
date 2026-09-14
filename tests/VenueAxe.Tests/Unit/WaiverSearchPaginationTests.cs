using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using VenueAxe.Data;
using VenueAxe.Data.Repositories;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.DTOs;
using VenueAxe.Infrastructure.Pdf;
using VenueAxe.Services;

namespace VenueAxe.Tests.Unit;

public class WaiverSearchPaginationTests
{
    private static UserContext CreateUserCtx(Guid tenantId, Guid? venueId = null)
    {
        var ctx = new UserContext();
        ctx.SetManualContext(tenantId, Guid.NewGuid(), venueId);
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
    public async Task SearchPagedAsync_PaginatesCorrectly_AndCalculatesTotalCount()
    {
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        var userCtx = CreateUserCtx(tenantId, venueId);
        using (var db = CreateDbContext(userCtx, dbName))
        {
            for (int i = 1; i <= 25; i++)
            {
                db.Waivers.Add(new Waiver
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    VenueId = venueId,
                    SignerFirstName = $"Thrower{i:D2}",
                    SignerLastName = "Archer",
                    SignerEmail = $"thrower{i}@example.com",
                    SignerPhone = $"555-01{i:D2}",
                    DateOfBirth = new DateOnly(1990, 1, 1),
                    SignedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-i),
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1)
                });
            }
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(userCtx, dbName))
        {
            var repo = new WaiverRepository(db, userCtx);

            // Page 1 of size 10
            var (page1, totalCount) = await repo.SearchPagedAsync(venueId, null, pageNumber: 1, pageSize: 10);
            Assert.Equal(25, totalCount);
            Assert.Equal(10, page1.Count);
            Assert.Equal("Thrower01", page1[0].SignerFirstName);

            // Page 3 of size 10 (remaining 5)
            var (page3, _) = await repo.SearchPagedAsync(venueId, null, pageNumber: 3, pageSize: 10);
            Assert.Equal(5, page3.Count);
            Assert.Equal("Thrower21", page3[0].SignerFirstName);
        }
    }

    [Fact]
    public async Task SearchPagedAsync_CaseInsensitiveMultiField_MatchesAllCriteria()
    {
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        var userCtx = CreateUserCtx(tenantId, venueId);
        using (var db = CreateDbContext(userCtx, dbName))
        {
            db.Waivers.AddRange(
                new Waiver
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    VenueId = venueId,
                    SignerFirstName = "Samantha",
                    SignerLastName = "Vance",
                    SignerEmail = "sam.vance@targetaxe.com",
                    SignerPhone = "555-4321",
                    DateOfBirth = new DateOnly(1988, 5, 20),
                    IsGuardianSigning = true,
                    MinorsCoveredJson = "[{\"name\":\"Leo Vance\"},{\"name\":\"Chloe Vance\"}]",
                    SignedAtUtc = DateTimeOffset.UtcNow
                },
                new Waiver
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    VenueId = venueId,
                    SignerFirstName = "Marcus",
                    SignerLastName = "Brody",
                    SignerEmail = "mbrody@adventure.org",
                    SignerPhone = "555-8888",
                    DateOfBirth = new DateOnly(1992, 11, 3),
                    SignedAtUtc = DateTimeOffset.UtcNow
                }
            );
            await db.SaveChangesAsync();
        }

        using (var db = CreateDbContext(userCtx, dbName))
        {
            var repo = new WaiverRepository(db, userCtx);

            // 1. Search by lowercase first name
            var (res1, count1) = await repo.SearchPagedAsync(venueId, "samantha");
            Assert.Equal(1, count1);
            Assert.Equal("Samantha", res1[0].SignerFirstName);

            // 2. Search by uppercase last name
            var (res2, count2) = await repo.SearchPagedAsync(venueId, "BRODY");
            Assert.Equal(1, count2);
            Assert.Equal("Marcus", res2[0].SignerFirstName);

            // 3. Search by full name with space
            var (res3, count3) = await repo.SearchPagedAsync(venueId, "samantha vance");
            Assert.Equal(1, count3);

            // 4. Search by email fragment
            var (res4, count4) = await repo.SearchPagedAsync(venueId, "TARGETAXE");
            Assert.Equal(1, count4);

            // 5. Search by phone fragment
            var (res5, count5) = await repo.SearchPagedAsync(venueId, "8888");
            Assert.Equal(1, count5);

            // 6. Search by minor child name
            var (res6, count6) = await repo.SearchPagedAsync(venueId, "Leo");
            Assert.Equal(1, count6);
            Assert.Equal("Samantha", res6[0].SignerFirstName);
        }
    }

    [Fact]
    public void FormatMinorNames_ExtractsNamesHumanReadably()
    {
        // Array of objects
        var json1 = "[{\"name\":\"Alex Jr.\"},{\"name\":\"Lily Doe\"}]";
        var result1 = WaiverPdfService.FormatMinorNames(json1);
        Assert.Equal("Alex Jr., Lily Doe", result1);

        // Array of plain strings
        var json2 = "[\"Timmy\",\"Bobby\"]";
        var result2 = WaiverPdfService.FormatMinorNames(json2);
        Assert.Equal("Timmy, Bobby", result2);

        // Single object
        var json3 = "{\"name\":\"Charlie\"}";
        var result3 = WaiverPdfService.FormatMinorNames(json3);
        Assert.Equal("Charlie", result3);

        // Null / Empty
        Assert.Equal(string.Empty, WaiverPdfService.FormatMinorNames(null));
        Assert.Equal(string.Empty, WaiverPdfService.FormatMinorNames("   "));
    }
}

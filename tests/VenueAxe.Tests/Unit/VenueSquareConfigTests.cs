using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Repositories;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class VenueSquareConfigTests
{
    private class FakeUserContext : IUserContext
    {
        public Guid? TenantId { get; set; } = Guid.NewGuid();
        public Guid? UserId { get; set; } = Guid.NewGuid();
        public Guid? VenueId { get; set; } = Guid.NewGuid();
        public string? Role { get; set; } = "Owner";
        public string? Email { get; set; } = "owner@example.com";
        public bool IsAuthenticated => true;
        public void SetManualContext(Guid tenantId, Guid? userId = null, Guid? venueId = null)
        {
            TenantId = tenantId;
            UserId = userId;
            VenueId = venueId;
        }
    }

    private class FakeVenueRepo : IVenueRepository
    {
        public Venue? Venue { get; set; }
        public FakeVenueRepo(Venue? venue = null) => Venue = venue;
        public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Venue);
        public Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(Venue);
        public Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult(Venue);
        public Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult(Venue);
        public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(Venue != null ? new[] { Venue } : Array.Empty<Venue>());
        public Task<IReadOnlyList<Venue>> FindAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(Venue != null ? new[] { Venue } : Array.Empty<Venue>());
        public Task<Venue> AddAsync(Venue entity, CancellationToken cancellationToken = default) { Venue = entity; return Task.FromResult(entity); }
        public Task UpdateAsync(Venue entity, CancellationToken cancellationToken = default) { Venue = entity; return Task.CompletedTask; }
        public Task DeleteAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Venue>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(Venue != null ? new[] { Venue } : Array.Empty<Venue>());
        public Task<IReadOnlyList<Venue>> FindInTenantAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(Venue != null ? new[] { Venue } : Array.Empty<Venue>());
        public Task<Venue?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Venue);
    }

    private class FakeBookingConfigRepo : IBookingConfigRepository
    {
        public BookingConfig? Config { get; set; }
        public FakeBookingConfigRepo(BookingConfig? config = null) => Config = config;
        public Task<BookingConfig?> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult(Config);
        public Task<BookingConfig?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Config);
        public Task<IReadOnlyList<BookingConfig>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(Config != null ? new[] { Config } : Array.Empty<BookingConfig>());
        public Task<IReadOnlyList<BookingConfig>> FindAsync(Expression<Func<BookingConfig, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(Config != null ? new[] { Config } : Array.Empty<BookingConfig>());
        public Task<BookingConfig> AddAsync(BookingConfig entity, CancellationToken cancellationToken = default) { Config = entity; return Task.FromResult(entity); }
        public Task UpdateAsync(BookingConfig entity, CancellationToken cancellationToken = default) { Config = entity; return Task.CompletedTask; }
        public Task DeleteAsync(BookingConfig entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<BookingConfig>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(Config != null ? new[] { Config } : Array.Empty<BookingConfig>());
        public Task<IReadOnlyList<BookingConfig>> FindInTenantAsync(Expression<Func<BookingConfig, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BookingConfig>>(Config != null ? new[] { Config } : Array.Empty<BookingConfig>());
        public Task<BookingConfig?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Config);
    }

    private class FakeUow : IUnitOfWork
    {
        public FakeVenueRepo VenueRepo { get; }
        public FakeBookingConfigRepo ConfigRepo { get; }

        public FakeUow(Venue? venue = null, BookingConfig? config = null)
        {
            VenueRepo = new FakeVenueRepo(venue);
            ConfigRepo = new FakeBookingConfigRepo(config);
        }

        public IVenueRepository Venues => VenueRepo;
        public IBookingConfigRepository BookingConfigs => ConfigRepo;

        public ILaneRepository Lanes => throw new NotImplementedException();
        public IBookingRepository Bookings => throw new NotImplementedException();
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
    public async Task UpdateVenue_PreservesExistingSecretAccessToken_WhenMaskedIsProvided()
    {
        var venueId = Guid.NewGuid();
        var initialVenue = new Venue
        {
            Id = venueId,
            TenantId = Guid.NewGuid(),
            Name = "Axe Haven",
            Slug = "axe-haven",
            BrandingConfigJson = """
            {
                "payment": {
                    "gateway": "square",
                    "locationId": "LOC_INITIAL",
                    "appId": "sandbox-sq0idb-initial",
                    "accessToken": "EAAAEMySecretRealAccessToken12345",
                    "webhookKey": "whsec_real_key",
                    "environment": "sandbox"
                }
            }
            """
        };

        var uow = new FakeUow(initialVenue);
        var userContext = new FakeUserContext { TenantId = initialVenue.TenantId };
        var venueService = new VenueService(uow, userContext);

        // Act: User edits venue address and provides masked token in update
        var updateRequest = new UpdateVenueRequest(
            Name: "Axe Haven Updated",
            AddressLine1: "123 Main St",
            City: "Austin",
            State: "TX",
            PostalCode: "78701",
            Phone: null,
            Email: null,
            BusinessHoursJson: "{}",
            BrandingConfigJson: """
            {
                "payment": {
                    "gateway": "square",
                    "locationId": "LOC_NEW_LOC",
                    "appId": "sandbox-sq0idb-initial",
                    "accessToken": "••••••••••••••••",
                    "webhookKey": "••••••••",
                    "environment": "production"
                }
            }
            """
        );

        var updatedVenue = await venueService.UpdateVenueAsync(venueId, updateRequest);

        Assert.NotNull(updatedVenue);
        Assert.NotNull(updatedVenue.SquareConfig);
        Assert.True(updatedVenue.SquareConfig.HasAccessToken);
        Assert.Equal("LOC_NEW_LOC", updatedVenue.SquareConfig.LocationId);
        Assert.Equal("production", updatedVenue.SquareConfig.Environment);

        // Verify underlying database entity preserved the actual unmasked access token and webhook key
        var savedEntity = uow.VenueRepo.Venue!;
        using var doc = JsonDocument.Parse(savedEntity.BrandingConfigJson);
        var paymentElem = doc.RootElement.GetProperty("payment");
        Assert.Equal("EAAAEMySecretRealAccessToken12345", paymentElem.GetProperty("accessToken").GetString());
        Assert.Equal("whsec_real_key", paymentElem.GetProperty("webhookKey").GetString());
    }

    [Fact]
    public async Task GetPublicBookingPage_SanitizesSecretTokens_AndExposesPublicSquareCredentials()
    {
        var venueId = Guid.NewGuid();
        var venue = new Venue
        {
            Id = venueId,
            TenantId = Guid.NewGuid(),
            Name = "Target Zone",
            Slug = "target-zone",
            Currency = "USD",
            BrandingConfigJson = """
            {
                "primaryColor": "#f59e0b",
                "payment": {
                    "gateway": "square",
                    "locationId": "LOC_PUBLIC_OK",
                    "appId": "sandbox-sq0idb-public-ok",
                    "accessToken": "SUPER_SECRET_TOKEN_DO_NOT_LEAK",
                    "webhookKey": "SUPER_SECRET_WEBHOOK_KEY",
                    "environment": "sandbox"
                }
            }
            """
        };
        var config = new BookingConfig
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            TenantId = venue.TenantId,
            SlotDurationsMinutes = [60]
        };
        venue.BookingConfig = config;

        var uow = new FakeUow(venue, config);
        var configBuilder = new ConfigurationBuilder().Build();
        var squareService = new SquarePaymentService(configBuilder, NullLogger<SquarePaymentService>.Instance);
        var bookingService = new BookingService(uow, squareService, NullLogger<BookingService>.Instance);

        // Act: fetch public booking page
        var page = await bookingService.GetPublicBookingPageAsync("target-zone");

        // Assert: public properties are available
        Assert.NotNull(page);
        Assert.Equal("sandbox-sq0idb-public-ok", page.SquareApplicationId);
        Assert.Equal("LOC_PUBLIC_OK", page.SquareLocationId);
        Assert.Equal("sandbox", page.SquareEnvironment);

        // Assert: BrandingConfigJson returned to public caller NEVER contains the access token or webhook key
        Assert.DoesNotContain("SUPER_SECRET_TOKEN_DO_NOT_LEAK", page.BrandingConfigJson);
        Assert.DoesNotContain("SUPER_SECRET_WEBHOOK_KEY", page.BrandingConfigJson);
        Assert.DoesNotContain("accessToken", page.BrandingConfigJson);
        Assert.DoesNotContain("webhookKey", page.BrandingConfigJson);
    }
}

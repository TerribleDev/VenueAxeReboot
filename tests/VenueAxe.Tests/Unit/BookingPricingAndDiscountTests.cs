using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.Repositories;
using VenueAxe.Services;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class BookingPricingAndDiscountTests
{
    private class FakeSquareService : ISquarePaymentService
    {
        public Task<SquarePaymentResult> ProcessPaymentAsync(SquarePaymentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new SquarePaymentResult(true, "sq_pay_test", "sq_ord_test", null, "COMPLETED"));

        public Task<bool> VerifyWebhookSignatureAsync(string requestBody, string signatureHeader, string webhookUrl)
            => Task.FromResult(true);

        public string GetApplicationId() => "sandbox-test-app-id";
        public string GetLocationId() => "sandbox-test-loc-id";
    }

    private class FakeBookingUnitOfWork : IUnitOfWork
    {
        public Venue? SeededVenue { get; set; }

        public IVenueRepository Venues { get; }
        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public IUserRepository Users => throw new NotImplementedException();
        public ILaneRepository Lanes => throw new NotImplementedException();
        public IBookingRepository Bookings => throw new NotImplementedException();
        public IBookingConfigRepository BookingConfigs => throw new NotImplementedException();
        public IWaiverRepository Waivers => throw new NotImplementedException();
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();

        public FakeBookingUnitOfWork(Venue venue)
        {
            SeededVenue = venue;
            Venues = new FakeVenueRepo(venue);
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private class FakeVenueRepo : IVenueRepository
    {
        private readonly Venue _venue;
        public FakeVenueRepo(Venue venue) => _venue = venue;

        public Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue);
        public Task<Venue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue);
        public Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue);
        public Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue);
        public Task<IReadOnlyList<Venue>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new[] { _venue });
        public Task<IReadOnlyList<Venue>> FindAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new[] { _venue });
        public Task<Venue> AddAsync(Venue entity, CancellationToken cancellationToken = default) => Task.FromResult(entity);
        public Task UpdateAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Venue entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Venue>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new[] { _venue });
        public Task<IReadOnlyList<Venue>> FindInTenantAsync(Expression<Func<Venue, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Venue>>(new[] { _venue });
        public Task<Venue?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Venue?>(_venue);
    }

    [Fact]
    public async Task CalculatePricing_GroupSizeAboveThreshold_AppliesVolumeDiscount()
    {
        var venueId = Guid.NewGuid();
        var bookingConfig = new BookingConfig
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BasePriceCents = 3500, // $35/person
            PeakPriceCents = 4500,
            PricingModel = PricingModel.PerPerson,
            DepositType = DepositType.FullPayment,
            DiscountRulesJson = """
            [
                {
                    "id": "large_group_10",
                    "name": "Large Group Discount",
                    "type": "group_size",
                    "minPartySize": 10,
                    "discountPercent": 10,
                    "autoApply": true
                }
            ]
            """
        };

        var venue = new Venue
        {
            Id = venueId,
            Name = "Downtown Axe Bay",
            Slug = "downtown",
            BookingConfig = bookingConfig
        };

        var uow = new FakeBookingUnitOfWork(venue);
        var square = new FakeSquareService();
        var service = new BookingService(uow, square, NullLogger<BookingService>.Instance);

        // Party of 12 @ $35/person = $420.00 (42000 cents). 10% discount = $42.00 (4200 cents). Net = $378.00 (37800 cents).
        var req = new CalculatePriceRequest(
            PartySize: 12,
            DurationMinutes: 60,
            StartTime: new DateTimeOffset(2026, 9, 10, 14, 0, 0, TimeSpan.Zero) // non-peak (14:00)
        );

        var pricing = await service.CalculatePricingAsync("downtown", req);

        Assert.NotNull(pricing);
        Assert.Equal(42000, pricing.BaseSubtotalCents);
        Assert.Equal(4200, pricing.DiscountAmountCents);
        Assert.Equal(37800, pricing.NetTotalCents);
        Assert.Contains("10% off", pricing.AppliedDiscountDescription ?? "");
    }

    [Fact]
    public async Task CalculatePricing_FirstResponderPromoCode_Applies15PercentDiscount()
    {
        var venueId = Guid.NewGuid();
        var bookingConfig = new BookingConfig
        {
            Id = Guid.NewGuid(),
            VenueId = venueId,
            BasePriceCents = 4000, // $40/person
            PeakPriceCents = 5000,
            PricingModel = PricingModel.PerPerson,
            DepositType = DepositType.FullPayment,
            DiscountRulesJson = """
            [
                {
                    "id": "hero_discount",
                    "name": "First Responder Appreciation",
                    "type": "promo_code",
                    "code": "HERO10",
                    "discountPercent": 15,
                    "autoApply": false
                }
            ]
            """
        };

        var venue = new Venue
        {
            Id = venueId,
            Name = "Downtown Axe Bay",
            Slug = "downtown",
            BookingConfig = bookingConfig
        };

        var uow = new FakeBookingUnitOfWork(venue);
        var square = new FakeSquareService();
        var service = new BookingService(uow, square, NullLogger<BookingService>.Instance);

        // Party of 4 @ $40 = $160.00 (16000 cents). 15% off = $24.00 (2400 cents). Net = $136.00 (13600 cents).
        var req = new CalculatePriceRequest(
            PartySize: 4,
            DurationMinutes: 60,
            StartTime: new DateTimeOffset(2026, 9, 10, 14, 0, 0, TimeSpan.Zero),
            PromoCode: "HERO10"
        );

        var pricing = await service.CalculatePricingAsync("downtown", req);

        Assert.NotNull(pricing);
        Assert.Equal(16000, pricing.BaseSubtotalCents);
        Assert.Equal(2400, pricing.DiscountAmountCents);
        Assert.Equal(13600, pricing.NetTotalCents);
    }

    [Fact]
    public async Task CalculatePricing_WithDefaultAdultAndMinor_AppliesZeroDiscount()
    {
        var venueId = Guid.NewGuid();
        var bookingConfig = new BookingConfig
        {
            VenueId = venueId,
            BasePriceCents = 3500, // $35.00
            PeakPriceCents = 4500,
            PricingModel = PricingModel.PerPerson,
            DepositType = DepositType.FullPayment,
            PersonTypesJson = """
            [
                { "id": "adult", "name": "Adult", "description": "Ages 18+", "discountPercent": 0, "isDefault": true },
                { "id": "minor", "name": "Minor", "description": "Ages 10-17", "discountPercent": 0, "isDefault": false }
            ]
            """
        };

        var venue = new Venue
        {
            Id = venueId,
            Name = "Apex Axes Downtown",
            Slug = "downtown",
            BookingConfig = bookingConfig
        };

        var uow = new FakeBookingUnitOfWork(venue);
        var square = new FakeSquareService();
        var service = new BookingService(uow, square, NullLogger<BookingService>.Instance);

        var req = new CalculatePriceRequest(
            PartySize: 5,
            DurationMinutes: 60,
            StartTime: new DateTimeOffset(2026, 9, 10, 14, 0, 0, TimeSpan.Zero),
            PersonTypes: new List<PersonTypeSelectionDto>
            {
                new("adult", 3),
                new("minor", 2)
            }
        );

        var pricing = await service.CalculatePricingAsync("downtown", req);

        Assert.NotNull(pricing);
        Assert.Equal(17500, pricing.BaseSubtotalCents); // 5 * 3500
        Assert.Equal(0, pricing.DiscountAmountCents);
        Assert.Equal(17500, pricing.NetTotalCents);
    }

    [Fact]
    public async Task CalculatePricing_WithFirstResponderDiscount_CalculatesPerPersonDiscountCorrectly()
    {
        var venueId = Guid.NewGuid();
        var bookingConfig = new BookingConfig
        {
            VenueId = venueId,
            BasePriceCents = 4000, // $40.00
            PeakPriceCents = 4000,
            PricingModel = PricingModel.PerPerson,
            DepositType = DepositType.FullPayment,
            PersonTypesJson = """
            [
                { "id": "adult", "name": "Adult", "description": "Ages 18+", "discountPercent": 0, "isDefault": true },
                { "id": "minor", "name": "Minor", "description": "Ages 10-17", "discountPercent": 0, "isDefault": false },
                { "id": "first_responder", "name": "First Responder", "description": "Police, Fire, EMT", "discountPercent": 10, "isDefault": false }
            ]
            """
        };

        var venue = new Venue
        {
            Id = venueId,
            Name = "Apex Axes Downtown",
            Slug = "downtown",
            BookingConfig = bookingConfig
        };

        var uow = new FakeBookingUnitOfWork(venue);
        var square = new FakeSquareService();
        var service = new BookingService(uow, square, NullLogger<BookingService>.Instance);

        // 5 throwers: 2 Adults ($40 ea = $80), 1 Minor ($40 ea = $40), 2 First Responders (10% off $40 = $4 off ea -> $36 ea = $72).
        // Gross = $200.00 (20000 cents). Discount = $8.00 (800 cents). Net = $192.00 (19200 cents).
        var req = new CalculatePriceRequest(
            PartySize: 5,
            DurationMinutes: 60,
            StartTime: new DateTimeOffset(2026, 9, 10, 14, 0, 0, TimeSpan.Zero),
            PersonTypes: new List<PersonTypeSelectionDto>
            {
                new("adult", 2),
                new("minor", 1),
                new("first_responder", 2)
            }
        );

        var pricing = await service.CalculatePricingAsync("downtown", req);

        Assert.NotNull(pricing);
        Assert.Equal(20000, pricing.BaseSubtotalCents);
        Assert.Equal(800, pricing.DiscountAmountCents);
        Assert.Equal(19200, pricing.NetTotalCents);
        Assert.Contains("First Responder (10% off x2)", pricing.AppliedDiscountDescription);
    }
}

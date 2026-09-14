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

public class WaiverLinkingTests
{
    private class FakeWaiverUnitOfWork : IUnitOfWork
    {
        public WaiverTemplate SeededTemplate { get; set; } = null!;
        public List<Booking> SeededBookings { get; set; } = new();
        public List<Waiver> StoredWaivers { get; set; } = new();

        public IRepository<Tenant> Tenants => throw new NotImplementedException();
        public IVenueRepository Venues => throw new NotImplementedException();
        public IUserRepository Users => throw new NotImplementedException();
        public ILaneRepository Lanes => throw new NotImplementedException();
        public IBookingRepository Bookings => new FakeBookingRepo(SeededBookings);
        public IBookingConfigRepository BookingConfigs => throw new NotImplementedException();
        public IWaiverRepository Waivers => new FakeWaiverRepo(SeededTemplate, StoredWaivers);
        public ITenantRepository<WaiverTemplate> WaiverTemplates => throw new NotImplementedException();
        public ILaneSessionRepository LaneSessions => throw new NotImplementedException();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
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
            => Task.FromResult<IReadOnlyList<Booking>>(_bookings.Where(b => b.VenueId == venueId).ToList());

        public Task<int> CountOverlappingBookingsAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<IReadOnlyList<Booking>> GetOverlappingBookingsWithLanesAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Booking>>(new List<Booking>());

        public Task<IReadOnlyList<Booking>> GetUpcomingBookingsByLaneAsync(Guid laneId, DateTimeOffset fromTime, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Booking>>(_bookings.Where(b => b.EndTime >= fromTime && b.BookingLanes.Any(bl => bl.LaneId == laneId)).ToList());

        public Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_bookings.FirstOrDefault(b => b.Id == id));
        public Task<IReadOnlyList<Booking>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings);
        public Task<IReadOnlyList<Booking>> FindAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings);
        public Task<Booking> AddAsync(Booking entity, CancellationToken cancellationToken = default) { _bookings.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Booking entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Booking entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Booking>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings);
        public Task<IReadOnlyList<Booking>> FindInTenantAsync(Expression<Func<Booking, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Booking>>(_bookings);
        public Task<Booking?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_bookings.FirstOrDefault(b => b.Id == id));
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
            => Task.FromResult(_waivers.FirstOrDefault(w => w.Id == id));

        public Task<IReadOnlyList<Waiver>> SearchAsync(Guid venueId, string? searchTerm, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);

        public Task<(IReadOnlyList<Waiver> Items, int TotalCount)> SearchPagedAsync(Guid venueId, string? searchTerm, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
            => Task.FromResult<(IReadOnlyList<Waiver> Items, int TotalCount)>((_waivers, _waivers.Count));

        public Task<int> CountSignedForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
            => Task.FromResult(_waivers.Count(w => w.BookingId == bookingId));

        public Task<Waiver?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_waivers.FirstOrDefault(w => w.Id == id));
        public Task<IReadOnlyList<Waiver>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<IReadOnlyList<Waiver>> FindAsync(Expression<Func<Waiver, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<Waiver> AddAsync(Waiver entity, CancellationToken cancellationToken = default) { _waivers.Add(entity); return Task.FromResult(entity); }
        public Task UpdateAsync(Waiver entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task DeleteAsync(Waiver entity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Waiver>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<IReadOnlyList<Waiver>> FindInTenantAsync(Expression<Func<Waiver, bool>> predicate, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Waiver>>(_waivers);
        public Task<Waiver?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(_waivers.FirstOrDefault(w => w.Id == id));
    }

    [Fact]
    public async Task SubmitWaiver_WithValidBookingReference_LinksToMatchingBooking()
    {
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var template = new WaiverTemplate
        {
            Id = templateId,
            TenantId = tenantId,
            VenueId = venueId,
            Title = "Standard Liability Waiver",
            BodyTextMarkdown = "Rules...",
            Sha256Hash = "abc",
            IsActive = true
        };

        var booking = new Booking
        {
            Id = bookingId,
            TenantId = tenantId,
            VenueId = venueId,
            BookingReference = "VA-99412",
            GuestFirstName = "Alice",
            GuestLastName = "Smith",
            GuestEmail = "alice@example.com",
            Status = BookingStatus.Confirmed
        };

        var uow = new FakeWaiverUnitOfWork
        {
            SeededTemplate = template,
            SeededBookings = new List<Booking> { booking }
        };

        var service = new WaiverService(uow);

        var req = new SubmitWaiverRequest(
            TemplateId: templateId,
            BookingId: null,
            SignerFirstName: "Bob",
            SignerLastName: "Jones",
            SignerEmail: "bob@other.com",
            SignerPhone: "555-1234",
            DateOfBirth: new DateOnly(1995, 5, 20),
            IsGuardianSigning: false,
            MinorsCoveredJson: null,
            SignatureImagePngBase64: "data:image/png;base64,fake",
            SignatureVectorSvg: null,
            UserAgent: "Unit-Test-Agent",
            BookingReference: "VA-99412"
        );

        var result = await service.SubmitWaiverAsync(req, "127.0.0.1");

        Assert.NotNull(result);
        Assert.Equal(bookingId, result.BookingId);
        Assert.Equal(bookingId, uow.StoredWaivers.Single().BookingId);
    }

    [Fact]
    public async Task SubmitWaiver_WithMatchingSignerEmail_AutoLinksToTodayBooking()
    {
        var tenantId = Guid.NewGuid();
        var venueId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        var template = new WaiverTemplate
        {
            Id = templateId,
            TenantId = tenantId,
            VenueId = venueId,
            Title = "Standard Liability Waiver",
            BodyTextMarkdown = "Rules...",
            Sha256Hash = "abc",
            IsActive = true
        };

        var booking = new Booking
        {
            Id = bookingId,
            TenantId = tenantId,
            VenueId = venueId,
            BookingReference = "VA-11223",
            GuestFirstName = "Charlie",
            GuestLastName = "Brown",
            GuestEmail = "charlie@example.com",
            Status = BookingStatus.Confirmed
        };

        var uow = new FakeWaiverUnitOfWork
        {
            SeededTemplate = template,
            SeededBookings = new List<Booking> { booking }
        };

        var service = new WaiverService(uow);

        // No booking reference provided, but signer email matches
        var req = new SubmitWaiverRequest(
            TemplateId: templateId,
            BookingId: null,
            SignerFirstName: "Charlie",
            SignerLastName: "Brown",
            SignerEmail: "charlie@example.com",
            SignerPhone: "555-9876",
            DateOfBirth: new DateOnly(1992, 3, 15),
            IsGuardianSigning: false,
            MinorsCoveredJson: null,
            SignatureImagePngBase64: "data:image/png;base64,fake",
            SignatureVectorSvg: null,
            UserAgent: "Unit-Test-Agent"
        );

        var result = await service.SubmitWaiverAsync(req, "127.0.0.1");

        Assert.NotNull(result);
        Assert.Equal(bookingId, result.BookingId);
        Assert.Equal(bookingId, uow.StoredWaivers.Single().BookingId);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.Repositories;

namespace VenueAxe.Data.Repositories;
public class BookingRepository : TenantRepository<Booking>, IBookingRepository
{
    public BookingRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<Booking?> GetByIdWithLanesAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(b => b.BookingLanes).ThenInclude(bl => bl.Lane)
            .Include(b => b.Waivers)
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);
    }

    public async Task<Booking?> GetByReferenceAsync(string referenceCode, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(b => b.BookingLanes).ThenInclude(bl => bl.Lane)
            .Include(b => b.Waivers)
            .Include(b => b.Venue)
            .FirstOrDefaultAsync(b => b.BookingReference == referenceCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetByVenueAndDateRangeAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var startUtc = start.ToUniversalTime();
        var endUtc = end.ToUniversalTime();
        return await DbSet
            .Where(b => b.VenueId == venueId && b.StartTime >= startUtc && b.StartTime < endUtc)
            .Include(b => b.BookingLanes).ThenInclude(bl => bl.Lane)
            .Include(b => b.Waivers)
            .OrderByDescending(b => b.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountOverlappingBookingsAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var startUtc = start.ToUniversalTime();
        var endUtc = end.ToUniversalTime();
        return await DbSet
            .IgnoreQueryFilters()
            .Where(b => b.VenueId == venueId && b.Status != BookingStatus.Cancelled &&
                        b.StartTime < endUtc && b.EndTime > startUtc)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetOverlappingBookingsWithLanesAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        var startUtc = start.ToUniversalTime();
        var endUtc = end.ToUniversalTime();
        return await DbSet
            .IgnoreQueryFilters()
            .Where(b => b.VenueId == venueId && b.Status != BookingStatus.Cancelled &&
                        b.StartTime < endUtc && b.EndTime > startUtc)
            .Include(b => b.BookingLanes)
                .ThenInclude(bl => bl.Lane)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetUpcomingBookingsByLaneAsync(Guid laneId, DateTimeOffset fromTime, CancellationToken cancellationToken = default)
    {
        var fromUtc = fromTime.ToUniversalTime();
        return await DbSet
            .IgnoreQueryFilters()
            .Where(b => b.Status != BookingStatus.Cancelled && b.EndTime >= fromUtc &&
                        b.BookingLanes.Any(bl => bl.LaneId == laneId))
            .Include(b => b.BookingLanes).ThenInclude(bl => bl.Lane)
            .OrderBy(b => b.StartTime)
            .ToListAsync(cancellationToken);
    }
}

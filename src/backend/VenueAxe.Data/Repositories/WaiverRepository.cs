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

public class WaiverRepository : TenantRepository<Waiver>, IWaiverRepository
{
    public WaiverRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<WaiverTemplate?> GetActiveTemplateByVenueSlugAsync(string venueSlug, CancellationToken cancellationToken = default)
    {
        var venue = await Context.Venues
            .IgnoreQueryFilters()
            .Include(v => v.WaiverTemplates.Where(t => t.IsActive))
            .FirstOrDefaultAsync(v => v.Slug.ToLower() == venueSlug.ToLower(), cancellationToken);

        return venue?.WaiverTemplates.OrderByDescending(t => t.VersionNumber).FirstOrDefault();
    }

    public async Task<WaiverTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await Context.WaiverTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == templateId, cancellationToken);
    }

    public async Task<IReadOnlyList<WaiverTemplate>> GetTemplatesByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await Context.WaiverTemplates
            .Where(t => t.VenueId == venueId)
            .OrderByDescending(t => t.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<Waiver?> GetWithDetailsAsync(Guid waiverId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(w => w.Venue)
            .Include(w => w.Template)
            .Include(w => w.Booking)
            .FirstOrDefaultAsync(w => w.Id == waiverId, cancellationToken);
    }

    public async Task<IReadOnlyList<Waiver>> SearchAsync(Guid venueId, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var (items, _) = await SearchPagedAsync(venueId, searchTerm, 1, 50, cancellationToken);
        return items;
    }

    public async Task<(IReadOnlyList<Waiver> Items, int TotalCount)> SearchPagedAsync(
        Guid venueId,
        string? searchTerm,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = DbSet.Where(w => w.VenueId == venueId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lower = searchTerm.Trim().ToLower();
            query = query.Where(w =>
                w.SignerLastName.ToLower().Contains(lower) ||
                w.SignerFirstName.ToLower().Contains(lower) ||
                (w.SignerFirstName + " " + w.SignerLastName).ToLower().Contains(lower) ||
                w.SignerEmail.ToLower().Contains(lower) ||
                w.SignerPhone.ToLower().Contains(lower) ||
                (w.MinorsCoveredJson != null && w.MinorsCoveredJson.ToLower().Contains(lower)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(w => w.SignedAtUtc)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> CountSignedForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await DbSet.IgnoreQueryFilters().CountAsync(w => w.BookingId == bookingId, cancellationToken);
    }
}

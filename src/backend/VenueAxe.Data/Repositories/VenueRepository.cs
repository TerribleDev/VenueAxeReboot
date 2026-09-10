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
public class VenueRepository : TenantRepository<Venue>, IVenueRepository
{
    public VenueRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(v => v.Slug.ToLower() == slug.ToLower() && v.IsActive, cancellationToken);
    }

    public async Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(v => v.BookingConfig)
            .FirstOrDefaultAsync(v => v.Slug.ToLower() == slug.ToLower() && v.IsActive, cancellationToken);
    }

    public async Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(v => v.Lanes.Where(l => l.IsActive))
            .Include(v => v.BookingConfig)
            .FirstOrDefaultAsync(v => v.Id == venueId, cancellationToken);
    }
}

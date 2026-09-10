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
public class LaneRepository : TenantRepository<Lane>, ILaneRepository
{
    public LaneRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = DbSet.IgnoreQueryFilters().Where(l => l.VenueId == venueId);
        if (!includeInactive)
        {
            query = query.Where(l => l.IsActive);
        }
        return await query
            .OrderBy(l => l.LaneNumber)
            .Include(l => l.Sessions.Where(s => s.Status == SessionStatus.Active))
                .ThenInclude(s => s.Matches.Where(m => m.Status == MatchStatus.InProgress))
                    .ThenInclude(m => m.Throws)
            .ToListAsync(cancellationToken);
    }

    public async Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, CancellationToken cancellationToken = default)
    {
        var cleanCode = code.Trim().ToUpper();
        if (isScreen)
        {
            return await DbSet
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(l => l.ScreenPairingCode == cleanCode && l.IsActive, cancellationToken);
        }
        return await DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.TabletPairingCode == cleanCode && l.IsActive, cancellationToken);
    }

    public async Task<Lane?> GetWithActiveSessionAsync(Guid laneId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(l => l.Sessions.Where(s => s.Status == SessionStatus.Active))
                .ThenInclude(s => s.Matches.Where(m => m.Status == MatchStatus.InProgress))
                    .ThenInclude(m => m.Throws)
            .FirstOrDefaultAsync(l => l.Id == laneId, cancellationToken);
    }

    public async Task<Lane?> GetByIdIgnoreQueryFiltersAsync(Guid laneId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(l => l.Id == laneId, cancellationToken);
    }
}

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
public class LaneSessionRepository : TenantRepository<LaneSession>, ILaneSessionRepository
{
    public LaneSessionRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<LaneSession?> GetActiveSessionForLaneAsync(Guid laneId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(s => s.Matches)
                .ThenInclude(m => m.Throws)
            .FirstOrDefaultAsync(s => s.LaneId == laneId && s.Status == SessionStatus.Active, cancellationToken);
    }

    public async Task<GameMatch?> GetActiveMatchWithThrowsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await Context.GameMatches
            .IgnoreQueryFilters()
            .Include(m => m.Throws)
            .FirstOrDefaultAsync(m => m.SessionId == sessionId && m.Status == MatchStatus.InProgress, cancellationToken);
    }

    public async Task AddMatchAsync(GameMatch match, CancellationToken cancellationToken = default)
    {
        await Context.GameMatches.AddAsync(match, cancellationToken);
    }

    public async Task AddMatchThrowAsync(MatchThrow matchThrow, CancellationToken cancellationToken = default)
    {
        await Context.MatchThrows.AddAsync(matchThrow, cancellationToken);
    }

    public Task RemoveMatchThrowAsync(MatchThrow matchThrow, CancellationToken cancellationToken = default)
    {
        Context.MatchThrows.Remove(matchThrow);
        return Task.CompletedTask;
    }
}

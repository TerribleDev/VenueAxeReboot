using System;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Repositories;

public interface ILaneSessionRepository : ITenantRepository<LaneSession>
{
    Task<LaneSession?> GetActiveSessionForLaneAsync(Guid laneId, CancellationToken cancellationToken = default);
    Task<GameMatch?> GetActiveMatchWithThrowsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task AddMatchAsync(GameMatch match, CancellationToken cancellationToken = default);
    Task AddMatchThrowAsync(MatchThrow matchThrow, CancellationToken cancellationToken = default);
    Task RemoveMatchThrowAsync(MatchThrow matchThrow, CancellationToken cancellationToken = default);
}

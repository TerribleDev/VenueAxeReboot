using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Repositories;

public interface ILaneRepository : ITenantRepository<Lane>
{
    Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, bool includeInactive = false, CancellationToken cancellationToken = default);
    Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, CancellationToken cancellationToken = default);
    Task<Lane?> GetWithActiveSessionAsync(Guid laneId, CancellationToken cancellationToken = default);
    Task<Lane?> GetByIdIgnoreQueryFiltersAsync(Guid laneId, CancellationToken cancellationToken = default);
}

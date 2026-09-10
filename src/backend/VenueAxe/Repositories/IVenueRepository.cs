using System;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Repositories;

public interface IVenueRepository : ITenantRepository<Venue>
{
    Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default);
}

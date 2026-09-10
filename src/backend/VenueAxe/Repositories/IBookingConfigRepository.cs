using System;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Repositories;

public interface IBookingConfigRepository : ITenantRepository<BookingConfig>
{
    Task<BookingConfig?> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default);
}

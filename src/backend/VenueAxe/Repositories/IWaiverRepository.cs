using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Repositories;

public interface IWaiverRepository : ITenantRepository<Waiver>
{
    Task<WaiverTemplate?> GetActiveTemplateByVenueSlugAsync(string venueSlug, CancellationToken cancellationToken = default);
    Task<WaiverTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WaiverTemplate>> GetTemplatesByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<Waiver?> GetWithDetailsAsync(Guid waiverId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Waiver>> SearchAsync(Guid venueId, string? searchTerm, CancellationToken cancellationToken = default);
    Task<int> CountSignedForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);
}

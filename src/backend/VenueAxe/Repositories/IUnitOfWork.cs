using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Repositories;

public interface IUnitOfWork
{
    IRepository<Tenant> Tenants { get; }
    IVenueRepository Venues { get; }
    IUserRepository Users { get; }
    ILaneRepository Lanes { get; }
    IBookingRepository Bookings { get; }
    IBookingConfigRepository BookingConfigs { get; }
    IWaiverRepository Waivers { get; }
    ITenantRepository<WaiverTemplate> WaiverTemplates { get; }
    ILaneSessionRepository LaneSessions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

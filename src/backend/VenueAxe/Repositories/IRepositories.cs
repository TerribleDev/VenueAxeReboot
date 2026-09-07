using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
}

public interface ITenantRepository<T> : IRepository<T> where T : TenantEntity
{
    Task<IReadOnlyList<T>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindInTenantAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IVenueRepository : ITenantRepository<Venue>
{
    Task<Venue?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Venue?> GetWithConfigBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Venue?> GetWithLanesAsync(Guid venueId, CancellationToken cancellationToken = default);
}

public interface IUserRepository : ITenantRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface ILaneRepository : ITenantRepository<Lane>
{
    Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<Lane?> GetByPairingCodeAsync(string code, bool isScreen, CancellationToken cancellationToken = default);
    Task<Lane?> GetWithActiveSessionAsync(Guid laneId, CancellationToken cancellationToken = default);
}

public interface IBookingRepository : ITenantRepository<Booking>
{
    Task<Booking?> GetByReferenceAsync(string referenceCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetByVenueAndDateRangeAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
    Task<int> CountOverlappingBookingsAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Booking>> GetOverlappingBookingsWithLanesAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default);
}

public interface IWaiverRepository : ITenantRepository<Waiver>
{
    Task<WaiverTemplate?> GetActiveTemplateByVenueSlugAsync(string venueSlug, CancellationToken cancellationToken = default);
    Task<WaiverTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Waiver>> SearchAsync(Guid venueId, string? searchTerm, CancellationToken cancellationToken = default);
    Task<int> CountSignedForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);
}

public interface ILaneSessionRepository : ITenantRepository<LaneSession>
{
    Task<LaneSession?> GetActiveSessionForLaneAsync(Guid laneId, CancellationToken cancellationToken = default);
    Task<GameMatch?> GetActiveMatchWithThrowsAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task AddMatchThrowAsync(MatchThrow matchThrow, CancellationToken cancellationToken = default);
    Task RemoveMatchThrowAsync(MatchThrow matchThrow, CancellationToken cancellationToken = default);
}

public interface IBookingConfigRepository : ITenantRepository<BookingConfig>
{
    Task<BookingConfig?> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default);
}

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

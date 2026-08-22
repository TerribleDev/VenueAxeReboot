using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.Repositories;

namespace VenueAxe.Data.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly VenueAxeDbContext Context;
    protected readonly DbSet<T> DbSet;

    public Repository(VenueAxeDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }
}

public class TenantRepository<T> : Repository<T>, ITenantRepository<T> where T : TenantEntity
{
    protected readonly IUserContext UserContext;

    public TenantRepository(VenueAxeDbContext context, IUserContext userContext) : base(context)
    {
        UserContext = userContext;
    }

    protected Guid RequiredTenantId => UserContext.TenantId 
        ?? throw new InvalidOperationException("Tenant ID is required for this operation in ITenantRepository.");

    public virtual async Task<IReadOnlyList<T>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(e => e.TenantId == RequiredTenantId).ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindInTenantAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await DbSet.Where(e => e.TenantId == RequiredTenantId).Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task<T?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(e => e.Id == id && e.TenantId == RequiredTenantId, cancellationToken);
    }

    public override async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity.TenantId == Guid.Empty && UserContext.TenantId.HasValue)
        {
            entity.TenantId = UserContext.TenantId.Value;
        }
        return await base.AddAsync(entity, cancellationToken);
    }
}

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

public class UserRepository : TenantRepository<User>, IUserRepository
{
    public UserRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(u => u.Venue)
            .Include(u => u.Tenant)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower() && u.IsActive, cancellationToken);
    }
}

public class LaneRepository : TenantRepository<Lane>, ILaneRepository
{
    public LaneRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<IReadOnlyList<Lane>> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(l => l.VenueId == venueId && l.IsActive)
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
            .Include(l => l.Sessions.Where(s => s.Status == SessionStatus.Active))
                .ThenInclude(s => s.Matches.Where(m => m.Status == MatchStatus.InProgress))
                    .ThenInclude(m => m.Throws)
            .FirstOrDefaultAsync(l => l.Id == laneId, cancellationToken);
    }
}

public class BookingRepository : TenantRepository<Booking>, IBookingRepository
{
    public BookingRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<Booking?> GetByReferenceAsync(string referenceCode, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Include(b => b.BookingLanes).ThenInclude(bl => bl.Lane)
            .Include(b => b.Waivers)
            .FirstOrDefaultAsync(b => b.BookingReference == referenceCode, cancellationToken);
    }

    public async Task<IReadOnlyList<Booking>> GetByVenueAndDateRangeAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(b => b.VenueId == venueId && b.StartTime >= start && b.StartTime <= end)
            .Include(b => b.BookingLanes).ThenInclude(bl => bl.Lane)
            .Include(b => b.Waivers)
            .OrderByDescending(b => b.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountOverlappingBookingsAsync(Guid venueId, DateTimeOffset start, DateTimeOffset end, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .IgnoreQueryFilters()
            .Where(b => b.VenueId == venueId && b.Status != BookingStatus.Cancelled &&
                        b.StartTime < end && b.EndTime > start)
            .CountAsync(cancellationToken);
    }
}

public class WaiverRepository : TenantRepository<Waiver>, IWaiverRepository
{
    public WaiverRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<WaiverTemplate?> GetActiveTemplateByVenueSlugAsync(string venueSlug, CancellationToken cancellationToken = default)
    {
        var venue = await Context.Venues
            .IgnoreQueryFilters()
            .Include(v => v.WaiverTemplates.Where(t => t.IsActive))
            .FirstOrDefaultAsync(v => v.Slug.ToLower() == venueSlug.ToLower(), cancellationToken);

        return venue?.WaiverTemplates.OrderByDescending(t => t.VersionNumber).FirstOrDefault();
    }

    public async Task<WaiverTemplate?> GetTemplateByIdAsync(Guid templateId, CancellationToken cancellationToken = default)
    {
        return await Context.WaiverTemplates.FindAsync(new object[] { templateId }, cancellationToken);
    }

    public async Task<IReadOnlyList<Waiver>> SearchAsync(Guid venueId, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = DbSet.Where(w => w.VenueId == venueId).AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lower = searchTerm.ToLower();
            query = query.Where(w =>
                w.SignerLastName.ToLower().Contains(lower) ||
                w.SignerFirstName.ToLower().Contains(lower) ||
                w.SignerEmail.ToLower().Contains(lower) ||
                w.SignerPhone.Contains(lower));
        }

        return await query.OrderByDescending(w => w.SignedAtUtc).Take(50).ToListAsync(cancellationToken);
    }

    public async Task<int> CountSignedForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await DbSet.CountAsync(w => w.BookingId == bookingId, cancellationToken);
    }
}

public class LaneSessionRepository : TenantRepository<LaneSession>, ILaneSessionRepository
{
    public LaneSessionRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<LaneSession?> GetActiveSessionForLaneAsync(Guid laneId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(s => s.Matches.Where(m => m.Status == MatchStatus.InProgress))
                .ThenInclude(m => m.Throws)
            .FirstOrDefaultAsync(s => s.LaneId == laneId && s.Status == SessionStatus.Active, cancellationToken);
    }

    public async Task<GameMatch?> GetActiveMatchWithThrowsAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await Context.GameMatches
            .Include(m => m.Throws)
            .FirstOrDefaultAsync(m => m.SessionId == sessionId && m.Status == MatchStatus.InProgress, cancellationToken);
    }

    public async Task AddMatchThrowAsync(MatchThrow matchThrow, CancellationToken cancellationToken = default)
    {
        await Context.MatchThrows.AddAsync(matchThrow, cancellationToken);
    }
}

public class BookingConfigRepository : TenantRepository<BookingConfig>, IBookingConfigRepository
{
    public BookingConfigRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<BookingConfig?> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.VenueId == venueId, cancellationToken);
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly VenueAxeDbContext _context;
    private readonly IUserContext _userContext;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(VenueAxeDbContext context, IUserContext userContext)
    {
        _context = context;
        _userContext = userContext;

        Tenants = new Repository<Tenant>(_context);
        Venues = new VenueRepository(_context, _userContext);
        Users = new UserRepository(_context, _userContext);
        Lanes = new LaneRepository(_context, _userContext);
        Bookings = new BookingRepository(_context, _userContext);
        BookingConfigs = new BookingConfigRepository(_context, _userContext);
        Waivers = new WaiverRepository(_context, _userContext);
        WaiverTemplates = new TenantRepository<WaiverTemplate>(_context, _userContext);
        LaneSessions = new LaneSessionRepository(_context, _userContext);
    }

    public IRepository<Tenant> Tenants { get; }
    public IVenueRepository Venues { get; }
    public IUserRepository Users { get; }
    public ILaneRepository Lanes { get; }
    public IBookingRepository Bookings { get; }
    public IBookingConfigRepository BookingConfigs { get; }
    public IWaiverRepository Waivers { get; }
    public ITenantRepository<WaiverTemplate> WaiverTemplates { get; }
    public ILaneSessionRepository LaneSessions { get; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}

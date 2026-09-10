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

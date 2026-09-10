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

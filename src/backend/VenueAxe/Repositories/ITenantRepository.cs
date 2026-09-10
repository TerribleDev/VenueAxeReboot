using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Common;

namespace VenueAxe.Repositories;

public interface ITenantRepository<T> : IRepository<T> where T : TenantEntity
{
    Task<IReadOnlyList<T>> GetForCurrentTenantAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindInTenantAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> GetByIdInTenantAsync(Guid id, CancellationToken cancellationToken = default);
}

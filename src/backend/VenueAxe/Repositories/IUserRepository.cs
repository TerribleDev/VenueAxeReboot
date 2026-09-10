using System.Threading;
using System.Threading.Tasks;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Repositories;

public interface IUserRepository : ITenantRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

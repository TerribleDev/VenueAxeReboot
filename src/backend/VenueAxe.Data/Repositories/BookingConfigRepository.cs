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
public class BookingConfigRepository : TenantRepository<BookingConfig>, IBookingConfigRepository
{
    public BookingConfigRepository(VenueAxeDbContext context, IUserContext userContext) : base(context, userContext) { }

    public async Task<BookingConfig?> GetByVenueIdAsync(Guid venueId, CancellationToken cancellationToken = default)
    {
        return await DbSet.FirstOrDefaultAsync(c => c.VenueId == venueId, cancellationToken);
    }
}

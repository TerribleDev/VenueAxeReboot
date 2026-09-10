using System;

namespace VenueAxe.Domain.Common;

public abstract class VenueScopedEntity : TenantEntity, IVenueEntity
{
    public Guid VenueId { get; set; }
}

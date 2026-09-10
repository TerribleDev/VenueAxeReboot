using System;

namespace VenueAxe.Domain.Common;

public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
}

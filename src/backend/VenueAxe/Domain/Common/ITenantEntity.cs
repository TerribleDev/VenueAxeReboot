using System;

namespace VenueAxe.Domain.Common;

public interface ITenantEntity : IEntity
{
    Guid TenantId { get; set; }
}

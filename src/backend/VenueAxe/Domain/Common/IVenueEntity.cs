using System;

namespace VenueAxe.Domain.Common;

public interface IVenueEntity : ITenantEntity
{
    Guid VenueId { get; set; }
}

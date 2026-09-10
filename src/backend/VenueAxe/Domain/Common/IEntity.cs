using System;

namespace VenueAxe.Domain.Common;

public interface IEntity
{
    Guid Id { get; set; }
    DateTimeOffset CreatedAt { get; set; }
}

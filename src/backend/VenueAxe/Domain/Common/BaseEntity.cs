using System;

namespace VenueAxe.Domain.Common;

public abstract class BaseEntity : IEntity
{
    public Guid Id { get; set; } = UuidV7.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

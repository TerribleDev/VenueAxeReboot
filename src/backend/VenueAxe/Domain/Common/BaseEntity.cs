using System;

namespace VenueAxe.Domain.Common;

/// <summary>
/// Utility for generating RFC 9562 UUID Version 7 identifiers.
/// UUIDv7 combines Unix epoch millisecond timestamp with monotonic random bits,
/// ensuring high-performance database B-tree index locality.
/// </summary>
public static class UuidV7
{
    public static Guid NewGuid() => Guid.CreateVersion7();
}

public interface IEntity
{
    Guid Id { get; set; }
    DateTimeOffset CreatedAt { get; set; }
}

public interface ITenantEntity : IEntity
{
    Guid TenantId { get; set; }
}

public interface IVenueEntity : ITenantEntity
{
    Guid VenueId { get; set; }
}

public abstract class BaseEntity : IEntity
{
    public Guid Id { get; set; } = UuidV7.NewGuid();
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public abstract class TenantEntity : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
}

public abstract class VenueScopedEntity : TenantEntity, IVenueEntity
{
    public Guid VenueId { get; set; }
}

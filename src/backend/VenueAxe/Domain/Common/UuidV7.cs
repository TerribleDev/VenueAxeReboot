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

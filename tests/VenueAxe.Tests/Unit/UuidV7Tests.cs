using System;
using System.Collections.Generic;
using System.Threading;
using VenueAxe.Domain.Common;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class UuidV7Tests
{
    [Fact]
    public void NewGuid_GeneratesNonEmptyGuid()
    {
        var id = UuidV7.NewGuid();
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public void NewGuid_GeneratesChronologicallyMonotonicIds()
    {
        var ids = new List<Guid>();
        for (int i = 0; i < 50; i++)
        {
            ids.Add(UuidV7.NewGuid());
            Thread.Sleep(1); // Small delay to guarantee timestamp monotonicity
        }

        // Verify that all IDs are unique
        var set = new HashSet<Guid>(ids);
        Assert.Equal(50, set.Count);

        // Verify monotonic sorting: each ID should be greater than the previous one when comparing string representations or bytes
        for (int i = 1; i < ids.Count; i++)
        {
            // Compare bytes as big-endian timestamp prefix
            var prevBytes = ids[i - 1].ToByteArray();
            var currBytes = ids[i].ToByteArray();

            // In UUIDv7 string format (RFC 9562), timestamp is encoded in the first 48 bits, ensuring string/lexicographical order matches chronological order
            Assert.True(string.Compare(ids[i - 1].ToString(), ids[i].ToString(), StringComparison.Ordinal) < 0,
                $"Expected {ids[i]} to be lexicographically greater than {ids[i - 1]}");
        }
    }
}

using System;
using System.Threading;
using VenueAxe.Domain.Common;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class SecurityAndIdentityTests
{
    [Fact]
    public void PasswordHelper_HashAndVerify_SucceedsForValidPassword()
    {
        var password = "SecureAxePassword!2026";
        var hash = PasswordHelper.HashPassword(password);

        Assert.NotNull(hash);
        Assert.Contains(".", hash); // Salt.Hash delimiter

        var isValid = PasswordHelper.VerifyPassword(password, hash);
        Assert.True(isValid);
    }

    [Fact]
    public void PasswordHelper_Verify_FailsForIncorrectPassword()
    {
        var hash = PasswordHelper.HashPassword("CorrectPassword123");
        var isValid = PasswordHelper.VerifyPassword("WrongPassword456", hash);

        Assert.False(isValid);
    }

    [Fact]
    public void UuidV7_GeneratedIds_AreMonotonicallyOrdered()
    {
        var id1 = UuidV7.NewGuid();
        Thread.Sleep(2);
        var id2 = UuidV7.NewGuid();
        Thread.Sleep(2);
        var id3 = UuidV7.NewGuid();

        Assert.NotEqual(id1, id2);
        Assert.NotEqual(id2, id3);

        // In UUIDv7 RFC 9562, chronological sort order matches byte order
        var bytes1 = id1.ToByteArray();
        var bytes2 = id2.ToByteArray();
        var bytes3 = id3.ToByteArray();

        Assert.True(string.Compare(id1.ToString(), id2.ToString(), StringComparison.Ordinal) < 0 || id1.CompareTo(id2) <= 0);
    }
}

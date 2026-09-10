using System;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public class ThrowRecord
{
    public string PlayerId { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public TargetZone Zone { get; set; }
    public int PointsAwarded { get; set; }
    public double? X { get; set; }
    public double? Y { get; set; }
    public bool IsKillCalled { get; set; }
    public bool IsClutchCalled
    {
        get => IsKillCalled;
        set => IsKillCalled = value;
    }
    public bool IsBullseye { get; set; }
    public bool IsKillshot { get; set; }
    public DateTimeOffset ThrownAt { get; set; } = DateTimeOffset.UtcNow;
}

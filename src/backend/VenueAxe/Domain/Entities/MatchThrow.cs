using System;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Domain.Entities;

public class MatchThrow : BaseEntity
{
    public Guid MatchId { get; set; }
    public string PlayerId { get; set; } = string.Empty;
    public int RoundNumber { get; set; } = 1;
    public int ThrowNumberInRound { get; set; } = 1;
    public int TotalThrowSequence { get; set; } = 1;
    public TargetZone TargetZone { get; set; } = TargetZone.Miss;
    public double? NormalizedX { get; set; }
    public double? NormalizedY { get; set; }
    public bool IsClutchCalled { get; set; } = false;
    public int PointsAwarded { get; set; } = 0;
    public int? TargetCellIndex { get; set; }
    public DateTimeOffset ThrownAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public GameMatch? Match { get; set; }
}

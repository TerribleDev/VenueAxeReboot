using System;
using System.Collections.Generic;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Domain.Entities;

public class LaneSession : VenueScopedEntity
{
    public Guid LaneId { get; set; }
    public Guid? BookingId { get; set; }

    public string SessionTitle { get; set; } = "Axe Throwing Session";
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; } = DateTimeOffset.UtcNow.AddMinutes(60);
    public DateTimeOffset? EndedAt { get; set; }

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Active player roster [{ id, name, avatarColor, score }])
    /// </summary>
    public string ActiveRosterJson { get; set; } = "[]";

    public Lane? Lane { get; set; }
    public Booking? Booking { get; set; }
    public ICollection<GameMatch> Matches { get; set; } = new List<GameMatch>();
}

public class GameMatch : BaseEntity
{
    public Guid SessionId { get; set; }
    public string GameTypeId { get; set; } = "watl_standard";
    
    /// <summary>
    /// Stored as JSONB in PostgreSQL (Game rules configuration)
    /// </summary>
    public string GameConfigJson { get; set; } = "{}";

    public MatchStatus Status { get; set; } = MatchStatus.InProgress;
    public string? WinnerPlayerId { get; set; }
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }

    public LaneSession? Session { get; set; }
    public ICollection<MatchThrow> Throws { get; set; } = new List<MatchThrow>();
}

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
    public DateTimeOffset ThrownAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public GameMatch? Match { get; set; }
}

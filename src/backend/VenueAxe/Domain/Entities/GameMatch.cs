using System;
using System.Collections.Generic;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Domain.Entities;

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

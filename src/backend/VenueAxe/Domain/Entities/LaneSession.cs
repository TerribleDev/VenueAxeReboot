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

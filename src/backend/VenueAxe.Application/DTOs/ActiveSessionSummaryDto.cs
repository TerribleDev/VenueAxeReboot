using System;
using VenueAxe.GameEngine;

namespace VenueAxe.DTOs;

public record ActiveSessionSummaryDto(
    Guid SessionId,
    string SessionTitle,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    int MinutesRemaining,
    string ActiveRosterJson,
    GameStateSnapshot? CurrentGame
);
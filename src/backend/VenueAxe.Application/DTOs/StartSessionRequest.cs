using System;
using System.Collections.Generic;
using VenueAxe.GameEngine;

namespace VenueAxe.DTOs;

public record StartSessionRequest(
    string SessionTitle,
    int DurationMinutes,
    List<GamePlayer> InitialRoster,
    Guid? BookingId,
    string? GameTypeId = null
);
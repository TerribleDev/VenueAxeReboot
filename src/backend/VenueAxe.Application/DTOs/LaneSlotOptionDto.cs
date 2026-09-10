using System;

namespace VenueAxe.DTOs;

public record LaneSlotOptionDto(
    Guid LaneId,
    int LaneNumber,
    string Name,
    bool IsAvailable,
    string? ConflictReason = null
);
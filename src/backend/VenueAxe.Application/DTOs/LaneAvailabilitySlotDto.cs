using System;

namespace VenueAxe.DTOs;

public record LaneAvailabilitySlotDto(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int AvailableLanesCount,
    bool IsAvailable
);
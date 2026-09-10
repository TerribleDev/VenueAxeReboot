using System;
using System.Collections.Generic;

namespace VenueAxe.DTOs;

public record TimeSlotDto(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsAvailable,
    int AvailableLanesCount,
    int PriceCents,
    List<int> ProposedLaneNumbers
);
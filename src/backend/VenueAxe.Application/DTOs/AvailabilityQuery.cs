using System;

namespace VenueAxe.DTOs;

public record AvailabilityQuery(
    DateOnly Date,
    int PartySize,
    int DurationMinutes,
    string? BookingTypeId = null
);
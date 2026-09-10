using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record ScheduleBookingBlockDto(
    Guid BookingId,
    string BookingReference,
    string GuestName,
    int PartySize,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    BookingStatus Status,
    string PaymentStatus,
    string? BookingTypeId,
    List<int> LaneNumbers,
    int SignedWaiverCount,
    List<Guid>? LaneIds = null
);
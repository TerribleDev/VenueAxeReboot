using System;

namespace VenueAxe.DTOs;

public record NextBookingSummaryDto(
    Guid BookingId,
    string BookingReference,
    string GuestName,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int PartySize,
    int TotalAmountCents = 0,
    int PaidAmountCents = 0,
    string PaymentStatus = "Pending",
    string? Notes = null
);
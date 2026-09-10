using System;
using System.Collections.Generic;

namespace VenueAxe.DTOs;

public record CreateAdminBookingRequest(
    Guid VenueId,
    string GuestFirstName,
    string GuestLastName,
    string? GuestEmail = null,
    string? GuestPhone = null,
    int PartySize = 2,
    DateTimeOffset? StartTime = null,
    int DurationMinutes = 60,
    List<int>? SpecificLaneNumbers = null,
    string PaymentMethod = "Cash",
    string PaymentStatus = "Pending",
    string? Notes = null,
    bool AutoCheckIn = false
);
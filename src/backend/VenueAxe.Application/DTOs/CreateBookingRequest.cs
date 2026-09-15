using System;
using System.Collections.Generic;

namespace VenueAxe.DTOs;

public record CreateBookingRequest(
    string GuestFirstName,
    string GuestLastName,
    string GuestEmail,
    string GuestPhone,
    int PartySize,
    DateTimeOffset StartTime,
    int DurationMinutes,
    string? SelectedPackageId = null,
    string? BookingTypeId = null,
    List<string>? SelectedAddonIds = null,
    string? PromoCode = null,
    string? SquarePaymentSourceId = null,
    string? CustomIntakeResponsesJson = null,
    List<PersonTypeSelectionDto>? PersonTypes = null,
    string? Notes = null,
    bool EmailMarketingOptIn = true
);
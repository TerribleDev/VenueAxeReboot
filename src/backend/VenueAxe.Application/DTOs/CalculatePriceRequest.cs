using System;
using System.Collections.Generic;

namespace VenueAxe.DTOs;

public record CalculatePriceRequest(
    int PartySize,
    int DurationMinutes,
    DateTimeOffset StartTime,
    string? SelectedPackageId = null,
    string? BookingTypeId = null,
    List<string>? SelectedAddonIds = null,
    string? PromoCode = null,
    List<PersonTypeSelectionDto>? PersonTypes = null
);
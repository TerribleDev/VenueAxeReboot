using System;

namespace VenueAxe.DTOs;

public record PublicVenueBookingPageDto(
    Guid VenueId,
    string VenueName,
    string VenueSlug,
    string Currency,
    BookingConfigDto BookingConfig,
    string BrandingConfigJson,
    string Timezone = "America/New_York"
);
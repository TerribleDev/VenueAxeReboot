using System.Collections.Generic;
using VenueAxe.Domain.Common;

namespace VenueAxe.Domain.Entities;

public class Venue : TenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "USA";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string Timezone { get; set; } = "America/New_York";
    public string Currency { get; set; } = "USD";
    public string? StripeAccountId { get; set; }
    public string? IconUrl { get; set; }
    
    /// <summary>
    /// Stored as JSONB in PostgreSQL (Weekly schedule)
    /// </summary>
    public string BusinessHoursJson { get; set; } = "{}";

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Theme colors, logos, fonts)
    /// </summary>
    public string BrandingConfigJson { get; set; } = "{}";

    public bool IsActive { get; set; } = true;

    public Tenant? Tenant { get; set; }
    public ICollection<Lane> Lanes { get; set; } = new List<Lane>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public BookingConfig? BookingConfig { get; set; }
    public ICollection<WaiverTemplate> WaiverTemplates { get; set; } = new List<WaiverTemplate>();
    public ICollection<Waiver> Waivers { get; set; } = new List<Waiver>();
}

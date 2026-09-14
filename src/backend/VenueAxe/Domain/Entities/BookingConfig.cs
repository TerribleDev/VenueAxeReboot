using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Domain.Entities;

public class BookingConfig : VenueScopedEntity
{
    public int MinPartySize { get; set; } = 2;
    public int MaxPartySize { get; set; } = 30;
    public int[] SlotDurationsMinutes { get; set; } = [60, 90, 120];
    public int TurnaroundBufferMinutes { get; set; } = 15;
    public PricingModel PricingModel { get; set; } = PricingModel.PerPerson;
    public int BasePriceCents { get; set; } = 3500; // $35.00
    public int PeakPriceCents { get; set; } = 4500; // $45.00
    public DepositType DepositType { get; set; } = DepositType.FullPayment;
    public int DepositAmountCents { get; set; } = 0;

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Theme colors, hero background, custom fonts)
    /// </summary>
    public string EditorThemeJson { get; set; } = "{}";

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public bool ShowAddress
    {
        get
        {
            if (string.IsNullOrWhiteSpace(EditorThemeJson)) return true;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(EditorThemeJson);
                if (doc.RootElement.TryGetProperty("showAddress", out var prop))
                {
                    return prop.GetBoolean();
                }
            }
            catch {}
            return true;
        }
        set
        {
            try
            {
                var dict = string.IsNullOrWhiteSpace(EditorThemeJson)
                    ? new Dictionary<string, object>()
                    : System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(EditorThemeJson) ?? new();
                dict["showAddress"] = value;
                EditorThemeJson = System.Text.Json.JsonSerializer.Serialize(dict);
            }
            catch
            {
                EditorThemeJson = $"{{\"showAddress\":{value.ToString().ToLower()}}}";
            }
        }
    }

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Custom intake form fields)
    /// </summary>
    public string CustomFieldsJson { get; set; } = "[]";

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Experiences and Packages catalog)
    /// </summary>
    public string PackagesJson { get; set; } = "[]";

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Volume group tiers, promo codes, First Responder discounts)
    /// </summary>
    public string DiscountRulesJson { get; set; } = "[]";

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Booking types and schedule overrides: Standard, Corporate, Private Buyout)
    /// </summary>
    public string BookingTypesJson { get; set; } = "[]";

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Optional Add-ons: Coaching, Drinks, Merchandise)
    /// </summary>
    public string AddonsJson { get; set; } = "[]";

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Person types and rate discounts: Adult, Minor, First Responder, etc.)
    /// </summary>
    public string PersonTypesJson { get; set; } = """
    [
        { "id": "adult", "name": "Adult", "description": "Ages 18+", "discountPercent": 0, "isDefault": true },
        { "id": "minor", "name": "Minor", "description": "Ages 10-17", "discountPercent": 0, "isDefault": false }
    ]
    """;

    public string? CancellationPolicy { get; set; }

    public Venue? Venue { get; set; }
}

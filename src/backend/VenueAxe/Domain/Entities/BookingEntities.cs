using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Custom intake form fields)
    /// </summary>
    public string CustomFieldsJson { get; set; } = "[]";

    /// <summary>
    /// Stored as JSONB in PostgreSQL (Experiences and Add-ons catalog)
    /// </summary>
    public string PackagesJson { get; set; } = "[]";

    public string? CancellationPolicy { get; set; }

    public Venue? Venue { get; set; }
}

public class Booking : VenueScopedEntity
{
    public string BookingReference { get; set; } = string.Empty;
    public BookingStatus Status { get; set; } = BookingStatus.Confirmed;
    public string GuestFirstName { get; set; } = string.Empty;
    public string GuestLastName { get; set; } = string.Empty;
    public string GuestEmail { get; set; } = string.Empty;
    public string GuestPhone { get; set; } = string.Empty;
    public int PartySize { get; set; } = 2;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public int TotalAmountCents { get; set; }
    public int PaidAmountCents { get; set; }
    public string? StripePaymentIntentId { get; set; }
    public string PaymentStatus { get; set; } = "Paid";
    
    /// <summary>
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    public string? CustomIntakeResponsesJson { get; set; }
    public string? Notes { get; set; }

    public Venue? Venue { get; set; }
    public ICollection<BookingLane> BookingLanes { get; set; } = new List<BookingLane>();
    public ICollection<Waiver> Waivers { get; set; } = new List<Waiver>();
    public ICollection<LaneSession> LaneSessions { get; set; } = new List<LaneSession>();
}

public class BookingLane
{
    public Guid BookingId { get; set; }
    public Booking? Booking { get; set; }

    public Guid LaneId { get; set; }
    public Lane? Lane { get; set; }
}

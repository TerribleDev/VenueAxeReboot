using System;
using System.Collections.Generic;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Domain.Entities;

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
    public string? SquarePaymentId { get; set; }
    public string? SquareOrderId { get; set; }
    public string? BookingTypeId { get; set; }
    public int DiscountAmountCents { get; set; } = 0;
    public string? AppliedDiscountCode { get; set; }
    public string PaymentStatus { get; set; } = "Paid";
    
    /// <summary>
    /// Stored as JSONB in PostgreSQL
    /// </summary>
    public string? CustomIntakeResponsesJson { get; set; }
    
    /// <summary>
    /// Stored as JSONB in PostgreSQL (Selected person type counts)
    /// </summary>
    public string? PersonBreakdownJson { get; set; }
    
    public string? Notes { get; set; }

    public Venue? Venue { get; set; }
    public ICollection<BookingLane> BookingLanes { get; set; } = new List<BookingLane>();
    public ICollection<Waiver> Waivers { get; set; } = new List<Waiver>();
    public ICollection<LaneSession> LaneSessions { get; set; } = new List<LaneSession>();
}

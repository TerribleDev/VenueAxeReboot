using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record BookingDto(
    Guid Id,
    Guid VenueId,
    string BookingReference,
    BookingStatus Status,
    string GuestFirstName,
    string GuestLastName,
    string GuestEmail,
    string GuestPhone,
    int PartySize,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int TotalAmountCents,
    int PaidAmountCents,
    string PaymentStatus,
    List<int> AssignedLaneNumbers,
    int SignedWaiverCount,
    string? BookingTypeId = null,
    int DiscountAmountCents = 0,
    string? AppliedDiscountCode = null,
    string? SquarePaymentId = null,
    string? VenueSlug = null,
    bool EmailMarketingOptIn = true
)
{
    public int BalanceDueCents => Math.Max(0, TotalAmountCents - PaidAmountCents);
}
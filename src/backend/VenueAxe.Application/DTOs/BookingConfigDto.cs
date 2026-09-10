using System;
using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record BookingConfigDto(
    Guid Id,
    Guid VenueId,
    int MinPartySize,
    int MaxPartySize,
    int[] SlotDurationsMinutes,
    int TurnaroundBufferMinutes,
    PricingModel PricingModel,
    int BasePriceCents,
    int PeakPriceCents,
    DepositType DepositType,
    int DepositAmountCents,
    string EditorThemeJson,
    string CustomFieldsJson,
    string PackagesJson,
    string DiscountRulesJson,
    string BookingTypesJson,
    string AddonsJson,
    string? CancellationPolicy
);
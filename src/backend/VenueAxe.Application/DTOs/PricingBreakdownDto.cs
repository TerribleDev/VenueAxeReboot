using VenueAxe.Domain.Enums;

namespace VenueAxe.DTOs;

public record PricingBreakdownDto(
    int BaseSubtotalCents,
    int AddonsTotalCents,
    int GrossTotalCents,
    int DiscountAmountCents,
    string? AppliedDiscountDescription,
    int NetTotalCents,
    int DepositDueCents,
    DepositType DepositType
);
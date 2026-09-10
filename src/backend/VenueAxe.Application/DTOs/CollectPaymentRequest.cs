namespace VenueAxe.DTOs;

public record CollectPaymentRequest(
    int? AmountCents = null,
    string? PaymentMethod = null
);
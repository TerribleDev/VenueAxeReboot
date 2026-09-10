namespace VenueAxe.DTOs;

public record SquarePaymentResult(
    bool Success,
    string? PaymentId,
    string? OrderId,
    string? ReceiptUrl,
    string? Status,
    string? ErrorMessage = null
);
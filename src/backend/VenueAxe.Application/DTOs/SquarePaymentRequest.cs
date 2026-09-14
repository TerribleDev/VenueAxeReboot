namespace VenueAxe.DTOs;

public record SquarePaymentRequest(
    string SourceId,
    int AmountCents,
    string Currency,
    string? VerificationToken = null,
    string? CustomerEmail = null,
    string? ReferenceId = null,
    string? CustomAccessToken = null,
    string? CustomLocationId = null,
    string? CustomEnvironment = null
);
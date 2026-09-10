namespace VenueAxe.Services;

public record SendTestEmailResponse(bool Success, string Message, string? Server, int Port, string Sender);
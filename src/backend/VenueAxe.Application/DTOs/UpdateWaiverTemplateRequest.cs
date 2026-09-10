namespace VenueAxe.DTOs;

public record UpdateWaiverTemplateRequest(
    string Title,
    string BodyTextMarkdown,
    bool IsActive = true
);
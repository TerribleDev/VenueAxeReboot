using VenueAxe.Domain.Common;

namespace VenueAxe.Domain.Entities;

public class WaiverTemplate : VenueScopedEntity
{
    public int VersionNumber { get; set; } = 1;
    public string Title { get; set; } = "Participant Release of Liability & Assumption of Risk";
    public string BodyTextMarkdown { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Venue? Venue { get; set; }
}

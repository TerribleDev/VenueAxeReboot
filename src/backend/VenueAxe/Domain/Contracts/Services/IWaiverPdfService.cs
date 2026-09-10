using VenueAxe.Domain.Entities;

namespace VenueAxe.Services;

public interface IWaiverPdfService
{
    byte[] GeneratePdf(Waiver waiver, Venue venue, WaiverTemplate? template);
}

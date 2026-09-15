using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VenueAxe.Application.Services;

public interface IVenueAssetStorageService
{
    Task<string> UploadVenueIconAsync(Guid venueId, Stream fileStream, string contentType, string extension, CancellationToken cancellationToken = default);
    Task DeleteVenueIconAsync(string fileUrl, CancellationToken cancellationToken = default);
}

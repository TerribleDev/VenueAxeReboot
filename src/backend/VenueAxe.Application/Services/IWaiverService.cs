using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;

namespace VenueAxe.Services;
public interface IWaiverService
{
    Task<WaiverTemplateDto?> GetTemplateByVenueSlugAsync(string venueSlug);
    Task<WaiverTemplateDto?> GetTemplateByBookingReferenceAsync(string bookingReference);
    Task<WaiverDto?> SubmitWaiverAsync(SubmitWaiverRequest request, string ipAddress);
    Task<PagedResult<WaiverDto>> SearchWaiversAsync(Guid venueId, string? term, int pageNumber = 1, int pageSize = 20);
    Task<Waiver?> GetWaiverWithDetailsAsync(Guid waiverId);
    Task<IReadOnlyList<WaiverTemplateDto>> GetTemplatesByVenueIdAsync(Guid venueId);
    Task<WaiverTemplateDto?> UpdateTemplateAsync(Guid templateId, UpdateWaiverTemplateRequest request);
    Task<byte[]> ExportWaiversCsvAsync(Guid venueId, CancellationToken cancellationToken = default);
}

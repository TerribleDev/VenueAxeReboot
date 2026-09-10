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
    Task<WaiverDto?> SubmitWaiverAsync(SubmitWaiverRequest request, string ipAddress);
    Task<IReadOnlyList<WaiverDto>> SearchWaiversAsync(Guid venueId, string? term);
    Task<Waiver?> GetWaiverWithDetailsAsync(Guid waiverId);
    Task<IReadOnlyList<WaiverTemplateDto>> GetTemplatesByVenueIdAsync(Guid venueId);
    Task<WaiverTemplateDto?> UpdateTemplateAsync(Guid templateId, UpdateWaiverTemplateRequest request);
}

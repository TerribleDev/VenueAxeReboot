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
public class WaiverService : IWaiverService
{
    private readonly IUnitOfWork _uow;
    private readonly IEmailService? _emailService;
    private readonly ILogger<WaiverService>? _logger;

    public WaiverService(
        IUnitOfWork uow,
        IEmailService? emailService = null,
        ILogger<WaiverService>? logger = null)
    {
        _uow = uow;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Waiver?> GetWaiverWithDetailsAsync(Guid waiverId)
    {
        return await _uow.Waivers.GetWithDetailsAsync(waiverId);
    }

    public async Task<IReadOnlyList<WaiverTemplateDto>> GetTemplatesByVenueIdAsync(Guid venueId)
    {
        var templates = await _uow.Waivers.GetTemplatesByVenueIdAsync(venueId);
        return templates.Select(t => new WaiverTemplateDto(
            t.Id, t.VenueId, t.VersionNumber, t.Title, t.BodyTextMarkdown, t.Sha256Hash
        )).ToList();
    }

    public async Task<WaiverTemplateDto?> UpdateTemplateAsync(Guid templateId, UpdateWaiverTemplateRequest request)
    {
        var template = await _uow.Waivers.GetTemplateByIdAsync(templateId);
        if (template == null) return null;

        var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.BodyTextMarkdown));
        var hashHex = Convert.ToHexString(hashBytes).ToLowerInvariant();

        template.Title = request.Title;
        template.BodyTextMarkdown = request.BodyTextMarkdown;
        template.Sha256Hash = hashHex;
        template.VersionNumber++;
        template.IsActive = request.IsActive;

        await _uow.SaveChangesAsync();

        return new WaiverTemplateDto(
            template.Id,
            template.VenueId,
            template.VersionNumber,
            template.Title,
            template.BodyTextMarkdown,
            template.Sha256Hash
        );
    }

    public async Task<WaiverTemplateDto?> GetTemplateByVenueSlugAsync(string venueSlug)
    {
        var template = await _uow.Waivers.GetActiveTemplateByVenueSlugAsync(venueSlug);
        return template != null ? new WaiverTemplateDto(
            template.Id, template.VenueId, template.VersionNumber, template.Title,
            template.BodyTextMarkdown, template.Sha256Hash
        ) : null;
    }

    public async Task<WaiverDto?> SubmitWaiverAsync(SubmitWaiverRequest request, string ipAddress)
    {
        var template = await _uow.Waivers.GetTemplateByIdAsync(request.TemplateId);
        if (template == null) return null;

        Guid? matchedBookingId = request.BookingId;

        // 1. If BookingReference provided, find booking by reference
        if (!matchedBookingId.HasValue && !string.IsNullOrWhiteSpace(request.BookingReference))
        {
            var b = await _uow.Bookings.GetByReferenceAsync(request.BookingReference.Trim());
            if (b != null && b.VenueId == template.VenueId)
            {
                matchedBookingId = b.Id;
            }
        }

        // 2. If still unlinked, attempt matching by SignerEmail or SignerPhone against today's active bookings
        if (!matchedBookingId.HasValue && !string.IsNullOrWhiteSpace(request.SignerEmail))
        {
            var nowUtc = DateTimeOffset.UtcNow;
            var startUtc = nowUtc.Date;
            var endUtc = startUtc.AddDays(1);
            var todayBookings = await _uow.Bookings.GetByVenueAndDateRangeAsync(template.VenueId, startUtc, endUtc);
            var candidate = todayBookings.FirstOrDefault(b =>
                string.Equals(b.GuestEmail?.Trim(), request.SignerEmail.Trim(), StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(request.SignerPhone) && !string.IsNullOrWhiteSpace(b.GuestPhone) && b.GuestPhone.Trim() == request.SignerPhone.Trim())
            );
            if (candidate != null)
            {
                matchedBookingId = candidate.Id;
            }
        }

        var waiver = new Waiver
        {
            Id = UuidV7.NewGuid(),
            TenantId = template.TenantId,
            VenueId = template.VenueId,
            TemplateId = template.Id,
            BookingId = matchedBookingId,
            SignerFirstName = request.SignerFirstName,
            SignerLastName = request.SignerLastName,
            SignerEmail = request.SignerEmail,
            SignerPhone = request.SignerPhone,
            DateOfBirth = request.DateOfBirth,
            IsGuardianSigning = request.IsGuardianSigning,
            MinorsCoveredJson = request.MinorsCoveredJson,
            SignatureImagePngBase64 = request.SignatureImagePngBase64,
            SignatureVectorSvg = request.SignatureVectorSvg,
            SignedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddYears(1),
            IpAddress = ipAddress,
            UserAgent = request.UserAgent
        };

        await _uow.Waivers.AddAsync(waiver);
        await _uow.SaveChangesAsync();

        if (_emailService != null && !string.IsNullOrWhiteSpace(waiver.SignerEmail))
        {
            var venue = await _uow.Venues.GetByIdAsync(waiver.VenueId);
            if (venue != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendWaiverConfirmationAsync(venue, waiver);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError(ex, "Background error sending waiver confirmation email for waiver {Id}", waiver.Id);
                    }
                });
            }
        }

        return new WaiverDto(
            waiver.Id, waiver.VenueId, waiver.BookingId, waiver.SignerFirstName, waiver.SignerLastName,
            waiver.SignerEmail, waiver.SignerPhone, waiver.DateOfBirth, waiver.IsGuardianSigning,
            waiver.MinorsCoveredJson, waiver.SignatureImagePngBase64, waiver.SignedAtUtc,
            waiver.ExpiresAtUtc, false
        );
    }

    public async Task<IReadOnlyList<WaiverDto>> SearchWaiversAsync(Guid venueId, string? term)
    {
        var list = await _uow.Waivers.SearchAsync(venueId, term);
        return list.Select(w => new WaiverDto(
            w.Id, w.VenueId, w.BookingId, w.SignerFirstName, w.SignerLastName,
            w.SignerEmail, w.SignerPhone, w.DateOfBirth, w.IsGuardianSigning,
            w.MinorsCoveredJson, w.SignatureImagePngBase64, w.SignedAtUtc,
            w.ExpiresAtUtc, w.ExpiresAtUtc < DateTimeOffset.UtcNow
        )).ToList();
    }
}

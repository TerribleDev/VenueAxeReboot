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
public class VenueService : IVenueService
{
    private readonly IUnitOfWork _uow;
    private readonly IUserContext _userContext;

    public VenueService(IUnitOfWork uow, IUserContext userContext)
    {
        _uow = uow;
        _userContext = userContext;
    }

    public async Task<IReadOnlyList<VenueDto>> GetAllVenuesAsync()
    {
        var venues = await _uow.Venues.GetAllAsync();
        return venues.Where(v => v.IsActive).Select(MapVenue).ToList();
    }

    public async Task<VenueDto?> GetVenueByIdAsync(Guid venueId)
    {
        var v = await _uow.Venues.GetByIdAsync(venueId);
        return v != null ? MapVenue(v) : null;
    }

    public async Task<VenueDto> CreateVenueAsync(CreateVenueRequest request)
    {
        if (!_userContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Active tenant context is required to create a venue.");
        }

        var tenantId = _userContext.TenantId.Value;
        string slug = !string.IsNullOrWhiteSpace(request.Slug) 
            ? Slugify(request.Slug) 
            : Slugify(request.Name);

        var venue = new Venue
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenantId,
            Name = request.Name.Trim(),
            Slug = slug,
            AddressLine1 = request.AddressLine1,
            City = request.City,
            State = request.State,
            PostalCode = request.PostalCode,
            Phone = request.Phone,
            Email = request.Email,
            Timezone = request.Timezone,
            Currency = request.Currency,
            BusinessHoursJson = "{\"Monday\":{\"Open\":\"12:00\",\"Close\":\"22:00\"},\"Tuesday\":{\"Open\":\"12:00\",\"Close\":\"22:00\"},\"Wednesday\":{\"Open\":\"12:00\",\"Close\":\"22:00\"},\"Thursday\":{\"Open\":\"12:00\",\"Close\":\"22:00\"},\"Friday\":{\"Open\":\"12:00\",\"Close\":\"23:00\"},\"Saturday\":{\"Open\":\"11:00\",\"Close\":\"23:00\"},\"Sunday\":{\"Open\":\"11:00\",\"Close\":\"21:00\"}}",
            BrandingConfigJson = "{\"PrimaryColor\":\"#f59e0b\",\"AccentColor\":\"#06b6d4\",\"BannerText\":\"Welcome to " + request.Name.Replace("\"", "") + "!\"}",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.Venues.AddAsync(venue);

        // Add default booking config
        var bookingConfig = new BookingConfig
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenantId,
            VenueId = venue.Id,
            SlotDurationsMinutes = [60, 90, 120],
            TurnaroundBufferMinutes = 15,
            MinPartySize = 1,
            MaxPartySize = 24,
            PricingModel = PricingModel.PerPerson,
            BasePriceCents = 3500,
            PeakPriceCents = 4500,
            DepositType = DepositType.FullPayment,
            DepositAmountCents = 3500,
            CancellationPolicy = "Full refund with at least 24 hours notice.",
            PackagesJson = "[{\"id\":\"pkg_std\",\"name\":\"Standard Target Throwing\",\"durationMinutes\":60,\"priceCents\":3500,\"description\":\"60-minute target lane rental with dedicated safety coaching.\"}]",
            CustomFieldsJson = "[]",
            EditorThemeJson = "{\"heroTitle\":\"Book Your Axe Throwing Experience\",\"themePreset\":\"arena\"}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.BookingConfigs.AddAsync(bookingConfig);

        // Add default waiver template
        var waiverTemplate = new WaiverTemplate
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenantId,
            VenueId = venue.Id,
            Title = $"{request.Name} Liability Waiver & Release",
            BodyTextMarkdown = "# Participant Agreement, Release and Assumption of Risk\n\nBy signing this document, I acknowledge that axe throwing involves inherent risks of physical injury. I agree to abide by all venue safety rules and follow all Lane Master instructions at all times.",
            VersionNumber = 1,
            Sha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.WaiverTemplates.AddAsync(waiverTemplate);

        await _uow.SaveChangesAsync();
        return MapVenue(venue);
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];
        var clean = System.Text.RegularExpressions.Regex.Replace(text.ToLower().Trim(), @"[^a-z0-9\s-]", "");
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(clean) ? Guid.NewGuid().ToString("N")[..8] : clean;
    }

    public async Task<VenueDto?> UpdateVenueAsync(Guid venueId, UpdateVenueRequest request)
    {
        var v = await _uow.Venues.GetByIdAsync(venueId);
        if (v == null) return null;

        v.Name = request.Name;
        v.AddressLine1 = request.AddressLine1;
        v.City = request.City;
        v.State = request.State;
        v.PostalCode = request.PostalCode;
        v.Phone = request.Phone;
        v.Email = request.Email;
        v.BusinessHoursJson = request.BusinessHoursJson;
        v.BrandingConfigJson = MergeBrandingConfig(v.BrandingConfigJson, request.BrandingConfigJson);
        v.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.Venues.UpdateAsync(v);
        await _uow.SaveChangesAsync();
        return MapVenue(v);
    }

    public async Task<BookingConfigDto?> GetBookingConfigAsync(Guid venueId)
    {
        var cfg = await _uow.BookingConfigs.GetByVenueIdAsync(venueId);
        return cfg != null ? MapBookingConfig(cfg) : null;
    }

    public async Task<BookingConfigDto?> UpdateBookingConfigAsync(Guid venueId, UpdateBookingConfigRequest request)
    {
        var cfg = await _uow.BookingConfigs.GetByVenueIdAsync(venueId);
        if (cfg == null) return null;

        cfg.MinPartySize = request.MinPartySize;
        cfg.MaxPartySize = request.MaxPartySize;
        cfg.SlotDurationsMinutes = request.SlotDurationsMinutes;
        cfg.TurnaroundBufferMinutes = request.TurnaroundBufferMinutes;
        cfg.PricingModel = request.PricingModel;
        cfg.BasePriceCents = request.BasePriceCents;
        cfg.PeakPriceCents = request.PeakPriceCents;
        cfg.DepositType = request.DepositType;
        cfg.DepositAmountCents = request.DepositAmountCents;
        cfg.EditorThemeJson = request.EditorThemeJson;
        cfg.CustomFieldsJson = request.CustomFieldsJson;
        cfg.PackagesJson = request.PackagesJson;
        cfg.DiscountRulesJson = request.DiscountRulesJson;
        cfg.BookingTypesJson = request.BookingTypesJson;
        cfg.AddonsJson = request.AddonsJson;
        cfg.PersonTypesJson = request.PersonTypesJson;
        cfg.CancellationPolicy = request.CancellationPolicy;
        if (request.ShowAddress.HasValue)
        {
            cfg.ShowAddress = request.ShowAddress.Value;
        }
        cfg.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.BookingConfigs.UpdateAsync(cfg);
        await _uow.SaveChangesAsync();
        return MapBookingConfig(cfg);
    }

    private static string MergeBrandingConfig(string? previousJson, string newJson)
    {
        if (string.IsNullOrWhiteSpace(previousJson) || string.IsNullOrWhiteSpace(newJson))
        {
            return newJson;
        }

        try
        {
            using var prevDoc = JsonDocument.Parse(previousJson);
            string? prevToken = null;
            string? prevWhKey = null;

            if (prevDoc.RootElement.TryGetProperty("payment", out var prevP))
            {
                if (prevP.TryGetProperty("accessToken", out var pt)) prevToken = pt.GetString();
                if (prevP.TryGetProperty("webhookKey", out var pw)) prevWhKey = pw.GetString();
                else if (prevP.TryGetProperty("webhookSignatureKey", out var pw2)) prevWhKey = pw2.GetString();
            }

            if (string.IsNullOrWhiteSpace(prevToken) && string.IsNullOrWhiteSpace(prevWhKey))
            {
                return newJson;
            }

            using var newDoc = JsonDocument.Parse(newJson);
            var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(newJson);
            if (dict != null && dict.TryGetValue("payment", out var pObj))
            {
                var pJson = JsonSerializer.Serialize(pObj);
                var pDict = JsonSerializer.Deserialize<Dictionary<string, object>>(pJson);
                if (pDict != null)
                {
                    if (pDict.TryGetValue("accessToken", out var tokenVal))
                    {
                        var tokenStr = tokenVal?.ToString();
                        if ((string.IsNullOrWhiteSpace(tokenStr) || tokenStr.Contains('•')) && !string.IsNullOrWhiteSpace(prevToken))
                        {
                            pDict["accessToken"] = prevToken;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(prevToken))
                    {
                        pDict["accessToken"] = prevToken;
                    }

                    if (pDict.TryGetValue("webhookKey", out var whVal))
                    {
                        var whStr = whVal?.ToString();
                        if ((string.IsNullOrWhiteSpace(whStr) || whStr.Contains('•')) && !string.IsNullOrWhiteSpace(prevWhKey))
                        {
                            pDict["webhookKey"] = prevWhKey;
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(prevWhKey))
                    {
                        pDict["webhookKey"] = prevWhKey;
                    }

                    dict["payment"] = pDict;
                    return JsonSerializer.Serialize(dict);
                }
            }

            return newJson;
        }
        catch
        {
            return newJson;
        }
    }

    private static VenueSquareConfigDto? ExtractSquareConfig(string brandingConfigJson)
    {
        if (string.IsNullOrWhiteSpace(brandingConfigJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(brandingConfigJson);
            if (!doc.RootElement.TryGetProperty("payment", out var p)) return null;

            string? appId = p.TryGetProperty("appId", out var a) ? a.GetString() : (p.TryGetProperty("applicationId", out var a2) ? a2.GetString() : null);
            string? locId = p.TryGetProperty("locationId", out var l) ? l.GetString() : null;
            string? env = p.TryGetProperty("environment", out var e) ? e.GetString() : "sandbox";
            string? token = p.TryGetProperty("accessToken", out var t) ? t.GetString() : null;
            string? whKey = p.TryGetProperty("webhookKey", out var w) ? w.GetString() : (p.TryGetProperty("webhookSignatureKey", out var w2) ? w2.GetString() : null);

            bool hasToken = !string.IsNullOrWhiteSpace(token);
            string? masked = null;
            if (hasToken)
            {
                masked = token!.Length > 4
                    ? new string('•', Math.Min(token.Length - 4, 16)) + token[^4..]
                    : "••••••••••••••••";
            }

            return new VenueSquareConfigDto(
                ApplicationId: appId,
                LocationId: locId,
                Environment: env,
                HasAccessToken: hasToken,
                MaskedAccessToken: masked,
                WebhookSignatureKey: whKey
            );
        }
        catch
        {
            return null;
        }
    }

    private static VenueDto MapVenue(Venue v) => new(
        v.Id, v.TenantId, v.Name, v.Slug, v.AddressLine1, v.AddressLine2,
        v.City, v.State, v.PostalCode, v.Country, v.Phone, v.Email,
        v.Timezone, v.Currency, v.BusinessHoursJson, v.BrandingConfigJson,
        ExtractSquareConfig(v.BrandingConfigJson)
    );

    private static BookingConfigDto MapBookingConfig(BookingConfig cfg) => new(
        cfg.Id, cfg.VenueId, cfg.MinPartySize, cfg.MaxPartySize, cfg.SlotDurationsMinutes,
        cfg.TurnaroundBufferMinutes, cfg.PricingModel, cfg.BasePriceCents, cfg.PeakPriceCents,
        cfg.DepositType, cfg.DepositAmountCents, cfg.EditorThemeJson, cfg.CustomFieldsJson,
        cfg.PackagesJson, cfg.DiscountRulesJson, cfg.BookingTypesJson, cfg.AddonsJson,
        cfg.PersonTypesJson, cfg.CancellationPolicy, cfg.ShowAddress
    );
}

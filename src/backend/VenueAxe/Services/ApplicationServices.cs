using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;

namespace VenueAxe.Services;

public interface IAuthService
{
    Task<User?> AuthenticateAsync(string email, string password);
    Task<User?> GetCurrentUserAsync();
    Task<User> RegisterTenantAsync(RegisterTenantRequest request);
}

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IUserContext _userContext;

    public AuthService(IUnitOfWork uow, IUserContext userContext)
    {
        _uow = uow;
        _userContext = userContext;
    }

    public async Task<User?> AuthenticateAsync(string email, string password)
    {
        var user = await _uow.Users.GetByEmailAsync(email);
        if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _uow.SaveChangesAsync();
        return user;
    }

    public async Task<User?> GetCurrentUserAsync()
    {
        if (!_userContext.UserId.HasValue) return null;
        return await _uow.Users.GetByIdAsync(_userContext.UserId.Value);
    }

    public async Task<User> RegisterTenantAsync(RegisterTenantRequest request)
    {
        var existingUser = await _uow.Users.GetByEmailAsync(request.Email.Trim().ToLower());
        if (existingUser != null)
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        string tenantSlug = Slugify(request.OrganizationName);
        string venueSlug = Slugify(request.VenueName);

        // 1. Create Tenant
        var tenant = new Tenant
        {
            Id = UuidV7.NewGuid(),
            Name = request.OrganizationName.Trim(),
            Slug = tenantSlug,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.Tenants.AddAsync(tenant);

        // 2. Create Default Venue
        var venue = new Venue
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenant.Id,
            Name = request.VenueName.Trim(),
            Slug = venueSlug,
            AddressLine1 = "100 Axe Way",
            City = !string.IsNullOrWhiteSpace(request.City) ? request.City.Trim() : "Downtown",
            State = "NY",
            PostalCode = "10001",
            Country = "USA",
            Email = request.Email.Trim().ToLower(),
            Timezone = !string.IsNullOrWhiteSpace(request.Timezone) ? request.Timezone : "America/New_York",
            Currency = "USD",
            BusinessHoursJson = "{\"Monday\":{\"Open\":\"12:00\",\"Close\":\"22:00\"},\"Tuesday\":{\"Open\":\"12:00\",\"Close\":\"22:00\"},\"Wednesday\":{\"Open\":\"12:00\",\"Close\":\"22:00\"},\"Thursday\":{\"Open\":\"12:00\",\"Close\":\"22:00\"},\"Friday\":{\"Open\":\"12:00\",\"Close\":\"23:00\"},\"Saturday\":{\"Open\":\"11:00\",\"Close\":\"23:00\"},\"Sunday\":{\"Open\":\"11:00\",\"Close\":\"21:00\"}}",
            BrandingConfigJson = "{\"PrimaryColor\":\"#f59e0b\",\"AccentColor\":\"#06b6d4\",\"BannerText\":\"Welcome to " + request.VenueName.Replace("\"", "") + "!\"}",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.Venues.AddAsync(venue);

        // 3. Create Booking Config
        var bookingConfig = new BookingConfig
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenant.Id,
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
            PackagesJson = "[{\"id\":\"pkg_std\",\"name\":\"Standard Target Throwing\",\"durationMinutes\":60,\"priceCents\":3500,\"description\":\"60-minute target lane rental with dedicated safety coaching.\"},{\"id\":\"pkg_pro\",\"name\":\"Tournament Pro 90\",\"durationMinutes\":90,\"priceCents\":4800,\"description\":\"90-minute competitive match session with full league scoring.\"}]",
            CustomFieldsJson = "[]",
            EditorThemeJson = "{\"heroTitle\":\"Book Your Axe Throwing Experience\",\"themePreset\":\"arena\"}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.BookingConfigs.AddAsync(bookingConfig);

        // 4. Create Waiver Template
        var waiverTemplate = new WaiverTemplate
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenant.Id,
            VenueId = venue.Id,
            Title = $"{request.VenueName} Liability Waiver & Release",
            BodyTextMarkdown = "# Participant Agreement, Release and Assumption of Risk\n\nBy signing this document, I acknowledge that axe throwing involves inherent risks of physical injury. I agree to abide by all venue safety rules and follow all Lane Master instructions at all times.",
            VersionNumber = 1,
            Sha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.WaiverTemplates.AddAsync(waiverTemplate);

        // 6. Create Owner User
        var user = new User
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenant.Id,
            VenueId = venue.Id,
            Email = request.Email.Trim().ToLower(),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            Role = UserRole.Owner,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.Users.AddAsync(user);

        await _uow.SaveChangesAsync();
        return user;
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];
        var clean = System.Text.RegularExpressions.Regex.Replace(text.ToLower().Trim(), @"[^a-z0-9\s-]", "");
        clean = System.Text.RegularExpressions.Regex.Replace(clean, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(clean) ? Guid.NewGuid().ToString("N")[..8] : clean;
    }
}

public interface IVenueService
{
    Task<IReadOnlyList<VenueDto>> GetAllVenuesAsync();
    Task<VenueDto?> GetVenueByIdAsync(Guid venueId);
    Task<VenueDto> CreateVenueAsync(CreateVenueRequest request);
    Task<VenueDto?> UpdateVenueAsync(Guid venueId, UpdateVenueRequest request);
    Task<BookingConfigDto?> GetBookingConfigAsync(Guid venueId);
    Task<BookingConfigDto?> UpdateBookingConfigAsync(Guid venueId, UpdateBookingConfigRequest request);
}

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
        v.BrandingConfigJson = request.BrandingConfigJson;
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
        cfg.CancellationPolicy = request.CancellationPolicy;
        cfg.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.BookingConfigs.UpdateAsync(cfg);
        await _uow.SaveChangesAsync();
        return MapBookingConfig(cfg);
    }

    private static VenueDto MapVenue(Venue v) => new(
        v.Id, v.TenantId, v.Name, v.Slug, v.AddressLine1, v.AddressLine2,
        v.City, v.State, v.PostalCode, v.Country, v.Phone, v.Email,
        v.Timezone, v.Currency, v.BusinessHoursJson, v.BrandingConfigJson
    );

    private static BookingConfigDto MapBookingConfig(BookingConfig cfg) => new(
        cfg.Id, cfg.VenueId, cfg.MinPartySize, cfg.MaxPartySize, cfg.SlotDurationsMinutes,
        cfg.TurnaroundBufferMinutes, cfg.PricingModel, cfg.BasePriceCents, cfg.PeakPriceCents,
        cfg.DepositType, cfg.DepositAmountCents, cfg.EditorThemeJson, cfg.CustomFieldsJson,
        cfg.PackagesJson, cfg.CancellationPolicy
    );
}

public interface ILaneService
{
    Task<IReadOnlyList<LaneDto>> GetLanesForVenueAsync(Guid venueId);
    Task<LaneDto?> GetLaneByIdAsync(Guid laneId);
    Task<LaneDto?> CreateLaneAsync(Guid venueId, CreateLaneRequest request);
    Task<LaneDto?> UpdateLaneAsync(Guid laneId, UpdateLaneRequest request);
    Task<bool> DeleteLaneAsync(Guid laneId);
    Task<bool> UpdateLaneStatusAsync(Guid laneId, LaneStatus status);
    Task<LaneDto?> RegeneratePairingCodesAsync(Guid laneId);
    Task<TerminalAuthResult?> PairTerminalAsync(string pairingCode, string terminalType);
}

public class LaneService : ILaneService
{
    private readonly IUnitOfWork _uow;

    public LaneService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IReadOnlyList<LaneDto>> GetLanesForVenueAsync(Guid venueId)
    {
        var lanes = await _uow.Lanes.GetByVenueIdAsync(venueId);
        return lanes.Select(l =>
        {
            var activeSession = l.Sessions.FirstOrDefault(s => s.Status == SessionStatus.Active);
            ActiveSessionSummaryDto? sessionSummary = null;

            if (activeSession != null)
            {
                var activeMatch = activeSession.Matches.FirstOrDefault(m => m.Status == MatchStatus.InProgress);
                GameStateSnapshot? gameState = null;
                if (activeMatch != null)
                {
                    var engine = GameEngineRegistry.GetEngine(activeMatch.GameTypeId);
                    var players = JsonSerializer.Deserialize<List<GamePlayer>>(activeSession.ActiveRosterJson) ?? new();
                    gameState = engine.Initialize(activeMatch.Id, players);
                    foreach (var t in activeMatch.Throws.OrderBy(x => x.TotalThrowSequence))
                    {
                        gameState = engine.RecordThrow(gameState, t.NormalizedX, t.NormalizedY, t.TargetZone, t.IsClutchCalled);
                    }
                }

                int minsRemaining = Math.Max(0, (int)(activeSession.ExpiresAt - DateTimeOffset.UtcNow).TotalMinutes);

                sessionSummary = new ActiveSessionSummaryDto(
                    activeSession.Id,
                    activeSession.SessionTitle,
                    activeSession.StartedAt,
                    activeSession.ExpiresAt,
                    minsRemaining,
                    activeSession.ActiveRosterJson,
                    gameState
                );
            }

            return new LaneDto(
                l.Id,
                l.VenueId,
                l.LaneNumber,
                l.Name,
                l.MaxThrowers,
                l.CurrentStatus,
                l.TabletPairingCode,
                l.ScreenPairingCode,
                l.LastHeartbeatAt,
                sessionSummary
            );
        }).ToList();
    }

    public async Task<LaneDto?> GetLaneByIdAsync(Guid laneId)
    {
        var lane = await _uow.Lanes.GetByIdAsync(laneId);
        if (lane == null) return null;

        return new LaneDto(
            lane.Id, lane.VenueId, lane.LaneNumber, lane.Name, lane.MaxThrowers,
            lane.CurrentStatus, lane.TabletPairingCode, lane.ScreenPairingCode,
            lane.LastHeartbeatAt, null
        );
    }

    public async Task<LaneDto?> CreateLaneAsync(Guid venueId, CreateLaneRequest request)
    {
        var venue = await _uow.Venues.GetByIdAsync(venueId);
        if (venue == null) return null;

        var random = new Random();
        var lane = new Lane
        {
            Id = UuidV7.NewGuid(),
            TenantId = venue.TenantId,
            VenueId = venueId,
            LaneNumber = request.LaneNumber,
            Name = string.IsNullOrWhiteSpace(request.Name) ? $"Lane {request.LaneNumber:D2}" : request.Name.Trim(),
            MaxThrowers = request.MaxThrowers > 0 ? request.MaxThrowers : 6,
            CurrentStatus = LaneStatus.Available,
            TabletPairingCode = $"AX{random.Next(100, 999)}",
            ScreenPairingCode = $"TV{random.Next(100, 999)}"
        };

        await _uow.Lanes.AddAsync(lane);
        await _uow.SaveChangesAsync();

        return new LaneDto(
            lane.Id, lane.VenueId, lane.LaneNumber, lane.Name, lane.MaxThrowers,
            lane.CurrentStatus, lane.TabletPairingCode, lane.ScreenPairingCode,
            lane.LastHeartbeatAt, null
        );
    }

    public async Task<LaneDto?> UpdateLaneAsync(Guid laneId, UpdateLaneRequest request)
    {
        var lane = await _uow.Lanes.GetByIdAsync(laneId);
        if (lane == null) return null;

        lane.LaneNumber = request.LaneNumber;
        lane.Name = request.Name.Trim();
        lane.MaxThrowers = request.MaxThrowers;
        lane.CurrentStatus = request.Status;
        lane.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.Lanes.UpdateAsync(lane);
        await _uow.SaveChangesAsync();

        return new LaneDto(
            lane.Id, lane.VenueId, lane.LaneNumber, lane.Name, lane.MaxThrowers,
            lane.CurrentStatus, lane.TabletPairingCode, lane.ScreenPairingCode,
            lane.LastHeartbeatAt, null
        );
    }

    public async Task<bool> DeleteLaneAsync(Guid laneId)
    {
        var lane = await _uow.Lanes.GetByIdAsync(laneId);
        if (lane == null) return false;

        await _uow.Lanes.DeleteAsync(lane);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateLaneStatusAsync(Guid laneId, LaneStatus status)
    {
        var lane = await _uow.Lanes.GetByIdAsync(laneId);
        if (lane == null) return false;

        lane.CurrentStatus = status;
        await _uow.Lanes.UpdateAsync(lane);
        await _uow.SaveChangesAsync();
        return true;
    }

    public async Task<LaneDto?> RegeneratePairingCodesAsync(Guid laneId)
    {
        var lane = await _uow.Lanes.GetByIdAsync(laneId);
        if (lane == null) return null;

        var random = new Random();
        lane.TabletPairingCode = $"AX{random.Next(100, 999)}";
        lane.ScreenPairingCode = $"TV{random.Next(100, 999)}";

        await _uow.Lanes.UpdateAsync(lane);
        await _uow.SaveChangesAsync();

        return new LaneDto(
            lane.Id, lane.VenueId, lane.LaneNumber, lane.Name, lane.MaxThrowers,
            lane.CurrentStatus, lane.TabletPairingCode, lane.ScreenPairingCode,
            lane.LastHeartbeatAt, null
        );
    }

    public async Task<TerminalAuthResult?> PairTerminalAsync(string pairingCode, string terminalType)
    {
        bool isScreen = terminalType.Equals("Screen", StringComparison.OrdinalIgnoreCase);
        var lane = await _uow.Lanes.GetByPairingCodeAsync(pairingCode, isScreen);
        if (lane == null) return null;

        var token = Guid.NewGuid().ToString("N");
        if (isScreen) lane.ScreenDeviceToken = token;
        else lane.TabletDeviceToken = token;

        lane.LastHeartbeatAt = DateTimeOffset.UtcNow;
        await _uow.Lanes.UpdateAsync(lane);
        await _uow.SaveChangesAsync();

        return new TerminalAuthResult(lane.Id, lane.LaneNumber, lane.Name, token, terminalType);
    }
}

public interface IBookingService
{
    Task<PublicVenueBookingPageDto?> GetPublicBookingPageAsync(string venueSlug);
    Task<IReadOnlyList<TimeSlotDto>> CheckAvailabilityAsync(string venueSlug, AvailabilityQuery query);
    Task<BookingDto?> CreateGuestBookingAsync(string venueSlug, CreateBookingRequest request);
    Task<IReadOnlyList<BookingDto>> GetVenueBookingsAsync(Guid venueId, DateOnly? date);
    Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status);
}

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;

    public BookingService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PublicVenueBookingPageDto?> GetPublicBookingPageAsync(string venueSlug)
    {
        var venue = await _uow.Venues.GetWithConfigBySlugAsync(venueSlug);
        if (venue == null || venue.BookingConfig == null) return null;

        var cfg = venue.BookingConfig;
        var configDto = new BookingConfigDto(
            cfg.Id, cfg.VenueId, cfg.MinPartySize, cfg.MaxPartySize, cfg.SlotDurationsMinutes,
            cfg.TurnaroundBufferMinutes, cfg.PricingModel, cfg.BasePriceCents, cfg.PeakPriceCents,
            cfg.DepositType, cfg.DepositAmountCents, cfg.EditorThemeJson, cfg.CustomFieldsJson,
            cfg.PackagesJson, cfg.CancellationPolicy
        );

        return new PublicVenueBookingPageDto(
            venue.Id, venue.Name, venue.Slug, venue.Currency, configDto, venue.BrandingConfigJson
        );
    }

    public async Task<IReadOnlyList<TimeSlotDto>> CheckAvailabilityAsync(string venueSlug, AvailabilityQuery query)
    {
        var venue = await _uow.Venues.GetWithConfigBySlugAsync(venueSlug);
        if (venue == null || venue.BookingConfig == null) return Array.Empty<TimeSlotDto>();

        var lanes = await _uow.Lanes.GetByVenueIdAsync(venue.Id);
        int activeLanesCount = lanes.Count;
        int lanesNeeded = (int)Math.Ceiling((double)query.PartySize / 6.0);

        var slots = new List<TimeSlotDto>();
        for (int hour = 12; hour <= 21; hour += Math.Max(1, query.DurationMinutes / 60))
        {
            var start = query.Date.ToDateTime(new TimeOnly(hour, 0), DateTimeKind.Utc);
            var end = start.AddMinutes(query.DurationMinutes);

            int overlapping = await _uow.Bookings.CountOverlappingBookingsAsync(venue.Id, start, end);
            int availableLanes = Math.Max(0, activeLanesCount - overlapping);
            bool isAvail = availableLanes >= lanesNeeded;

            int price = hour >= 17 ? venue.BookingConfig.PeakPriceCents : venue.BookingConfig.BasePriceCents;
            int total = venue.BookingConfig.PricingModel == PricingModel.PerPerson ? price * query.PartySize : price * lanesNeeded;

            slots.Add(new TimeSlotDto(start, end, isAvail, availableLanes, total));
        }

        return slots;
    }

    public async Task<BookingDto?> CreateGuestBookingAsync(string venueSlug, CreateBookingRequest request)
    {
        var venue = await _uow.Venues.GetWithConfigBySlugAsync(venueSlug);
        if (venue == null || venue.BookingConfig == null) return null;

        var lanes = await _uow.Lanes.GetByVenueIdAsync(venue.Id);
        int lanesNeeded = (int)Math.Ceiling((double)request.PartySize / 6.0);
        var availableLanes = lanes.Take(lanesNeeded).ToList();

        var refCode = $"VA-{RandomNumberGenerator.GetInt32(10000, 99999)}";
        var endTime = request.StartTime.AddMinutes(request.DurationMinutes);
        int pricePerPerson = request.StartTime.Hour >= 17 ? venue.BookingConfig.PeakPriceCents : venue.BookingConfig.BasePriceCents;
        int totalCents = pricePerPerson * request.PartySize;

        var booking = new Booking
        {
            Id = UuidV7.NewGuid(),
            TenantId = venue.TenantId,
            VenueId = venue.Id,
            BookingReference = refCode,
            Status = BookingStatus.Confirmed,
            GuestFirstName = request.GuestFirstName,
            GuestLastName = request.GuestLastName,
            GuestEmail = request.GuestEmail,
            GuestPhone = request.GuestPhone,
            PartySize = request.PartySize,
            StartTime = request.StartTime,
            EndTime = endTime,
            TotalAmountCents = totalCents,
            PaidAmountCents = totalCents,
            PaymentStatus = "Paid",
            CustomIntakeResponsesJson = request.CustomIntakeResponsesJson,
            Notes = request.Notes
        };

        foreach (var l in availableLanes)
        {
            booking.BookingLanes.Add(new BookingLane { BookingId = booking.Id, LaneId = l.Id });
        }

        await _uow.Bookings.AddAsync(booking);
        await _uow.SaveChangesAsync();

        return new BookingDto(
            booking.Id, booking.VenueId, booking.BookingReference, booking.Status,
            booking.GuestFirstName, booking.GuestLastName, booking.GuestEmail, booking.GuestPhone,
            booking.PartySize, booking.StartTime, booking.EndTime, booking.TotalAmountCents,
            booking.PaidAmountCents, booking.PaymentStatus, availableLanes.Select(l => l.LaneNumber).ToList(), 0
        );
    }

    public async Task<IReadOnlyList<BookingDto>> GetVenueBookingsAsync(Guid venueId, DateOnly? date)
    {
        var startUtc = date.HasValue ? date.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : DateTimeOffset.UtcNow.AddDays(-30);
        var endUtc = date.HasValue ? date.Value.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc) : DateTimeOffset.UtcNow.AddDays(30);

        var list = await _uow.Bookings.GetByVenueAndDateRangeAsync(venueId, startUtc, endUtc);
        return list.Select(b => new BookingDto(
            b.Id, b.VenueId, b.BookingReference, b.Status, b.GuestFirstName, b.GuestLastName,
            b.GuestEmail, b.GuestPhone, b.PartySize, b.StartTime, b.EndTime, b.TotalAmountCents,
            b.PaidAmountCents, b.PaymentStatus, b.BookingLanes.Select(bl => bl.Lane?.LaneNumber ?? 0).Where(n => n > 0).ToList(),
            b.Waivers.Count
        )).ToList();
    }

    public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId);
        if (booking == null) return false;

        booking.Status = status;
        await _uow.Bookings.UpdateAsync(booking);
        await _uow.SaveChangesAsync();
        return true;
    }
}

public interface IWaiverService
{
    Task<WaiverTemplateDto?> GetTemplateByVenueSlugAsync(string venueSlug);
    Task<WaiverDto?> SubmitWaiverAsync(SubmitWaiverRequest request, string ipAddress);
    Task<IReadOnlyList<WaiverDto>> SearchWaiversAsync(Guid venueId, string? term);
}

public class WaiverService : IWaiverService
{
    private readonly IUnitOfWork _uow;

    public WaiverService(IUnitOfWork uow)
    {
        _uow = uow;
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

        var waiver = new Waiver
        {
            Id = UuidV7.NewGuid(),
            TenantId = template.TenantId,
            VenueId = template.VenueId,
            TemplateId = template.Id,
            BookingId = request.BookingId,
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

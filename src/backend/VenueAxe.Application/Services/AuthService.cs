using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;

namespace VenueAxe.Services;

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
        if (user == null) return null;

        bool isValid = PasswordHelper.VerifyPassword(password, user.PasswordHash);
        if (!isValid)
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
        string baseVenueSlug = Slugify(request.VenueName);
        string venueSlug = baseVenueSlug;
        int counter = 2;
        while (await _uow.Venues.GetBySlugAsync(venueSlug) != null)
        {
            venueSlug = $"{baseVenueSlug}-{counter++}";
        }

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
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        await _uow.BookingConfigs.AddAsync(bookingConfig);

        // 4. New accounts start with 0 lanes by default (configured by operator)

        // 5. Create Default Waiver Template
        var waiverTemplate = new WaiverTemplate
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenant.Id,
            VenueId = venue.Id,
            Title = "Standard Axe Throwing Liability Waiver & Release",
            BodyTextMarkdown = "I acknowledge that axe throwing involves inherent risks of injury, and I voluntarily assume all such risks. I certify that I am in good physical condition and have no medical reason to avoid participating.",
            VersionNumber = 1,
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
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
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
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("n")[..8];
        string slug = text.ToLowerInvariant().Trim();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("n")[..8] : slug;
    }
}

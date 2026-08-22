using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;
using VenueAxe.Domain.Enums;

namespace VenueAxe.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(VenueAxeDbContext context)
    {
        if (await context.Tenants.AnyAsync()) return;

        // 1. Seed Tenant
        var tenantId = UuidV7.NewGuid();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = "Apex Throwing Group",
            Slug = "apex-axes",
            PlanTier = "pro",
            IsActive = true
        };
        context.Tenants.Add(tenant);

        // 2. Seed Venue
        var venueId = UuidV7.NewGuid();
        var venue = new Venue
        {
            Id = venueId,
            TenantId = tenantId,
            Name = "Apex Axe House - Downtown",
            Slug = "downtown",
            AddressLine1 = "100 Timberland Blvd",
            City = "Austin",
            State = "TX",
            PostalCode = "78701",
            Country = "USA",
            Phone = "(512) 555-0199",
            Email = "downtown@apexaxes.com",
            Timezone = "America/Chicago",
            Currency = "USD",
            BusinessHoursJson = """
            {
                "monday": { "isOpen": true, "open": "16:00", "close": "22:00" },
                "tuesday": { "isOpen": true, "open": "16:00", "close": "22:00" },
                "wednesday": { "isOpen": true, "open": "16:00", "close": "22:00" },
                "thursday": { "isOpen": true, "open": "16:00", "close": "23:00" },
                "friday": { "isOpen": true, "open": "14:00", "close": "00:00" },
                "saturday": { "isOpen": true, "open": "11:00", "close": "00:00" },
                "sunday": { "isOpen": true, "open": "12:00", "close": "21:00" }
            }
            """,
            BrandingConfigJson = """
            {
                "primaryColor": "#f59e0b",
                "accentColor": "#06b6d4",
                "backgroundColor": "#0a0c10",
                "surfaceColor": "#131722",
                "fontFamily": "Inter",
                "logoUrl": "/assets/axe-logo.svg",
                "heroHeadline": "The Ultimate Competitive Axe Experience"
            }
            """
        };
        context.Venues.Add(venue);

        // 3. Seed Owner User
        var user = new User
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenantId,
            VenueId = venueId,
            Email = "owner@venueaxe.com",
            PasswordHash = PasswordHelper.HashPassword("password123"),
            FirstName = "Dave",
            LastName = "Miller",
            Role = UserRole.Owner,
            Phone = "(512) 555-0100",
            IsActive = true
        };
        context.Users.Add(user);

        // 4. Seed Lanes (Lanes 1 to 8)
        for (int i = 1; i <= 8; i++)
        {
            var lane = new Lane
            {
                Id = UuidV7.NewGuid(),
                TenantId = tenantId,
                VenueId = venueId,
                LaneNumber = i,
                Name = $"Lane {i:D2}",
                MaxThrowers = 6,
                CurrentStatus = LaneStatus.Available,
                TabletPairingCode = $"AX{100 + i}",
                ScreenPairingCode = $"TV{100 + i}"
            };
            context.Lanes.Add(lane);
        }

        // 5. Seed Booking Config
        var bookingConfig = new BookingConfig
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenantId,
            VenueId = venueId,
            MinPartySize = 2,
            MaxPartySize = 24,
            SlotDurationsMinutes = [60, 90, 120],
            TurnaroundBufferMinutes = 15,
            PricingModel = PricingModel.PerPerson,
            BasePriceCents = 3500,
            PeakPriceCents = 4500,
            DepositType = DepositType.FullPayment,
            EditorThemeJson = """
            {
                "headline": "Book Your Axe Throwing Session",
                "subheadline": "Select your group size, date, and game package",
                "accentColor": "#f59e0b"
            }
            """,
            PackagesJson = """
            [
                {
                    "id": "pkg_standard",
                    "name": "Standard Target Throwing",
                    "description": "60 or 90 minutes of lane time with dedicated Axe Coach orientation",
                    "pricePerPersonCents": 3500,
                    "isDefault": true
                },
                {
                    "id": "pkg_glow",
                    "name": "Glow-in-the-Dark Cosmic Axe",
                    "description": "Blacklight throwing with UV targets and neon glowing axes",
                    "pricePerPersonCents": 4200,
                    "isDefault": false
                },
                {
                    "id": "pkg_vip",
                    "name": "VIP Tournament Battle",
                    "description": "Private bays, drink pitcher, championship trophy, and dedicated referee",
                    "pricePerPersonCents": 5500,
                    "isDefault": false
                }
            ]
            """,
            CustomFieldsJson = """
            [
                { "id": "occasion", "label": "Occasion (Birthday, Corporate, Casual)", "type": "text", "required": false },
                { "id": "experience", "label": "Have you thrown with us before?", "type": "select", "options": ["First Time", "Intermediate", "League Member"] }
            ]
            """,
            CancellationPolicy = "Free cancellations up to 24 hours prior to booking time. No-shows are non-refundable."
        };
        context.BookingConfigs.Add(bookingConfig);

        // 6. Seed Waiver Template
        var waiverTemplate = new WaiverTemplate
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenantId,
            VenueId = venueId,
            VersionNumber = 1,
            Title = "Release and Waiver of Liability, Assumption of Risk, and Indemnity Agreement",
            BodyTextMarkdown = """
            # Participant Safety & Release Agreement

            In consideration of being allowed to participate in axe throwing activities and services provided by **{{VenueName}}**, I hereby agree:

            1. **Assumption of Risk**: I acknowledge that axe throwing is a physical sport involving sharp blades and projectile objects. I knowingly assume all inherent risks of injury.
            2. **Closed-Toe Footwear**: I certify that I and all members of my party are wearing closed-toe shoes.
            3. **Substance & Alcohol Policy**: I agree that {{VenueName}} reserves the right to immediately terminate my session without refund if staff determine I am impaired or behaving unsafely.
            4. **Safety Rules**: I agree to obey all instructions from the Axe Coach / Lane Master at all times.
            5. **Parental Consent (Minors)**: If signing on behalf of a minor, I represent that I am the legal parent or guardian and agree to indemnify and hold harmless the venue.
            """,
            Sha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            IsActive = true
        };
        context.WaiverTemplates.Add(waiverTemplate);

        // ----------------------------------------------------
        // 7. Seed Venue 2 under Tenant 1 ("Apex Axe House - Uptown")
        // ----------------------------------------------------
        var venue2Id = UuidV7.NewGuid();
        var venue2 = new Venue
        {
            Id = venue2Id,
            TenantId = tenantId,
            Name = "Apex Axe House - Uptown Club",
            Slug = "uptown",
            AddressLine1 = "500 North Lamar Blvd",
            City = "Austin",
            State = "TX",
            PostalCode = "78703",
            Country = "USA",
            Phone = "(512) 555-0299",
            Email = "uptown@apexaxes.com",
            Timezone = "America/Chicago",
            Currency = "USD",
            BusinessHoursJson = venue.BusinessHoursJson,
            BrandingConfigJson = venue.BrandingConfigJson,
            IsActive = true
        };
        context.Venues.Add(venue2);

        for (int i = 1; i <= 4; i++)
        {
            context.Lanes.Add(new Lane
            {
                Id = UuidV7.NewGuid(),
                TenantId = tenantId,
                VenueId = venue2Id,
                LaneNumber = i,
                Name = $"Uptown Lane {i:D2}",
                MaxThrowers = 6,
                CurrentStatus = LaneStatus.Available,
                TabletPairingCode = $"UP{100 + i}",
                ScreenPairingCode = $"UT{100 + i}",
                IsActive = true
            });
        }

        context.BookingConfigs.Add(new BookingConfig
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenantId,
            VenueId = venue2Id,
            MinPartySize = 2,
            MaxPartySize = 16,
            SlotDurationsMinutes = [60, 90],
            TurnaroundBufferMinutes = 15,
            PricingModel = PricingModel.PerPerson,
            BasePriceCents = 3800,
            PeakPriceCents = 4800,
            DepositType = DepositType.FullPayment,
            EditorThemeJson = bookingConfig.EditorThemeJson,
            PackagesJson = bookingConfig.PackagesJson,
            CustomFieldsJson = "[]",
            CancellationPolicy = bookingConfig.CancellationPolicy
        });

        // ----------------------------------------------------
        // 8. Seed Tenant 2 ("Valhalla Sports Entertainment")
        // ----------------------------------------------------
        var tenant2Id = UuidV7.NewGuid();
        var tenant2 = new Tenant
        {
            Id = tenant2Id,
            Name = "Valhalla Sports Entertainment",
            Slug = "valhalla-sports",
            PlanTier = "pro",
            IsActive = true
        };
        context.Tenants.Add(tenant2);

        var venue3Id = UuidV7.NewGuid();
        var venue3 = new Venue
        {
            Id = venue3Id,
            TenantId = tenant2Id,
            Name = "Valhalla Axe Lounge",
            Slug = "valhalla",
            AddressLine1 = "777 Viking Way",
            City = "Denver",
            State = "CO",
            PostalCode = "80202",
            Country = "USA",
            Phone = "(303) 555-0777",
            Email = "denver@valhallaaxe.com",
            Timezone = "America/Denver",
            Currency = "USD",
            BusinessHoursJson = venue.BusinessHoursJson,
            BrandingConfigJson = """
            {
                "primaryColor": "#10b981",
                "accentColor": "#8b5cf6",
                "backgroundColor": "#090d16",
                "surfaceColor": "#111827",
                "fontFamily": "Inter",
                "logoUrl": "/assets/valhalla-logo.svg",
                "heroHeadline": "Nordic Axe Throwing & Craft Mead"
            }
            """,
            IsActive = true
        };
        context.Venues.Add(venue3);

        context.Users.Add(new User
        {
            Id = UuidV7.NewGuid(),
            TenantId = tenant2Id,
            VenueId = venue3Id,
            Email = "owner@valhallaaxe.com",
            PasswordHash = PasswordHelper.HashPassword("password123"),
            FirstName = "Erik",
            LastName = "Vance",
            Role = UserRole.Owner,
            Phone = "(303) 555-0101",
            IsActive = true
        });

        for (int i = 1; i <= 6; i++)
        {
            context.Lanes.Add(new Lane
            {
                Id = UuidV7.NewGuid(),
                TenantId = tenant2Id,
                VenueId = venue3Id,
                LaneNumber = i,
                Name = $"Valhalla Lane {i:D2}",
                MaxThrowers = 8,
                CurrentStatus = LaneStatus.Available,
                TabletPairingCode = $"VH{100 + i}",
                ScreenPairingCode = $"VS{100 + i}",
                IsActive = true
            });
        }

        await context.SaveChangesAsync();
    }
}

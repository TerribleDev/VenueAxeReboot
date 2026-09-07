using System;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VenueAxe.Domain.Common;
using VenueAxe.Domain.Entities;

namespace VenueAxe.Data;

public class VenueAxeDbContext : DbContext, IDataProtectionKeyContext
{
    private readonly IUserContext? _userContext;
    public Guid? CurrentTenantId => _userContext?.TenantId;

    public VenueAxeDbContext(DbContextOptions<VenueAxeDbContext> options, IUserContext? userContext = null) : base(options)
    {
        _userContext = userContext;
    }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Lane> Lanes => Set<Lane>();
    public DbSet<BookingConfig> BookingConfigs => Set<BookingConfig>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingLane> BookingLanes => Set<BookingLane>();
    public DbSet<WaiverTemplate> WaiverTemplates => Set<WaiverTemplate>();
    public DbSet<Waiver> Waivers => Set<Waiver>();
    public DbSet<LaneSession> LaneSessions => Set<LaneSession>();
    public DbSet<GameMatch> GameMatches => Set<GameMatch>();
    public DbSet<MatchThrow> MatchThrows => Set<MatchThrow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- Multi-Tenant Global Query Filters ---
        modelBuilder.Entity<Venue>().HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        modelBuilder.Entity<User>().HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Lane>().HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        modelBuilder.Entity<BookingConfig>().HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Booking>().HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        modelBuilder.Entity<WaiverTemplate>().HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Waiver>().HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);
        modelBuilder.Entity<LaneSession>().HasQueryFilter(e => CurrentTenantId != null && e.TenantId == CurrentTenantId);

        // --- Tenant Configuration ---
        modelBuilder.Entity<Tenant>(b =>
        {
            b.ToTable("tenants");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.Slug).IsUnique();
            b.Property(e => e.Name).HasMaxLength(150).IsRequired();
            b.Property(e => e.Slug).HasMaxLength(80).IsRequired();
        });

        // --- Venue Configuration ---
        modelBuilder.Entity<Venue>(b =>
        {
            b.ToTable("venues");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.TenantId, e.Slug }).IsUnique();
            b.Property(e => e.Name).HasMaxLength(150).IsRequired();
            b.Property(e => e.Slug).HasMaxLength(80).IsRequired();
            b.Property(e => e.BusinessHoursJson).HasColumnType("jsonb");
            b.Property(e => e.BrandingConfigJson).HasColumnType("jsonb");

            b.HasOne(e => e.Tenant)
                .WithMany(t => t.Venues)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- User Configuration ---
        modelBuilder.Entity<User>(b =>
        {
            b.ToTable("users");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.TenantId, e.Email }).IsUnique();
            b.Property(e => e.Email).HasMaxLength(150).IsRequired();
            b.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            b.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            b.Property(e => e.Role).HasConversion<string>();

            b.HasOne(e => e.Tenant)
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.TenantId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.Venue)
                .WithMany()
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Lane Configuration ---
        modelBuilder.Entity<Lane>(b =>
        {
            b.ToTable("lanes");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.VenueId, e.LaneNumber }).IsUnique();
            b.Property(e => e.Name).HasMaxLength(100).IsRequired();
            b.Property(e => e.CurrentStatus).HasConversion<string>();

            b.HasOne(e => e.Venue)
                .WithMany(v => v.Lanes)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Booking Config ---
        modelBuilder.Entity<BookingConfig>(b =>
        {
            b.ToTable("booking_configs");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.VenueId).IsUnique();
            b.Property(e => e.PricingModel).HasConversion<string>();
            b.Property(e => e.DepositType).HasConversion<string>();
            b.Property(e => e.EditorThemeJson).HasColumnType("jsonb");
            b.Property(e => e.CustomFieldsJson).HasColumnType("jsonb");
            b.Property(e => e.PackagesJson).HasColumnType("jsonb");
            b.Property(e => e.DiscountRulesJson).HasColumnType("jsonb");
            b.Property(e => e.BookingTypesJson).HasColumnType("jsonb");
            b.Property(e => e.AddonsJson).HasColumnType("jsonb");

            b.HasOne(e => e.Venue)
                .WithOne(v => v.BookingConfig)
                .HasForeignKey<BookingConfig>(e => e.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Booking & BookingLane ---
        modelBuilder.Entity<Booking>(b =>
        {
            b.ToTable("bookings");
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.BookingReference).IsUnique();
            b.HasIndex(e => new { e.VenueId, e.StartTime, e.EndTime });
            b.Property(e => e.Status).HasConversion<string>();
            b.Property(e => e.CustomIntakeResponsesJson).HasColumnType("jsonb");

            b.HasOne(e => e.Venue)
                .WithMany(v => v.Bookings)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BookingLane>(b =>
        {
            b.ToTable("booking_lanes");
            b.HasKey(e => new { e.BookingId, e.LaneId });

            b.HasOne(e => e.Booking)
                .WithMany(b => b.BookingLanes)
                .HasForeignKey(e => e.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.Lane)
                .WithMany(l => l.BookingLanes)
                .HasForeignKey(e => e.LaneId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // --- Waiver Templates & Waivers ---
        modelBuilder.Entity<WaiverTemplate>(b =>
        {
            b.ToTable("waiver_templates");
            b.HasKey(e => e.Id);
            b.Property(e => e.Title).HasMaxLength(200).IsRequired();

            b.HasOne(e => e.Venue)
                .WithMany(v => v.WaiverTemplates)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Waiver>(b =>
        {
            b.ToTable("waivers");
            b.HasKey(e => e.Id);
            b.HasIndex(e => new { e.VenueId, e.SignerLastName, e.SignerEmail });
            b.Property(e => e.MinorsCoveredJson).HasColumnType("jsonb");

            b.HasOne(e => e.Venue)
                .WithMany(v => v.Waivers)
                .HasForeignKey(e => e.VenueId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.Template)
                .WithMany()
                .HasForeignKey(e => e.TemplateId)
                .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(e => e.Booking)
                .WithMany(b => b.Waivers)
                .HasForeignKey(e => e.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // --- Lane Sessions & Game Matches ---
        modelBuilder.Entity<LaneSession>(b =>
        {
            b.ToTable("lane_sessions");
            b.HasKey(e => e.Id);
            b.Property(e => e.Status).HasConversion<string>();
            b.Property(e => e.ActiveRosterJson).HasColumnType("jsonb");

            b.HasOne(e => e.Lane)
                .WithMany(l => l.Sessions)
                .HasForeignKey(e => e.LaneId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(e => e.Booking)
                .WithMany(b => b.LaneSessions)
                .HasForeignKey(e => e.BookingId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<GameMatch>(b =>
        {
            b.ToTable("game_matches");
            b.HasKey(e => e.Id);
            b.Property(e => e.Status).HasConversion<string>();
            b.Property(e => e.GameConfigJson).HasColumnType("jsonb");

            b.HasOne(e => e.Session)
                .WithMany(s => s.Matches)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MatchThrow>(b =>
        {
            b.ToTable("match_throws");
            b.HasKey(e => e.Id);
            b.Property(e => e.TargetZone).HasConversion<string>();

            b.HasOne(e => e.Match)
                .WithMany(m => m.Throws)
                .HasForeignKey(e => e.MatchId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;

namespace VenueAxe.DTOs;

// --- Auth DTOs ---
public record LoginRequest(string Email, string Password);
public record RegisterTenantRequest(
    string OrganizationName,
    string VenueName,
    string FirstName,
    string LastName,
    string Email,
    string Password,
    string? City = null,
    string? Timezone = null
);
public record UserProfileDto(Guid Id, Guid TenantId, Guid? VenueId, string Email, string FirstName, string LastName, UserRole Role, string? VenueName, string? TenantName = null);

// --- Venue & Lane DTOs ---
public record VenueDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Slug,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string State,
    string PostalCode,
    string Country,
    string? Phone,
    string? Email,
    string Timezone,
    string Currency,
    string BusinessHoursJson,
    string BrandingConfigJson
);

public record CreateVenueRequest(
    string Name,
    string? Slug,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string? Phone,
    string? Email,
    string Timezone = "America/New_York",
    string Currency = "USD"
);

public record UpdateVenueRequest(
    string Name,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string? Phone,
    string? Email,
    string BusinessHoursJson,
    string BrandingConfigJson
);

public record LaneDto(
    Guid Id,
    Guid VenueId,
    int LaneNumber,
    string Name,
    int MaxThrowers,
    LaneStatus CurrentStatus,
    string? TabletPairingCode,
    string? ScreenPairingCode,
    DateTimeOffset? LastHeartbeatAt,
    ActiveSessionSummaryDto? ActiveSession
);

public record CreateLaneRequest(
    int LaneNumber,
    string Name,
    int MaxThrowers = 6
);

public record UpdateLaneRequest(
    int LaneNumber,
    string Name,
    int MaxThrowers,
    LaneStatus Status
);

public record ActiveSessionSummaryDto(
    Guid SessionId,
    string SessionTitle,
    DateTimeOffset StartedAt,
    DateTimeOffset ExpiresAt,
    int MinutesRemaining,
    string ActiveRosterJson,
    GameStateSnapshot? CurrentGame
);

public record PairTerminalRequest(string PairingCode, string TerminalType); // "Tablet" or "Screen"
public record TerminalAuthResult(Guid LaneId, int LaneNumber, string LaneName, string DeviceToken, string TerminalType);

public record StartSessionRequest(
    string SessionTitle,
    int DurationMinutes,
    List<GamePlayer> InitialRoster,
    Guid? BookingId,
    string? GameTypeId = null
);

public record ExtendSessionRequest(int ExtraMinutes);

// --- Booking DTOs ---
public record BookingConfigDto(
    Guid Id,
    Guid VenueId,
    int MinPartySize,
    int MaxPartySize,
    int[] SlotDurationsMinutes,
    int TurnaroundBufferMinutes,
    PricingModel PricingModel,
    int BasePriceCents,
    int PeakPriceCents,
    DepositType DepositType,
    int DepositAmountCents,
    string EditorThemeJson,
    string CustomFieldsJson,
    string PackagesJson,
    string DiscountRulesJson,
    string BookingTypesJson,
    string AddonsJson,
    string? CancellationPolicy
);

public record UpdateBookingConfigRequest(
    int MinPartySize,
    int MaxPartySize,
    int[] SlotDurationsMinutes,
    int TurnaroundBufferMinutes,
    PricingModel PricingModel,
    int BasePriceCents,
    int PeakPriceCents,
    DepositType DepositType,
    int DepositAmountCents,
    string EditorThemeJson,
    string CustomFieldsJson,
    string PackagesJson,
    string DiscountRulesJson,
    string BookingTypesJson,
    string AddonsJson,
    string? CancellationPolicy
);

public record PublicVenueBookingPageDto(
    Guid VenueId,
    string VenueName,
    string VenueSlug,
    string Currency,
    BookingConfigDto BookingConfig,
    string BrandingConfigJson
);

public record AvailabilityQuery(
    DateOnly Date,
    int PartySize,
    int DurationMinutes,
    string? BookingTypeId = null
);

public record TimeSlotDto(
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsAvailable,
    int AvailableLanesCount,
    int PriceCents,
    List<int> ProposedLaneNumbers
);

public record CalculatePriceRequest(
    int PartySize,
    int DurationMinutes,
    DateTimeOffset StartTime,
    string? SelectedPackageId = null,
    string? BookingTypeId = null,
    List<string>? SelectedAddonIds = null,
    string? PromoCode = null
);

public record PricingBreakdownDto(
    int BaseSubtotalCents,
    int AddonsTotalCents,
    int GrossTotalCents,
    int DiscountAmountCents,
    string? AppliedDiscountDescription,
    int NetTotalCents,
    int DepositDueCents,
    DepositType DepositType
);

public record CreateBookingRequest(
    string GuestFirstName,
    string GuestLastName,
    string GuestEmail,
    string GuestPhone,
    int PartySize,
    DateTimeOffset StartTime,
    int DurationMinutes,
    string? SelectedPackageId = null,
    string? BookingTypeId = null,
    List<string>? SelectedAddonIds = null,
    string? PromoCode = null,
    string? SquarePaymentSourceId = null,
    string? CustomIntakeResponsesJson = null,
    string? Notes = null
);

public record BookingDto(
    Guid Id,
    Guid VenueId,
    string BookingReference,
    BookingStatus Status,
    string GuestFirstName,
    string GuestLastName,
    string GuestEmail,
    string GuestPhone,
    int PartySize,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    int TotalAmountCents,
    int PaidAmountCents,
    string PaymentStatus,
    List<int> AssignedLaneNumbers,
    int SignedWaiverCount,
    string? BookingTypeId = null,
    int DiscountAmountCents = 0,
    string? AppliedDiscountCode = null,
    string? SquarePaymentId = null
);

public record ScheduleBookingBlockDto(
    Guid BookingId,
    string BookingReference,
    string GuestName,
    int PartySize,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    BookingStatus Status,
    string PaymentStatus,
    string? BookingTypeId,
    List<int> LaneNumbers,
    int SignedWaiverCount
);

public record LaneScheduleMatrixDto(
    DateOnly Date,
    List<LaneDto> Lanes,
    List<ScheduleBookingBlockDto> Bookings,
    string BusinessHoursJson
);

// --- Square Payment DTOs ---
public record SquarePaymentRequest(
    string SourceId,
    int AmountCents,
    string Currency,
    string? VerificationToken = null,
    string? CustomerEmail = null,
    string? ReferenceId = null
);

public record SquarePaymentResult(
    bool Success,
    string? PaymentId,
    string? OrderId,
    string? ReceiptUrl,
    string? Status,
    string? ErrorMessage = null
);

// --- Waiver DTOs ---
public record WaiverTemplateDto(
    Guid Id,
    Guid VenueId,
    int VersionNumber,
    string Title,
    string BodyTextMarkdown,
    string Sha256Hash
);

public record SubmitWaiverRequest(
    Guid TemplateId,
    Guid? BookingId,
    string SignerFirstName,
    string SignerLastName,
    string SignerEmail,
    string SignerPhone,
    DateOnly DateOfBirth,
    bool IsGuardianSigning,
    string? MinorsCoveredJson,
    string SignatureImagePngBase64,
    string? SignatureVectorSvg,
    string UserAgent
);

public record WaiverDto(
    Guid Id,
    Guid VenueId,
    Guid? BookingId,
    string SignerFirstName,
    string SignerLastName,
    string SignerEmail,
    string SignerPhone,
    DateOnly DateOfBirth,
    bool IsGuardianSigning,
    string? MinorsCoveredJson,
    string SignatureImagePngBase64,
    DateTimeOffset SignedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    bool IsExpired
);

// --- Game Telemetry DTOs ---
public record ThrowInputDto(
    double? X,
    double? Y,
    TargetZone? ManualZone,
    bool IsClutchCalled
);

public record SelectGameRequest(
    string GameTypeId,
    GameConfig? Config
);

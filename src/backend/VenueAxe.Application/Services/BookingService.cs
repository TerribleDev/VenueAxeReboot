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
public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;
    private readonly ISquarePaymentService _squarePaymentService;
    private readonly IEmailService? _emailService;
    private readonly ILogger<BookingService> _logger;

    public BookingService(
        IUnitOfWork uow,
        ISquarePaymentService squarePaymentService,
        ILogger<BookingService> logger,
        IEmailService? emailService = null)
    {
        _uow = uow;
        _squarePaymentService = squarePaymentService;
        _logger = logger;
        _emailService = emailService;
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
            cfg.PackagesJson, cfg.DiscountRulesJson, cfg.BookingTypesJson, cfg.AddonsJson,
            cfg.PersonTypesJson, cfg.CancellationPolicy
        );

        return new PublicVenueBookingPageDto(
            venue.Id, venue.Name, venue.Slug, venue.Currency, configDto, venue.BrandingConfigJson, venue.Timezone
        );
    }

    public async Task<IReadOnlyList<TimeSlotDto>> CheckAvailabilityAsync(string venueSlug, AvailabilityQuery query)
    {
        var venue = await _uow.Venues.GetWithConfigBySlugAsync(venueSlug);
        if (venue == null || venue.BookingConfig == null) return Array.Empty<TimeSlotDto>();

        var lanes = await _uow.Lanes.GetByVenueIdAsync(venue.Id);
        if (lanes.Count == 0) return Array.Empty<TimeSlotDto>();

        // 1. Evaluate Operating Hours & Booking Type Overrides
        var (startHour, endHour, isDayAllowed) = ResolveOperatingWindow(venue.BusinessHoursJson, venue.BookingConfig.BookingTypesJson, query.Date, query.BookingTypeId);
        if (!isDayAllowed)
        {
            return Array.Empty<TimeSlotDto>();
        }

        var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);
        var slots = new List<TimeSlotDto>();
        int durationMins = query.DurationMinutes > 0 ? query.DurationMinutes : 60;
        int stepHours = Math.Max(1, durationMins / 60);

        for (int hour = startHour; hour < endHour; hour += stepHours)
        {
            var start = VenueTimeZoneHelper.ToVenueDateTimeOffset(query.Date, hour, 0, tz);
            var end = start.AddMinutes(durationMins);

            // 2. Query active overlapping bookings with assigned lanes
            var overlappingBookings = await _uow.Bookings.GetOverlappingBookingsWithLanesAsync(venue.Id, start, end);

            // 3. Enforce Contiguous Adjacent Lane Allocation
            var allocResult = LaneAllocationEngine.AllocateContiguousLanes(lanes, overlappingBookings, query.PartySize);

            // 4. Calculate slot price
            var pricing = CalculatePricingInternal(venue.BookingConfig, new CalculatePriceRequest(
                query.PartySize, durationMins, start, null, query.BookingTypeId, null, null
            ));

            var proposedLanes = allocResult.IsSuccess
                ? allocResult.AllocatedLanes.Select(l => l.LaneNumber).ToList()
                : new List<int>();

            slots.Add(new TimeSlotDto(
                start,
                end,
                allocResult.IsSuccess,
                allocResult.TotalAvailableLanesCount,
                pricing.NetTotalCents,
                proposedLanes
            ));
        }

        return slots;
    }

    public async Task<PricingBreakdownDto?> CalculatePricingAsync(string venueSlug, CalculatePriceRequest request)
    {
        var venue = await _uow.Venues.GetWithConfigBySlugAsync(venueSlug);
        if (venue == null || venue.BookingConfig == null) return null;

        var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);
        var localStart = VenueTimeZoneHelper.ConvertToVenueTime(request.StartTime, tz);
        var normalizedRequest = request with { StartTime = localStart };

        return CalculatePricingInternal(venue.BookingConfig, normalizedRequest);
    }

    public async Task<BookingDto?> CreateGuestBookingAsync(string venueSlug, CreateBookingRequest request)
    {
        var venue = await _uow.Venues.GetWithConfigBySlugAsync(venueSlug);
        if (venue == null || venue.BookingConfig == null) return null;

        var lanes = await _uow.Lanes.GetByVenueIdAsync(venue.Id);
        if (lanes.Count == 0) return null;

        var endTime = request.StartTime.AddMinutes(request.DurationMinutes);

        // 1. Verify operating window in venue's timezone
        var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);
        var localStartTime = VenueTimeZoneHelper.ConvertToVenueTime(request.StartTime, tz);
        var bookingDate = DateOnly.FromDateTime(localStartTime.DateTime);
        var (startHour, endHour, isDayAllowed) = ResolveOperatingWindow(venue.BusinessHoursJson, venue.BookingConfig.BookingTypesJson, bookingDate, request.BookingTypeId);
        if (!isDayAllowed)
        {
            _logger.LogWarning("Booking creation rejected: venue is closed on {Date} for booking type {BookingType}", bookingDate, request.BookingTypeId);
            return null;
        }

        // 2. Enforce Contiguous Adjacent Lane Allocation
        var overlapping = await _uow.Bookings.GetOverlappingBookingsWithLanesAsync(venue.Id, request.StartTime, endTime);
        var allocResult = LaneAllocationEngine.AllocateContiguousLanes(lanes, overlapping, request.PartySize);

        if (!allocResult.IsSuccess)
        {
            _logger.LogWarning("Contiguous lane allocation failed: {Reason}", allocResult.FailureReason);
            return null;
        }

        // 3. Evaluate Pricing & Discounts (using local start time for peak rate evaluation)
        var priceRequest = new CalculatePriceRequest(
            request.PartySize,
            request.DurationMinutes,
            localStartTime,
            request.SelectedPackageId,
            request.BookingTypeId,
            request.SelectedAddonIds,
            request.PromoCode,
            request.PersonTypes
        );
        var pricing = CalculatePricingInternal(venue.BookingConfig, priceRequest);

        // 4. Process Square Payment (if card source or deposit due)
        SquarePaymentResult? paymentResult = null;
        var refCode = $"VA-{RandomNumberGenerator.GetInt32(10000, 99999)}";

        if (!string.IsNullOrWhiteSpace(request.SquarePaymentSourceId) && pricing.DepositDueCents > 0)
        {
            paymentResult = await _squarePaymentService.ProcessPaymentAsync(new SquarePaymentRequest(
                request.SquarePaymentSourceId,
                pricing.DepositDueCents,
                venue.Currency,
                CustomerEmail: request.GuestEmail,
                ReferenceId: refCode
            ));

            if (!paymentResult.Success)
            {
                _logger.LogWarning("Square checkout payment failed: {Error}", paymentResult.ErrorMessage);
                return null;
            }
        }

        // 5. Create Confirmed Booking Record
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
            TotalAmountCents = pricing.NetTotalCents,
            PaidAmountCents = paymentResult?.Success == true ? pricing.DepositDueCents : (pricing.DepositDueCents == 0 ? pricing.NetTotalCents : 0),
            SquarePaymentId = paymentResult?.PaymentId,
            SquareOrderId = paymentResult?.OrderId,
            BookingTypeId = request.BookingTypeId,
            DiscountAmountCents = pricing.DiscountAmountCents,
            AppliedDiscountCode = request.PromoCode,
            PaymentStatus = (paymentResult?.Success == true ? pricing.DepositDueCents : (pricing.DepositDueCents == 0 ? pricing.NetTotalCents : 0)) >= pricing.NetTotalCents ? "PaidInFull" : "DepositPaid",
            CustomIntakeResponsesJson = request.CustomIntakeResponsesJson,
            PersonBreakdownJson = request.PersonTypes != null && request.PersonTypes.Count > 0 ? JsonSerializer.Serialize(request.PersonTypes) : null,
            Notes = request.Notes
        };

        foreach (var l in allocResult.AllocatedLanes)
        {
            booking.BookingLanes.Add(new BookingLane { BookingId = booking.Id, LaneId = l.Id });
        }

        await _uow.Bookings.AddAsync(booking);
        await _uow.SaveChangesAsync();

        if (_emailService != null && !string.IsNullOrWhiteSpace(booking.GuestEmail))
        {
            var allocatedLaneNumbers = allocResult.AllocatedLanes.Select(l => l.LaneNumber).ToList();
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendBookingConfirmationAsync(venue, booking, allocatedLaneNumbers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background error sending booking confirmation email for {Ref}", booking.BookingReference);
                }
            });
        }

        return new BookingDto(
            booking.Id, booking.VenueId, booking.BookingReference, booking.Status,
            booking.GuestFirstName, booking.GuestLastName, booking.GuestEmail, booking.GuestPhone,
            booking.PartySize, booking.StartTime, booking.EndTime, booking.TotalAmountCents,
            booking.PaidAmountCents, booking.PaymentStatus, allocResult.AllocatedLanes.Select(l => l.LaneNumber).ToList(), 0,
            booking.BookingTypeId, booking.DiscountAmountCents, booking.AppliedDiscountCode, booking.SquarePaymentId
        );
    }

    public async Task<IReadOnlyList<BookingDto>> GetVenueBookingsAsync(Guid venueId, DateOnly? date)
    {
        var venue = await _uow.Venues.GetByIdAsync(venueId);
        var tz = VenueTimeZoneHelper.GetTimeZone(venue?.Timezone);
        DateTimeOffset startUtc;
        DateTimeOffset endUtc;

        if (date.HasValue)
        {
            (startUtc, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(date.Value, tz);
        }
        else
        {
            startUtc = DateTimeOffset.UtcNow.AddDays(-30);
            endUtc = DateTimeOffset.UtcNow.AddDays(30);
        }

        var list = await _uow.Bookings.GetByVenueAndDateRangeAsync(venueId, startUtc, endUtc);
        return list.Select(b => new BookingDto(
            b.Id, b.VenueId, b.BookingReference, b.Status, b.GuestFirstName, b.GuestLastName,
            b.GuestEmail, b.GuestPhone, b.PartySize, b.StartTime, b.EndTime, b.TotalAmountCents,
            b.PaidAmountCents, b.PaymentStatus, b.BookingLanes.Select(bl => bl.Lane?.LaneNumber ?? 0).Where(n => n > 0).ToList(),
            b.Waivers.Count, b.BookingTypeId, b.DiscountAmountCents, b.AppliedDiscountCode, b.SquarePaymentId
        )).ToList();
    }

    public async Task<LaneScheduleMatrixDto?> GetLaneScheduleMatrixAsync(Guid venueId, DateOnly date)
    {
        var venue = await _uow.Venues.GetByIdAsync(venueId);
        if (venue == null) return null;

        var lanes = await _uow.Lanes.GetByVenueIdAsync(venueId, includeInactive: true);
        var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);
        var (startUtc, endUtc) = VenueTimeZoneHelper.GetUtcDayRange(date, tz);

        var bookings = await _uow.Bookings.GetByVenueAndDateRangeAsync(venueId, startUtc, endUtc);

        var laneDtos = lanes.Select(l => new LaneDto(
            l.Id, l.VenueId, l.LaneNumber, l.Name, l.MaxThrowers, l.CurrentStatus,
            l.TabletPairingCode, l.ScreenPairingCode, l.LastHeartbeatAt, null, l.IsActive
        )).ToList();

        var bookingBlocks = bookings.Select(b => new ScheduleBookingBlockDto(
            b.Id,
            b.BookingReference,
            $"{b.GuestFirstName} {b.GuestLastName}".Trim(),
            b.PartySize,
            b.StartTime,
            b.EndTime,
            b.Status,
            b.PaymentStatus,
            b.BookingTypeId,
            b.BookingLanes.Select(bl => bl.Lane?.LaneNumber ?? 0).Where(n => n > 0).OrderBy(n => n).ToList(),
            b.Waivers.Count,
            b.BookingLanes.Select(bl => bl.LaneId).ToList()
        )).ToList();

        return new LaneScheduleMatrixDto(date, laneDtos, bookingBlocks, venue.BusinessHoursJson);
    }

    public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status)
    {
        var booking = await _uow.Bookings.GetByIdAsync(bookingId);
        if (booking == null) return false;

        var prevStatus = booking.Status;
        booking.Status = status;
        await _uow.Bookings.UpdateAsync(booking);
        await _uow.SaveChangesAsync();

        if (status == BookingStatus.Cancelled && prevStatus != BookingStatus.Cancelled && _emailService != null && !string.IsNullOrWhiteSpace(booking.GuestEmail))
        {
            var venue = await _uow.Venues.GetByIdAsync(booking.VenueId);
            if (venue != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _emailService.SendBookingCancellationAsync(venue, booking);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background error sending cancellation email for {Ref}", booking.BookingReference);
                    }
                });
            }
        }

        return true;
    }

    public async Task<BookingDto?> GetBookingByReferenceAsync(string reference)
    {
        var booking = await _uow.Bookings.GetByReferenceAsync(reference);
        if (booking == null) return null;

        var waiverCount = await _uow.Waivers.CountSignedForBookingAsync(booking.Id);
        var laneNumbers = booking.BookingLanes?.Select(bl => bl.Lane?.LaneNumber ?? 0).Where(n => n > 0).ToList() ?? new List<int>();

        return new BookingDto(
            booking.Id,
            booking.VenueId,
            booking.BookingReference,
            booking.Status,
            booking.GuestFirstName,
            booking.GuestLastName,
            booking.GuestEmail,
            booking.GuestPhone,
            booking.PartySize,
            booking.StartTime,
            booking.EndTime,
            booking.TotalAmountCents,
            booking.PaidAmountCents,
            booking.PaymentStatus,
            laneNumbers,
            waiverCount,
            booking.BookingTypeId,
            booking.DiscountAmountCents,
            booking.AppliedDiscountCode,
            booking.SquarePaymentId
        );
    }

    public async Task<bool> ProcessSquareWebhookAsync(string paymentId, string status, string? referenceId, string? orderId, int amountCents)
    {
        Booking? booking = null;
        if (!string.IsNullOrWhiteSpace(referenceId))
        {
            booking = await _uow.Bookings.GetByReferenceAsync(referenceId.Trim());
        }
        if (booking == null && !string.IsNullOrWhiteSpace(paymentId))
        {
            var all = await _uow.Bookings.GetAllAsync();
            booking = all.FirstOrDefault(b => b.SquarePaymentId == paymentId);
        }

        if (booking == null)
        {
            _logger.LogWarning("Square webhook received for unlinked booking (PaymentId: {PaymentId}, Ref: {Ref})", paymentId, referenceId);
            return false;
        }

        if (status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase) || status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
        {
            booking.SquarePaymentId = paymentId;
            if (!string.IsNullOrWhiteSpace(orderId)) booking.SquareOrderId = orderId;
            if (amountCents > 0) booking.PaidAmountCents = Math.Max(booking.PaidAmountCents, amountCents);
            booking.PaymentStatus = booking.PaidAmountCents >= booking.TotalAmountCents ? "PaidInFull" : "DepositPaid";
            booking.Status = BookingStatus.Confirmed;
            await _uow.SaveChangesAsync();
            _logger.LogInformation("Booking {Ref} marked {Status} / {PaymentStatus} via Square webhook", booking.BookingReference, booking.Status, booking.PaymentStatus);
            return true;
        }

        return false;
    }

    public async Task<BookingDto?> CreateAdminBookingAsync(CreateAdminBookingRequest request)
    {
        var venue = await _uow.Venues.GetByIdAsync(request.VenueId);
        if (venue == null) return null;

        var lanes = await _uow.Lanes.GetByVenueIdAsync(venue.Id);
        if (lanes.Count == 0) return null;

        var startTime = request.StartTime ?? DateTimeOffset.UtcNow;
        var duration = request.DurationMinutes <= 0 ? 60 : request.DurationMinutes;
        var endTime = startTime.AddMinutes(duration);

        List<Lane> allocatedLanes = new();
        var overlapping = await _uow.Bookings.GetOverlappingBookingsWithLanesAsync(venue.Id, startTime, endTime);

        if (request.SpecificLaneNumbers != null && request.SpecificLaneNumbers.Count > 0)
        {
            foreach (var num in request.SpecificLaneNumbers)
            {
                var lane = lanes.FirstOrDefault(l => l.LaneNumber == num);
                if (lane == null || !lane.IsActive)
                {
                    _logger.LogWarning("Specified lane {LaneNumber} is deactivated or not found in venue {VenueId}", num, venue.Id);
                    return null;
                }

                bool isBusy = overlapping.Any(b => b.BookingLanes.Any(bl => bl.LaneId == lane.Id));
                if (isBusy)
                {
                    _logger.LogWarning("Specified lane {LaneNumber} is occupied during {StartTime} to {EndTime}", num, startTime, endTime);
                    return null;
                }
                allocatedLanes.Add(lane);
            }
        }
        else
        {
            var allocResult = LaneAllocationEngine.AllocateContiguousLanes(lanes, overlapping, request.PartySize);
            if (!allocResult.IsSuccess)
            {
                _logger.LogWarning("Contiguous lane allocation failed for admin booking: {Reason}", allocResult.FailureReason);
                return null;
            }
            allocatedLanes = allocResult.AllocatedLanes.ToList();
        }

        var cfg = await _uow.BookingConfigs.GetByVenueIdAsync(venue.Id);
        int totalCents = 0;
        int paidCents = 0;

        if (request.PaymentMethod.Equals("Comp", StringComparison.OrdinalIgnoreCase))
        {
            totalCents = 0;
            paidCents = 0;
        }
        else if (cfg != null)
        {
            var tz = VenueTimeZoneHelper.GetTimeZone(venue.Timezone);
            var localStart = VenueTimeZoneHelper.ConvertToVenueTime(startTime, tz);
            var priceRequest = new CalculatePriceRequest(
                request.PartySize,
                duration,
                localStart
            );
            var pricing = CalculatePricingInternal(cfg, priceRequest);
            totalCents = pricing.NetTotalCents;

            if (request.PaymentStatus.Equals("PaidInFull", StringComparison.OrdinalIgnoreCase))
            {
                paidCents = totalCents;
            }
            else if (request.PaymentStatus.Equals("DepositPaid", StringComparison.OrdinalIgnoreCase))
            {
                paidCents = pricing.DepositDueCents;
            }
            else
            {
                paidCents = 0;
            }
        }

        var refCode = $"WA-{RandomNumberGenerator.GetInt32(10000, 99999)}";
        var status = request.AutoCheckIn ? BookingStatus.CheckedIn : BookingStatus.Confirmed;

        var booking = new Booking
        {
            Id = UuidV7.NewGuid(),
            TenantId = venue.TenantId,
            VenueId = venue.Id,
            BookingReference = refCode,
            Status = status,
            GuestFirstName = request.GuestFirstName,
            GuestLastName = request.GuestLastName,
            GuestEmail = request.GuestEmail ?? $"walkin-{refCode.ToLowerInvariant()}@venueaxe.local",
            GuestPhone = request.GuestPhone ?? "N/A",
            PartySize = request.PartySize,
            StartTime = startTime,
            EndTime = endTime,
            TotalAmountCents = totalCents,
            PaidAmountCents = paidCents,
            PaymentStatus = request.PaymentStatus,
            Notes = string.IsNullOrWhiteSpace(request.Notes)
                ? $"Staff Walk-In / Phone Entry ({request.PaymentMethod})"
                : $"{request.Notes} ({request.PaymentMethod})"
        };

        foreach (var l in allocatedLanes)
        {
            booking.BookingLanes.Add(new BookingLane
            {
                BookingId = booking.Id,
                LaneId = l.Id
            });
        }

        await _uow.Bookings.AddAsync(booking);
        await _uow.SaveChangesAsync();

        if (_emailService != null && !string.IsNullOrWhiteSpace(booking.GuestEmail))
        {
            var allocatedLaneNumbers = allocatedLanes.Select(l => l.LaneNumber).ToList();
            _ = Task.Run(async () =>
            {
                try
                {
                    await _emailService.SendBookingConfirmationAsync(venue, booking, allocatedLaneNumbers);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background error sending admin booking confirmation email for {Ref}", booking.BookingReference);
                }
            });
        }

        return new BookingDto(
            booking.Id, booking.VenueId, booking.BookingReference, booking.Status,
            booking.GuestFirstName, booking.GuestLastName, booking.GuestEmail, booking.GuestPhone,
            booking.PartySize, booking.StartTime, booking.EndTime, booking.TotalAmountCents,
            booking.PaidAmountCents, booking.PaymentStatus, allocatedLanes.Select(l => l.LaneNumber).ToList(), 0,
            booking.BookingTypeId, booking.DiscountAmountCents, booking.AppliedDiscountCode, booking.SquarePaymentId
        );
    }

    public async Task<BookingDto?> ReassignBookingLaneAsync(Guid bookingId, Guid targetLaneId)
    {
        var booking = await _uow.Bookings.GetByIdWithLanesAsync(bookingId);
        if (booking == null) return null;

        var targetLane = await _uow.Lanes.GetByIdAsync(targetLaneId);
        if (targetLane == null || !targetLane.IsActive) throw new InvalidOperationException("Target lane does not exist or is deactivated.");

        var cfg = await _uow.BookingConfigs.GetByVenueIdAsync(booking.VenueId);
        int buffer = cfg?.TurnaroundBufferMinutes ?? 0;
        var bufferedStart = booking.StartTime.AddMinutes(-buffer);
        var bufferedEnd = booking.EndTime.AddMinutes(buffer);

        var overlapping = await _uow.Bookings.GetOverlappingBookingsWithLanesAsync(booking.VenueId, bufferedStart, bufferedEnd);
        bool conflict = overlapping.Any(b => b.Id != booking.Id && b.BookingLanes.Any(bl => bl.LaneId == targetLaneId));
        if (conflict)
        {
            throw new InvalidOperationException($"Lane {targetLane.LaneNumber} already has a conflicting reservation during that timeslot.");
        }

        booking.BookingLanes.Clear();
        booking.BookingLanes.Add(new BookingLane
        {
            BookingId = booking.Id,
            LaneId = targetLaneId,
            Lane = targetLane
        });
        booking.UpdatedAt = DateTimeOffset.UtcNow;

        await _uow.Bookings.UpdateAsync(booking);
        await _uow.SaveChangesAsync();

        var refreshed = await _uow.Bookings.GetByIdWithLanesAsync(bookingId);
        if (refreshed == null) return null;

        return new BookingDto(
            refreshed.Id, refreshed.VenueId, refreshed.BookingReference, refreshed.Status,
            refreshed.GuestFirstName, refreshed.GuestLastName, refreshed.GuestEmail, refreshed.GuestPhone,
            refreshed.PartySize, refreshed.StartTime, refreshed.EndTime, refreshed.TotalAmountCents,
            refreshed.PaidAmountCents, refreshed.PaymentStatus,
            refreshed.BookingLanes.Select(bl => bl.Lane?.LaneNumber ?? 0).Where(n => n > 0).ToList(),
            refreshed.Waivers.Count, refreshed.BookingTypeId, refreshed.DiscountAmountCents,
            refreshed.AppliedDiscountCode, refreshed.SquarePaymentId
        );
    }

    public async Task<BookingDto?> CollectPaymentAsync(Guid bookingId, int? amountCents, string? paymentMethod)
    {
        var booking = await _uow.Bookings.GetByIdWithLanesAsync(bookingId);
        if (booking == null) return null;

        var remainingCents = Math.Max(0, booking.TotalAmountCents - booking.PaidAmountCents);
        var amountToAdd = amountCents.HasValue && amountCents.Value > 0 ? amountCents.Value : remainingCents;

        booking.PaidAmountCents += amountToAdd;
        booking.PaymentStatus = booking.PaidAmountCents >= booking.TotalAmountCents ? "PaidInFull" : "DepositPaid";
        if (booking.Status == BookingStatus.Pending)
        {
            booking.Status = BookingStatus.Confirmed;
        }

        await _uow.SaveChangesAsync();

        var laneNumbers = booking.BookingLanes.Select(bl => bl.Lane?.LaneNumber ?? 0).Where(n => n > 0).ToList();
        return new BookingDto(
            booking.Id, booking.VenueId, booking.BookingReference, booking.Status,
            booking.GuestFirstName, booking.GuestLastName, booking.GuestEmail, booking.GuestPhone,
            booking.PartySize, booking.StartTime, booking.EndTime, booking.TotalAmountCents,
            booking.PaidAmountCents, booking.PaymentStatus, laneNumbers,
            booking.Waivers.Count, booking.BookingTypeId, booking.DiscountAmountCents,
            booking.AppliedDiscountCode, booking.SquarePaymentId
        );
    }

    // --- Pricing & Discount Calculation Helpers ---
    public static PricingBreakdownDto CalculatePricingInternal(BookingConfig cfg, CalculatePriceRequest req)
    {
        // 1. Base Price
        int unitPriceCents = req.StartTime.Hour >= 17 ? cfg.PeakPriceCents : cfg.BasePriceCents;

        if (!string.IsNullOrWhiteSpace(req.SelectedPackageId) && !string.IsNullOrWhiteSpace(cfg.PackagesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(cfg.PackagesJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("id", out var idProp) && idProp.GetString() == req.SelectedPackageId)
                        {
                            if (item.TryGetProperty("pricePerPersonCents", out var pProp))
                            {
                                unitPriceCents = pProp.GetInt32();
                            }
                            break;
                        }
                    }
                }
            }
            catch {}
        }

        int baseSubtotal = cfg.PricingModel == PricingModel.PerPerson
            ? unitPriceCents * req.PartySize
            : unitPriceCents * (int)Math.Ceiling((double)req.PartySize / 6.0);

        // 2. Add-ons Total
        int addonsTotal = 0;
        if (req.SelectedAddonIds != null && req.SelectedAddonIds.Count > 0 && !string.IsNullOrWhiteSpace(cfg.AddonsJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(cfg.AddonsJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.TryGetProperty("id", out var idProp) && req.SelectedAddonIds.Contains(idProp.GetString() ?? ""))
                        {
                            int itemPrice = item.TryGetProperty("priceCents", out var pProp) ? pProp.GetInt32() : 0;
                            string priceType = item.TryGetProperty("priceType", out var ptProp) ? ptProp.GetString() ?? "flat" : "flat";

                            addonsTotal += priceType.Equals("per_person", StringComparison.OrdinalIgnoreCase)
                                ? itemPrice * req.PartySize
                                : itemPrice;
                        }
                    }
                }
            }
            catch {}
        }

        int grossTotal = baseSubtotal + addonsTotal;

        // 3. Evaluate Person-Type Specific Rate Discounts
        int personTypeDiscountAmount = 0;
        var personDiscountDescriptions = new List<string>();

        if (req.PersonTypes != null && req.PersonTypes.Count > 0 && !string.IsNullOrWhiteSpace(cfg.PersonTypesJson))
        {
            try
            {
                using var pdoc = JsonDocument.Parse(cfg.PersonTypesJson);
                if (pdoc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var configTypes = new Dictionary<string, (string Name, int DiscountPercent, int FlatDiscountCents)>(StringComparer.OrdinalIgnoreCase);
                    foreach (var item in pdoc.RootElement.EnumerateArray())
                    {
                        var tid = item.TryGetProperty("id", out var idProp) ? idProp.GetString() ?? "" : "";
                        var tname = item.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? tid : tid;
                        var tpercent = item.TryGetProperty("discountPercent", out var dpProp) ? dpProp.GetInt32() : 0;
                        var tflat = item.TryGetProperty("discountAmountCents", out var daProp) ? daProp.GetInt32() : 0;
                        if (!string.IsNullOrWhiteSpace(tid))
                        {
                            configTypes[tid] = (tname, tpercent, tflat);
                        }
                    }

                    foreach (var selection in req.PersonTypes)
                    {
                        if (selection.Count <= 0) continue;
                        if (configTypes.TryGetValue(selection.PersonTypeId, out var pinfo))
                        {
                            int singleDiscount = pinfo.DiscountPercent > 0
                                ? (unitPriceCents * pinfo.DiscountPercent) / 100
                                : pinfo.FlatDiscountCents;

                            if (singleDiscount > 0)
                            {
                                int totalTypeDiscount = singleDiscount * selection.Count;
                                personTypeDiscountAmount += totalTypeDiscount;
                                personDiscountDescriptions.Add($"{pinfo.Name} ({pinfo.DiscountPercent}% off x{selection.Count})");
                            }
                        }
                    }
                }
            }
            catch {}
        }

        // 4. Evaluate Configurable Discount Rules & Volume Tiers
        int ruleDiscountAmount = 0;
        string? appliedRuleDesc = null;

        if (!string.IsNullOrWhiteSpace(cfg.DiscountRulesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(cfg.DiscountRulesJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        string name = item.TryGetProperty("name", out var nProp) ? nProp.GetString() ?? "Discount" : "Discount";
                        string type = item.TryGetProperty("type", out var tProp) ? tProp.GetString() ?? "group_size" : "group_size";
                        int percent = item.TryGetProperty("discountPercent", out var dpProp) ? dpProp.GetInt32() : 0;
                        int flatCents = item.TryGetProperty("discountAmountCents", out var daProp) ? daProp.GetInt32() : 0;
                        bool autoApply = item.TryGetProperty("autoApply", out var aaProp) && aaProp.GetBoolean();
                        string? code = item.TryGetProperty("code", out var cProp) ? cProp.GetString() : null;
                        int minParty = item.TryGetProperty("minPartySize", out var mpProp) ? mpProp.GetInt32() : 0;

                        bool matches = false;

                        // Volume Group Tier
                        if (type.Equals("group_size", StringComparison.OrdinalIgnoreCase) && (autoApply || !string.IsNullOrWhiteSpace(req.PromoCode)))
                        {
                            if (req.PartySize >= minParty) matches = true;
                        }
                        // Promo Code or Categorical (e.g. HERO10 First Responder)
                        else if (!string.IsNullOrWhiteSpace(code) && !string.IsNullOrWhiteSpace(req.PromoCode))
                        {
                            if (code.Trim().Equals(req.PromoCode.Trim(), StringComparison.OrdinalIgnoreCase)) matches = true;
                        }

                        if (matches)
                        {
                            int calculated = percent > 0 ? (grossTotal * percent) / 100 : flatCents;
                            if (calculated > ruleDiscountAmount)
                            {
                                ruleDiscountAmount = calculated;
                                appliedRuleDesc = percent > 0 ? $"{name} ({percent}% off)" : $"{name} (${flatCents / 100} off)";
                            }
                        }
                    }
                }
            }
            catch {}
        }

        int totalDiscountAmount = personTypeDiscountAmount + ruleDiscountAmount;
        string? appliedDiscountDesc = null;
        if (personDiscountDescriptions.Count > 0 && !string.IsNullOrWhiteSpace(appliedRuleDesc))
        {
            appliedDiscountDesc = $"{string.Join(", ", personDiscountDescriptions)} + {appliedRuleDesc}";
        }
        else if (personDiscountDescriptions.Count > 0)
        {
            appliedDiscountDesc = string.Join(", ", personDiscountDescriptions);
        }
        else
        {
            appliedDiscountDesc = appliedRuleDesc;
        }

        int netTotal = Math.Max(0, grossTotal - totalDiscountAmount);

        // 4. Deposit Due Calculation
        int depositDue = cfg.DepositType switch
        {
            DepositType.FullPayment => netTotal,
            DepositType.FixedDeposit => Math.Min(netTotal, cfg.DepositAmountCents),
            DepositType.PerPersonDeposit => Math.Min(netTotal, cfg.DepositAmountCents * req.PartySize),
            _ => netTotal
        };

        return new PricingBreakdownDto(
            baseSubtotal,
            addonsTotal,
            grossTotal,
            totalDiscountAmount,
            appliedDiscountDesc,
            netTotal,
            depositDue,
            cfg.DepositType
        );
    }

    // --- Operating Hours & Schedule Override Resolver ---
    private static (int StartHour, int EndHour, bool IsDayAllowed) ResolveOperatingWindow(
        string businessHoursJson,
        string bookingTypesJson,
        DateOnly date,
        string? bookingTypeId)
    {
        bool allowAfterHours = false;
        bool allowOffDays = false;

        if (!string.IsNullOrWhiteSpace(bookingTypeId) && !string.IsNullOrWhiteSpace(bookingTypesJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(bookingTypesJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var bt in doc.RootElement.EnumerateArray())
                    {
                        if (bt.TryGetProperty("id", out var idProp) && idProp.GetString() == bookingTypeId)
                        {
                            allowAfterHours = bt.TryGetProperty("allowAfterHoursBooking", out var ahProp) && ahProp.GetBoolean();
                            allowOffDays = bt.TryGetProperty("allowOffDaysBooking", out var odProp) && odProp.GetBoolean();
                            break;
                        }
                    }
                }
            }
            catch {}
        }

        string dayKey = date.DayOfWeek.ToString().ToLowerInvariant();
        bool isOpen = true;
        int startHour = 12;
        int endHour = 22;

        if (!string.IsNullOrWhiteSpace(businessHoursJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(businessHoursJson);
                if (doc.RootElement.TryGetProperty(dayKey, out var dayProp))
                {
                    isOpen = dayProp.TryGetProperty("isOpen", out var oProp) && oProp.GetBoolean();
                    if (dayProp.TryGetProperty("open", out var opProp) && TimeOnly.TryParse(opProp.GetString(), out var opTime))
                    {
                        startHour = opTime.Hour;
                    }
                    if (dayProp.TryGetProperty("close", out var clProp) && TimeOnly.TryParse(clProp.GetString(), out var clTime))
                    {
                        endHour = clTime.Hour == 0 ? 24 : clTime.Hour;
                    }
                }
            }
            catch {}
        }

        // If closed on this day, permit only if booking type has AllowOffDaysBooking
        if (!isOpen && !allowOffDays)
        {
            return (0, 0, false);
        }

        // If booking type allows after hours, extend window
        if (allowAfterHours)
        {
            startHour = Math.Min(startHour, 10);
            endHour = Math.Max(endHour, 26); // into 2:00 AM
        }

        return (startHour, endHour, true);
    }
}

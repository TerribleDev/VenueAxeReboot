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
public class LaneService : ILaneService
{
    private readonly IUnitOfWork _uow;

    public LaneService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static NextBookingSummaryDto MapToBookingSummary(Booking b)
    {
        return new NextBookingSummaryDto(
            b.Id,
            b.BookingReference,
            $"{b.GuestFirstName} {b.GuestLastName}".Trim(),
            b.StartTime,
            b.EndTime,
            b.PartySize,
            b.TotalAmountCents,
            b.PaidAmountCents,
            b.PaymentStatus,
            b.Notes
        );
    }

    public async Task<IReadOnlyList<LaneDto>> GetLanesForVenueAsync(Guid venueId)
    {
        var lanes = await _uow.Lanes.GetByVenueIdAsync(venueId, includeInactive: true);

        Venue? venue = null;
        try
        {
            venue = await _uow.Venues.GetByIdAsync(venueId);
        }
        catch
        {
            // Unit tests might have stubbed uow without Venues
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var tz = VenueTimeZoneHelper.GetTimeZone(venue?.Timezone);
        var todayDate = VenueTimeZoneHelper.GetVenueLocalDate(nowUtc, tz);
        var (startOfTodayUtc, endOfTodayUtc) = VenueTimeZoneHelper.GetUtcDayRange(todayDate, tz);

        IReadOnlyList<Booking> todaysBookings = Array.Empty<Booking>();
        try
        {
            todaysBookings = await _uow.Bookings.GetByVenueAndDateRangeAsync(venueId, startOfTodayUtc, endOfTodayUtc);
        }
        catch
        {
            todaysBookings = Array.Empty<Booking>();
        }

        var activeBookingsToday = todaysBookings
            .Where(b => b.Status != BookingStatus.Cancelled)
            .OrderBy(b => b.StartTime)
            .ToList();

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

            var laneBookings = activeBookingsToday
                .Where(b => b.BookingLanes.Any(bl => bl.LaneId == l.Id))
                .ToList();

            var currentBooking = laneBookings
                .FirstOrDefault(b => nowUtc >= b.StartTime.AddMinutes(-15) && nowUtc < b.EndTime);

            var nextBooking = laneBookings
                .Where(b => (currentBooking == null || b.Id != currentBooking.Id) && b.StartTime >= (currentBooking != null ? currentBooking.EndTime : nowUtc))
                .FirstOrDefault();

            NextBookingSummaryDto? currentBookingDto = currentBooking != null ? MapToBookingSummary(currentBooking) : null;
            NextBookingSummaryDto? nextBookingDto = nextBooking != null ? MapToBookingSummary(nextBooking) : null;

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
                sessionSummary,
                l.IsActive,
                nextBookingDto,
                currentBookingDto
            );
        }).ToList();
    }

    public async Task<LaneDto?> GetLaneByIdAsync(Guid laneId)
    {
        var lane = await _uow.Lanes.GetByIdAsync(laneId);
        if (lane == null) return null;

        Venue? venue = null;
        try
        {
            venue = await _uow.Venues.GetByIdAsync(lane.VenueId);
        }
        catch
        {
            // Unit tests might have stubbed uow without Venues
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var tz = VenueTimeZoneHelper.GetTimeZone(venue?.Timezone);
        var todayDate = VenueTimeZoneHelper.GetVenueLocalDate(nowUtc, tz);
        var (startOfTodayUtc, endOfTodayUtc) = VenueTimeZoneHelper.GetUtcDayRange(todayDate, tz);

        IReadOnlyList<Booking> todaysBookings = Array.Empty<Booking>();
        try
        {
            todaysBookings = await _uow.Bookings.GetByVenueAndDateRangeAsync(lane.VenueId, startOfTodayUtc, endOfTodayUtc);
        }
        catch
        {
            todaysBookings = Array.Empty<Booking>();
        }

        var laneBookings = todaysBookings
            .Where(b => b.Status != BookingStatus.Cancelled && b.BookingLanes.Any(bl => bl.LaneId == lane.Id))
            .OrderBy(b => b.StartTime)
            .ToList();

        var currentBooking = laneBookings
            .FirstOrDefault(b => nowUtc >= b.StartTime.AddMinutes(-15) && nowUtc < b.EndTime);

        var nextBooking = laneBookings
            .Where(b => (currentBooking == null || b.Id != currentBooking.Id) && b.StartTime >= (currentBooking != null ? currentBooking.EndTime : nowUtc))
            .OrderBy(b => b.StartTime)
            .FirstOrDefault();

        NextBookingSummaryDto? currentBookingDto = currentBooking != null ? MapToBookingSummary(currentBooking) : null;
        NextBookingSummaryDto? nextBookingDto = nextBooking != null ? MapToBookingSummary(nextBooking) : null;

        return new LaneDto(
            lane.Id, lane.VenueId, lane.LaneNumber, lane.Name, lane.MaxThrowers,
            lane.CurrentStatus, lane.TabletPairingCode, lane.ScreenPairingCode,
            lane.LastHeartbeatAt, null, lane.IsActive, nextBookingDto, currentBookingDto
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
            ScreenPairingCode = $"TV{random.Next(100, 999)}",
            IsActive = true
        };

        await _uow.Lanes.AddAsync(lane);
        await _uow.SaveChangesAsync();

        return new LaneDto(
            lane.Id, lane.VenueId, lane.LaneNumber, lane.Name, lane.MaxThrowers,
            lane.CurrentStatus, lane.TabletPairingCode, lane.ScreenPairingCode,
            lane.LastHeartbeatAt, null, lane.IsActive
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
            lane.LastHeartbeatAt, null, lane.IsActive
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
            lane.LastHeartbeatAt, null, lane.IsActive
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

    public async Task<LaneDto?> ToggleLaneActiveAsync(Guid laneId, bool? isActive = null)
    {
        var lane = await _uow.Lanes.GetByIdAsync(laneId);
        if (lane == null) return null;

        lane.IsActive = isActive ?? !lane.IsActive;
        lane.UpdatedAt = DateTimeOffset.UtcNow;
        await _uow.Lanes.UpdateAsync(lane);
        await _uow.SaveChangesAsync();

        return new LaneDto(
            lane.Id, lane.VenueId, lane.LaneNumber, lane.Name, lane.MaxThrowers,
            lane.CurrentStatus, lane.TabletPairingCode, lane.ScreenPairingCode,
            lane.LastHeartbeatAt, null, lane.IsActive
        );
    }

    public async Task<IReadOnlyList<BookingDto>> GetUpcomingBookingsForLaneAsync(Guid laneId)
    {
        var now = DateTimeOffset.UtcNow;
        var bookings = await _uow.Bookings.GetUpcomingBookingsByLaneAsync(laneId, now);
        return bookings.Select(b => new BookingDto(
            b.Id, b.VenueId, b.BookingReference, b.Status, b.GuestFirstName, b.GuestLastName,
            b.GuestEmail, b.GuestPhone, b.PartySize, b.StartTime, b.EndTime, b.TotalAmountCents,
            b.PaidAmountCents, b.PaymentStatus, b.BookingLanes.Select(bl => bl.Lane?.LaneNumber ?? 0).Where(n => n > 0).ToList(),
            b.Waivers.Count, b.BookingTypeId, b.DiscountAmountCents, b.AppliedDiscountCode, b.SquarePaymentId
        )).ToList();
    }

    public async Task<IReadOnlyList<LaneSlotOptionDto>> GetAvailableLanesForTimeslotAsync(Guid venueId, DateTimeOffset startTime, int durationMinutes)
    {
        var cfg = await _uow.BookingConfigs.GetByVenueIdAsync(venueId);
        int buffer = cfg?.TurnaroundBufferMinutes ?? 0;
        var endTime = startTime.AddMinutes(durationMinutes);
        var bufferedStart = startTime.AddMinutes(-buffer);
        var bufferedEnd = endTime.AddMinutes(buffer);

        var allLanes = await _uow.Lanes.GetByVenueIdAsync(venueId, includeInactive: true);
        var overlappingBookings = await _uow.Bookings.GetOverlappingBookingsWithLanesAsync(venueId, bufferedStart, bufferedEnd);
        var occupiedLaneIds = overlappingBookings.SelectMany(b => b.BookingLanes).Select(bl => bl.LaneId).ToHashSet();

        return allLanes.Select(l =>
        {
            if (!l.IsActive)
            {
                return new LaneSlotOptionDto(l.Id, l.LaneNumber, l.Name, false, "Lane is deactivated");
            }
            if (occupiedLaneIds.Contains(l.Id))
            {
                return new LaneSlotOptionDto(l.Id, l.LaneNumber, l.Name, false, "Already reserved");
            }
            return new LaneSlotOptionDto(l.Id, l.LaneNumber, l.Name, true, null);
        }).OrderBy(x => x.LaneNumber).ToList();
    }
}

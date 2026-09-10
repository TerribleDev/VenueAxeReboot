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
    Task<LaneDto?> ToggleLaneActiveAsync(Guid laneId, bool? isActive = null);
    Task<IReadOnlyList<BookingDto>> GetUpcomingBookingsForLaneAsync(Guid laneId);
    Task<IReadOnlyList<LaneSlotOptionDto>> GetAvailableLanesForTimeslotAsync(Guid venueId, DateTimeOffset startTime, int durationMinutes);
}

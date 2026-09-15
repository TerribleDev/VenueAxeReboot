using System;
using System.Threading;
using System.Threading.Tasks;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;

namespace VenueAxe.Services;

public interface ILaneGameService
{
    Task<ActiveSessionSummaryDto?> StartSessionAsync(Guid laneId, StartSessionRequest request);
    Task<ActiveSessionSummaryDto?> GetActiveSessionAsync(Guid laneId);
    Task<GameStateSnapshot?> RecordThrowAsync(Guid laneId, ThrowInputDto input);
    Task<GameStateSnapshot?> UndoLastThrowAsync(Guid laneId);
    Task<GameStateSnapshot?> SkipTurnAsync(Guid laneId);
    Task<bool> ExtendSessionAsync(Guid laneId, int extraMinutes);
    Task<GameStateSnapshot?> StartRematchAsync(Guid laneId);
    Task<GameStateSnapshot?> SwitchGameAsync(Guid laneId, string newGameTypeId);
    Task<bool> EndSessionAsync(Guid laneId);
    Task<bool> SubstitutePlayerAsync(Guid laneId, string playerId, string newName, string? newAvatarColor);
    Task<bool> TransferLaneAsync(Guid sourceLaneId, Guid targetLaneId);
    Task<bool> UpdateSessionTitleAsync(Guid laneId, string newTitle);
}

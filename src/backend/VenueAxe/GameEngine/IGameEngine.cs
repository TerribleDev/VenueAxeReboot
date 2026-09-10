using System;
using System.Collections.Generic;
using VenueAxe.Domain.Enums;

namespace VenueAxe.GameEngine;

public interface IGameEngine
{
    string GameTypeId { get; }
    string DisplayName { get; }
    string Description { get; }
    int DefaultRounds { get; }

    GameStateSnapshot Initialize(Guid matchId, List<GamePlayer> players, GameConfig? config = null);
    GameStateSnapshot RecordThrow(GameStateSnapshot state, double? x, double? y, TargetZone? manualZone, bool isClutchCalled);
    GameStateSnapshot UndoLastThrow(GameStateSnapshot state);
}

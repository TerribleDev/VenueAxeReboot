using VenueAxe.GameEngine;

namespace VenueAxe.DTOs;

public record SelectGameRequest(
    string GameTypeId,
    GameConfig? Config
);
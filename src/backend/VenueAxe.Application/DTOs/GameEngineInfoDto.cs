namespace VenueAxe.DTOs;

public record GameEngineInfoDto(
    string GameTypeId,
    string DisplayName,
    string Description,
    int DefaultRounds,
    string Objective = "",
    string ScoringRules = "",
    string SpecialRules = ""
);
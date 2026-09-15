using System.Collections.Generic;

namespace VenueAxe.GameEngine;

public static class GameEngineRegistry
{
    private static readonly FirstTo21Engine _firstTo21 = new();
    private static readonly CountdownGameEngine _countdown603 = new();

    private static readonly Dictionary<string, IGameEngine> _engines = new()
    {
        { "watl_standard", new WatlStandardMatchEngine() },
        { "kill_hunter", new KillHunterEngine() },
        { "around_the_world", new AroundTheWorldEngine() },
        { "axe_tictactoe", new AxeTicTacToeEngine() },
        { "first_to_21", _firstTo21 },
        { "blackjack_21", _firstTo21 },
        { "countdown_603", _countdown603 },
        { "countdown_301", _countdown603 },
        { "countdown", _countdown603 }
    };

    public static IGameEngine GetEngine(string gameTypeId)
    {
        return _engines.TryGetValue(gameTypeId, out var engine) ? engine : _engines["watl_standard"];
    }

    public static IEnumerable<IGameEngine> GetAllEngines() => _engines.Values;
}

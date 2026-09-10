using System.Collections.Generic;

namespace VenueAxe.GameEngine;

public static class GameEngineRegistry
{
    private static readonly Dictionary<string, IGameEngine> _engines = new()
    {
        { "watl_standard", new WatlStandardMatchEngine() },
        { "kill_hunter", new KillHunterEngine() },
        { "around_the_world", new AroundTheWorldEngine() },
        { "axe_tictactoe", new AxeTicTacToeEngine() },
        { "blackjack_21", new Blackjack21Engine() },
        { "countdown_301", new CountdownGameEngine() }
    };

    public static IGameEngine GetEngine(string gameTypeId)
    {
        return _engines.TryGetValue(gameTypeId, out var engine) ? engine : _engines["watl_standard"];
    }

    public static IEnumerable<IGameEngine> GetAllEngines() => _engines.Values;
}

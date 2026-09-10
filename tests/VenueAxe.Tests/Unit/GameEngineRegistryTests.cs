using VenueAxe.GameEngine;
using Xunit;

namespace VenueAxe.Tests.Unit;

public class GameEngineRegistryTests
{
    [Theory]
    [InlineData("watl_standard", typeof(WatlStandardMatchEngine))]
    [InlineData("countdown_301", typeof(CountdownGameEngine))]
    [InlineData("blackjack_21", typeof(Blackjack21Engine))]
    [InlineData("axe_tictactoe", typeof(AxeTicTacToeEngine))]
    [InlineData("around_the_world", typeof(AroundTheWorldEngine))]
    public void GetEngine_ResolvesCorrectEngineType(string gameTypeId, System.Type expectedType)
    {
        var engine = GameEngineRegistry.GetEngine(gameTypeId);
        Assert.NotNull(engine);
        Assert.IsType(expectedType, engine);
    }

    [Theory]
    [InlineData("")]
    [InlineData("unknown-game-mode")]
    [InlineData("invalid-123")]
    public void GetEngine_FallsBackToWatlStandard_WhenGameTypeUnknown(string unknownType)
    {
        var engine = GameEngineRegistry.GetEngine(unknownType);
        Assert.NotNull(engine);
        Assert.IsType<WatlStandardMatchEngine>(engine);
        Assert.Equal("watl_standard", engine.GameTypeId);
    }
}

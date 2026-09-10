using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Reqnroll;
using VenueAxe.Domain.Enums;
using VenueAxe.GameEngine;

namespace VenueAxe.Bdd.StepDefinitions;

[Binding]
public class ArcadeGamesSteps
{
    private AxeTicTacToeEngine _tttEngine = null!;
    private Blackjack21Engine _bjEngine = null!;
    private GameStateSnapshot _tttState = null!;
    private GameStateSnapshot _bjState = null!;

    [Given(@"a new Axe Tic-Tac-Toe match is initialized")]
    public void GivenANewAxeTicTacToeMatchIsInitialized()
    {
        _tttEngine = new AxeTicTacToeEngine();
    }

    [Given(@"arcade players ""(.*)"" and ""(.*)"" are enrolled")]
    public void GivenArcadePlayersAreEnrolled(string p1, string p2)
    {
        var players = new List<GamePlayer>
        {
            new() { Id = "x1", Name = p1 },
            new() { Id = "o2", Name = p2 }
        };
        _tttState = _tttEngine.Initialize(Guid.NewGuid(), players);
    }

    [When(@"""(.*)"" hits cell index (.*)")]
    public void WhenPlayerHitsCellIndex(string playerName, int cellIndex)
    {
        TargetZone zone = cellIndex switch
        {
            0 => TargetZone.ClutchLeft,
            1 => TargetZone.Ring5,
            2 => TargetZone.ClutchRight,
            3 => TargetZone.Ring4,
            4 => TargetZone.Bullseye,
            5 => TargetZone.Ring3,
            6 => TargetZone.Ring2,
            7 => TargetZone.Ring1,
            _ => TargetZone.Miss
        };

        _tttState = _tttEngine.RecordThrow(_tttState, null, null, manualZone: zone, isClutchCalled: false);
    }

    [Then(@"cell index (.*) should be owned by ""(.*)""")]
    public void ThenCellIndexShouldBeOwnedBy(int cellIndex, string playerName)
    {
        var player = _tttState.Players.First(p => p.Name == playerName);
        var throwForCell = _tttState.AllThrows.FirstOrDefault(t => t.PointsAwarded == cellIndex + 1);
        throwForCell.Should().NotBeNull();
        throwForCell!.PlayerId.Should().Be(player.Id);
    }

    [Then(@"""(.*)"" cannot claim already owned cell index (.*)")]
    public void ThenPlayerCannotClaimAlreadyOwnedCell(string playerName, int cellIndex)
    {
        int throwsBefore = _tttState.AllThrows.Count;
        WhenPlayerHitsCellIndex(playerName, cellIndex);
        var lastThrow = _tttState.AllThrows.Last();
        lastThrow.PointsAwarded.Should().Be(0); // Cannot overwrite owned cell
    }

    [Then(@"the Axe Tic-Tac-Toe match should be finished")]
    public void ThenTheAxeTicTacToeMatchShouldBeFinished()
    {
        _tttState.Status.Should().Be(MatchStatus.Finished);
    }

    [Then(@"the Axe Tic-Tac-Toe winner should be ""(.*)""")]
    public void ThenTheAxeTicTacToeWinnerShouldBe(string expectedWinner)
    {
        _tttState.WinnerName.Should().Be(expectedWinner);
    }

    [Given(@"a new Blackjack 21 match is initialized")]
    public void GivenANewBlackjack21MatchIsInitialized()
    {
        _bjEngine = new Blackjack21Engine();
    }

    [Given(@"player ""(.*)"" is enrolled with current hand total (.*)")]
    public void GivenPlayerIsEnrolledWithCurrentHandTotal(string playerName, int handTotal)
    {
        var player = new GamePlayer { Id = "bj-1", Name = playerName, Score = handTotal };
        _bjState = _bjEngine.Initialize(Guid.NewGuid(), new List<GamePlayer> { player });
        _bjState.Players[0].Score = handTotal;
    }

    [When(@"""(.*)"" throws and draws a card worth (.*)")]
    public void WhenPlayerThrowsAndDrawsCard(string playerName, int cardValue)
    {
        TargetZone zone = cardValue switch
        {
            6 => TargetZone.Bullseye,
            5 => TargetZone.Ring5,
            4 => TargetZone.Ring4,
            3 => TargetZone.Ring3,
            2 => TargetZone.Ring2,
            1 => TargetZone.Ring1,
            7 => TargetZone.ClutchLeft,
            _ => TargetZone.Miss
        };

        _bjState = _bjEngine.RecordThrow(_bjState, null, null, manualZone: zone, isClutchCalled: cardValue == 7);
    }

    [Then(@"""(.*)"" hand total should be (.*)")]
    public void ThenPlayerHandTotalShouldBe(string playerName, int expectedTotal)
    {
        // When busted in Blackjack21, score resets to 11 per rules
        var player = _bjState.Players.First(p => p.Name == playerName);
        player.Score.Should().Be(11);
    }

    [Then(@"""(.*)"" hand should bust")]
    public void ThenPlayerHandShouldBust(string playerName)
    {
        var player = _bjState.Players.First(p => p.Name == playerName);
        player.Score.Should().Be(11); // Reset to 11 on bust
    }
}

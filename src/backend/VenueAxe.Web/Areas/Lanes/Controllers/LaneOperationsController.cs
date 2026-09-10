using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Services;
using VenueAxe.Web.Hubs;

namespace VenueAxe.Web.Areas.Lanes.Controllers;

[Area("Lanes")]
[Route("api/lanes/operations")]
[ApiController]
public class LaneOperationsController : ControllerBase
{
    private readonly ILaneGameService _gameService;
    private readonly IHubContext<LaneHub, ILaneClient> _hub;

    public LaneOperationsController(ILaneGameService gameService, IHubContext<LaneHub, ILaneClient> hub)
    {
        _gameService = gameService;
        _hub = hub;
    }

    [HttpGet("games")]
    public ActionResult<List<GameEngineInfoDto>> GetAvailableGames()
    {
        var engines = GameEngineRegistry.GetAllEngines().Select(e => new GameEngineInfoDto(
            e.GameTypeId,
            e.DisplayName,
            e.Description,
            e.DefaultRounds
        )).ToList();
        return Ok(engines);
    }

    [HttpPost("{laneId:guid}/start-session")]
    public async Task<ActionResult<ActiveSessionSummaryDto>> StartSession(
        Guid laneId,
        [FromBody] StartSessionRequest request)
    {
        var summary = await _gameService.StartSessionAsync(laneId, request);
        if (summary == null) return NotFound();

        if (summary.CurrentGame != null)
        {
            await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnThrowRecorded(summary.CurrentGame);
        }

        return Ok(summary);
    }

    [HttpGet("{laneId:guid}/active-session")]
    public async Task<ActionResult<ActiveSessionSummaryDto>> GetActiveSession(Guid laneId)
    {
        var summary = await _gameService.GetActiveSessionAsync(laneId);
        if (summary == null) return NotFound(new { message = "No active session on this lane" });
        return Ok(summary);
    }

    [HttpPost("{laneId:guid}/throw")]
    public async Task<ActionResult<GameStateSnapshot>> RecordThrow(
        Guid laneId,
        [FromBody] ThrowInputDto input)
    {
        var updatedState = await _gameService.RecordThrowAsync(laneId, input);
        if (updatedState == null) return NotFound(new { message = "No active game session on this lane" });

        await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnThrowRecorded(updatedState);
        return Ok(updatedState);
    }

    [HttpPost("{laneId:guid}/undo")]
    public async Task<ActionResult<GameStateSnapshot>> UndoLastThrow(Guid laneId)
    {
        var updatedState = await _gameService.UndoLastThrowAsync(laneId);
        if (updatedState == null) return NotFound(new { message = "No throws to undo on this lane" });

        await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnThrowRecorded(updatedState);
        return Ok(updatedState);
    }

    [HttpPost("{laneId:guid}/skip-turn")]
    public async Task<ActionResult<GameStateSnapshot>> SkipTurn(Guid laneId)
    {
        var updatedState = await _gameService.SkipTurnAsync(laneId);
        if (updatedState == null) return NotFound(new { message = "No active game session to skip turn on" });

        await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnThrowRecorded(updatedState);
        return Ok(updatedState);
    }

    [HttpPost("{laneId:guid}/extend")]
    public async Task<IActionResult> ExtendSession(Guid laneId, [FromBody] ExtendSessionRequest request)
    {
        var success = await _gameService.ExtendSessionAsync(laneId, request.ExtraMinutes);
        if (!success) return NotFound();

        await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnSessionExtended(request.ExtraMinutes);
        return Ok(new { laneId, extendedMinutes = request.ExtraMinutes });
    }

    [HttpPost("{laneId:guid}/safety-stop")]
    public async Task<IActionResult> SafetyStop(Guid laneId, [FromQuery] string reason = "Safety Briefing Required")
    {
        await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnSafetyStopActivated(reason);
        return Ok(new { laneId, message = "Safety stop triggered" });
    }

    [HttpPost("{laneId:guid}/rematch")]
    public async Task<ActionResult<GameStateSnapshot>> Rematch(Guid laneId)
    {
        var newState = await _gameService.StartRematchAsync(laneId);
        if (newState == null) return NotFound(new { message = "No active session to rematch" });

        await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnThrowRecorded(newState);
        return Ok(newState);
    }

    [HttpPost("{laneId:guid}/switch-game")]
    public async Task<ActionResult<GameStateSnapshot>> SwitchGame(Guid laneId, [FromBody] SelectGameRequest request)
    {
        var newState = await _gameService.SwitchGameAsync(laneId, request.GameTypeId);
        if (newState == null) return NotFound(new { message = "No active session on this lane to switch games" });

        await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnThrowRecorded(newState);
        return Ok(newState);
    }

    [HttpPost("{laneId:guid}/end-session")]
    public async Task<IActionResult> EndSession(Guid laneId)
    {
        var success = await _gameService.EndSessionAsync(laneId);
        if (!success) return NotFound(new { message = "Lane not found" });

        await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnLaneStateChanged(laneId, LaneStatus.Turnaround.ToString());
        await _hub.Clients.Group("admin").OnLaneStateChanged(laneId, LaneStatus.Turnaround.ToString());
        return Ok(new { laneId, status = "Completed" });
    }

    [HttpPost("{laneId:guid}/substitute")]
    public async Task<IActionResult> SubstitutePlayer(Guid laneId, [FromBody] SubstitutePlayerRequest request)
    {
        var success = await _gameService.SubstitutePlayerAsync(laneId, request.PlayerId, request.NewName, request.NewAvatarColor);
        if (!success) return NotFound(new { message = "Player or session not found" });

        var summary = await _gameService.GetActiveSessionAsync(laneId);
        if (summary?.CurrentGame != null)
        {
            await _hub.Clients.Group(LaneHub.GetLaneGroupName(laneId)).OnThrowRecorded(summary.CurrentGame);
        }

        return Ok(new { laneId, message = "Player substituted" });
    }
}

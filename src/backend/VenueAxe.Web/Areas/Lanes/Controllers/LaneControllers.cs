using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Services;
using VenueAxe.Web.Hubs;

namespace VenueAxe.Web.Areas.Lanes.Controllers;

[Area("Lanes")]
[Route("api/lanes/terminals")]
[ApiController]
public class LaneTerminalsController : ControllerBase
{
    private readonly ILaneService _laneService;

    public LaneTerminalsController(ILaneService laneService)
    {
        _laneService = laneService;
    }

    [HttpPost("pair")]
    public async Task<ActionResult<TerminalAuthResult>> PairTerminal([FromBody] PairTerminalRequest request)
    {
        var result = await _laneService.PairTerminalAsync(request.PairingCode, request.TerminalType);
        if (result == null) return NotFound(new { message = "Invalid or expired pairing code" });
        return Ok(result);
    }
}

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
}

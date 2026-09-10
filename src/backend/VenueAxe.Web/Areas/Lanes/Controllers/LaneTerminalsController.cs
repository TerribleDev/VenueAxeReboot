using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Services;

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

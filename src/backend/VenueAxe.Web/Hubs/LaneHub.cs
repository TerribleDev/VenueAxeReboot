using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;

namespace VenueAxe.Web.Hubs;

public interface ILaneClient
{
    Task OnLaneStateChanged(LaneDto lane);
    Task OnThrowRecorded(GameStateSnapshot gameState);
    Task OnClutchCalled(string playerId, string side);
    Task OnSafetyStopActivated(string reason);
    Task OnSessionExtended(int newRemainingMinutes);
}

public class LaneHub : Hub<ILaneClient>
{
    public async Task JoinLaneGroup(Guid laneId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetLaneGroupName(laneId));
    }

    public async Task LeaveLaneGroup(Guid laneId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetLaneGroupName(laneId));
    }

    public static string GetLaneGroupName(Guid laneId) => $"lane_{laneId}";
}

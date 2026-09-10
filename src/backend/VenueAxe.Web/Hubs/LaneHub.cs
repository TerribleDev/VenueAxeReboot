using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using VenueAxe.Domain.Enums;
using VenueAxe.DTOs;
using VenueAxe.GameEngine;
using VenueAxe.Repositories;

namespace VenueAxe.Web.Hubs;
public class LaneHub : Hub<ILaneClient>
{
    private readonly IUnitOfWork _uow;

    public LaneHub(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task JoinLaneGroup(Guid laneId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetLaneGroupName(laneId));
    }

    public async Task LeaveLaneGroup(Guid laneId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetLaneGroupName(laneId));
    }

    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "admin");
    }

    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admin");
    }

    public async Task SendHeartbeat(Guid laneId, string terminalType, int? batteryLevel)
    {
        var lane = await _uow.Lanes.GetByIdAsync(laneId);
        if (lane != null)
        {
            lane.LastHeartbeatAt = DateTimeOffset.UtcNow;
            await _uow.Lanes.UpdateAsync(lane);
            await _uow.SaveChangesAsync();
        }

        await Clients.Group("admin").OnHeartbeatReceived(laneId, terminalType, batteryLevel, DateTimeOffset.UtcNow);
    }

    public async Task CallKillshot(Guid laneId, string playerId, string side)
    {
        await Clients.Group(GetLaneGroupName(laneId)).OnKillCalled(playerId, side);
        await Clients.Group(GetLaneGroupName(laneId)).OnClutchCalled(playerId, side); // Backwards compatibility
    }

    public static string GetLaneGroupName(Guid laneId) => $"lane_{laneId}";
}

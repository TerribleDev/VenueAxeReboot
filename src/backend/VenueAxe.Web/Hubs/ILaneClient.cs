using System;
using System.Threading.Tasks;
using VenueAxe.GameEngine;

namespace VenueAxe.Web.Hubs;
public interface ILaneClient
{
    Task OnLaneStateChanged(Guid laneId, string status);
    Task OnThrowRecorded(GameStateSnapshot gameState);
    Task OnClutchCalled(string playerId, string side);
    Task OnKillCalled(string playerId, string side);
    Task OnSafetyStopActivated(string reason);
    Task OnSessionExtended(int newRemainingMinutes);
    Task OnHeartbeatReceived(Guid laneId, string terminalType, int? batteryLevel, DateTimeOffset timestamp);
}

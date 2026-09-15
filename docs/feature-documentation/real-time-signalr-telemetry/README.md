# Real-Time SignalR Telemetry

## 1. Overview & Business Value
VenueAxe uses ASP.NET Core SignalR for sub-50ms real-time bidirectional communication between the .NET API, in-lane tablet consoles, overhead TV displays, and the admin dashboard. All game state changes, safety events, and session updates are broadcast instantly to synchronized clients.

### Target Roles
- **All connected clients**: Receive real-time updates

## 2. Technical Architecture

### SignalR Hub: `LaneHub`
- **Route**: `/hubs/lane`
- **Hub Type**: Strongly-typed (`Hub<ILaneClient>`)
- **Authentication**: None (supports unauthenticated tablets/TVs)

### Group Architecture
| Group Name | Members | Purpose |
|:-----------|:--------|:--------|
| `lane_{laneId}` | Tablet + TV for that lane | Lane-specific game events |
| `admin` | Admin dashboard connections | Fleet-wide lane status updates |

### Client Interface: `ILaneClient`
```csharp
Task OnLaneStateChanged(Guid laneId, string status);
Task OnThrowRecorded(GameStateSnapshot gameState);
Task OnClutchCalled(string playerId, string side);
Task OnKillCalled(string playerId, string side);
Task OnSafetyStopActivated(string reason);
Task OnSessionExtended(int newRemainingMinutes);
Task OnHeartbeatReceived(Guid laneId, string terminalType, int? batteryLevel, DateTimeOffset timestamp);
```

### Hub Methods (Client → Server)
| Method | Purpose |
|:-------|:--------|
| `JoinLaneGroup(laneId)` | Subscribe to lane events |
| `LeaveLaneGroup(laneId)` | Unsubscribe from lane events |
| `JoinAdminGroup()` / `JoinAdminDashboard()` | Subscribe to admin updates |
| `LeaveAdminGroup()` / `LeaveAdminDashboard()` | Unsubscribe from admin updates |
| `SendHeartbeat(laneId, terminalType, batteryLevel)` | Terminal health check (persisted to DB) |
| `CallKillshot(laneId, playerId, side)` | Broadcast kill/clutch call to lane |

### Backplane
- **Optional Redis**: When `ConnectionStrings__Redis` is set, SignalR uses Redis backplane for multi-instance support
- **Default**: In-memory (single-instance only)

### Event Flow
```
Tablet tap → POST /api/lanes/operations/{laneId}/throw
  → LaneGameService.RecordThrowAsync()
    → Game engine processes throw
    → MatchThrow persisted to PostgreSQL
  → Controller broadcasts via IHubContext<LaneHub>
    → hub.Clients.Group("lane_{laneId}").OnThrowRecorded(snapshot)
      → Tablet updates UI
      → TV updates scoreboard + hit visualization
```

## 3. Testing Strategy

### Backend
- **Integration**: `SignalRTelemetryFunctionalTests.cs`
- **BDD**: `LaneOperations.feature` (covers SignalR-triggered workflows)

## 4. Manual Verification
See root `GEMINI.md` §5.3 for the complete dual-screen throwing verification protocol.

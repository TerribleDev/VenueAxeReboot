# Session Lifecycle

## 1. Overview & Business Value
The Session Lifecycle system automatically manages the expiration of throwing sessions. When a session's allocated time runs out, the system transitions the lane from `Active` to `Turnaround` status, notifying all connected clients in real-time. A 3-minute grace period prevents abrupt cutoffs during active matches.

### Target Roles
- **System (Background)**: Automatic session expiration
- **Admin**: Monitors via dashboard

## 2. Technical Architecture

### Background Service: `SessionLifecycleBackgroundService`
- **Type**: `BackgroundService` (hosted service)
- **Poll Interval**: 15 seconds
- **Location**: `VenueAxe.Web/BackgroundServices/`

### Expiration Logic
```
Every 15 seconds:
  1. Query all sessions where Status = Active AND ExpiresAt <= now
  2. For each candidate:
     - If no in-progress match → expire immediately
     - If in-progress match exists AND ExpiresAt + 3min <= now → expire (grace period exceeded)
     - If in-progress match exists AND ExpiresAt + 3min > now → skip (still in grace period)
  3. For expired sessions:
     - Set Status = Completed, EndedAt = now
     - Set Lane.CurrentStatus = Turnaround
     - Broadcast OnLaneStateChanged to lane group and admin group
  4. SaveChangesAsync()
```

### Session Extensions
Sessions can be extended via `POST /api/lanes/operations/{laneId}/extend`:
- Adds `ExtraMinutes` to `Session.ExpiresAt`
- Broadcasts `OnSessionExtended` to lane group
- No limit on number of extensions

### Key Design Decisions
- Uses `IgnoreQueryFilters()` since background service has no ambient tenant context
- Creates its own `IServiceScope` per poll cycle
- Directly accesses `VenueAxeDbContext` (bypasses UoW pattern for simplicity)
- 3-minute grace period for active matches prevents mid-throw interruptions

## 3. Testing Strategy

### Backend
- **Unit Tests**: Needed — see `implementation_plan.md` Phase 8
- **Integration**: `SignalRTelemetryFunctionalTests.cs` (partial coverage)

## 4. Manual Verification
1. Start a session via admin panel with a short duration (e.g., 2 minutes)
2. Wait for session to expire naturally
3. Verify lane transitions to "Turnaround" on admin dashboard
4. Verify tablet/TV receive the state change notification
5. Start a match, let session expire during active play → verify 3-minute grace period

# Lane Management

## 1. Overview & Business Value
The Lane Management subsystem is the real-time operations dashboard for venue staff. It provides visibility into all throwing lanes, active game sessions, upcoming bookings, session timers, and hardware terminal status (tablets and overhead TVs). Staff can start/stop sessions, transfer lanes, regenerate pairing codes, and toggle lane availability.

### Target Roles
- **Owner/Manager**: Full lane configuration and operations
- **LaneMaster**: Lane floor operations (start/stop sessions, check-ins)

## 2. Technical Architecture & Data Model

### Key Entities
| Entity | Base Class | Purpose |
|:-------|:-----------|:--------|
| `Lane` | `VenueScopedEntity` | Physical throwing lane |
| `LaneSession` | `VenueScopedEntity` | Time-bounded throwing session on a lane |

### Lane Properties
- `LaneNumber` (int): Physical position for contiguous allocation
- `Name` (string): Display name (e.g., "Lane 01")
- `MaxThrowers` (int): Capacity limit per lane (default 6)
- `CurrentStatus` (LaneStatus enum): Available, Active, Turnaround, Maintenance, Frozen
- `TabletPairingCode` / `ScreenPairingCode`: 6-digit codes for device pairing
- `IsActive` (bool): Whether the lane is included in booking capacity

### LaneService Capabilities
- `GetLanesForVenueAsync(venueId)` — loads all lanes with active sessions, game states, and today's booking assignments
- `CreateLaneAsync(venueId, request)` — creates a new lane with auto-generated pairing codes
- `UpdateLaneAsync(laneId, request)` — updates lane properties and session title
- `DeleteLaneAsync(laneId)` — removes a lane
- `UpdateLaneStatusAsync(laneId, status)` — changes lane status
- `RegeneratePairingCodesAsync(laneId)` — generates new pairing codes
- `ToggleLaneActiveAsync(laneId)` — activates/deactivates for booking
- `PairTerminalAsync(code, type)` — pairs tablet/screen device to lane
- `GetAvailableLanesForTimeslotAsync(venueId, start, duration)` — returns lane availability for admin booking

### Lane Deactivation Impact
Deactivating a lane (`IsActive = false`) has cascading effects:
1. Excluded from public availability slot calculations
2. Excluded from lane allocation engine
3. Flagged with "Lane is deactivated" in admin booking modal
4. Contiguous allocation chains cannot cross deactivated lanes

## 3. API & Telemetry Specifications

### Admin Endpoints
| Method | Route | Purpose |
|:-------|:------|:--------|
| GET | `/api/admin/lanes/venue/{venueId}` | Get all lanes for venue |
| GET | `/api/admin/lanes/{id}` | Get single lane |
| POST | `/api/admin/lanes/venue/{venueId}` | Create lane |
| PUT | `/api/admin/lanes/{id}` | Update lane |
| DELETE | `/api/admin/lanes/{id}` | Delete lane |
| PUT | `/api/admin/lanes/{id}/status` | Update lane status |
| PUT | `/api/admin/lanes/{id}/toggle-active` | Toggle lane active |
| POST | `/api/admin/lanes/{id}/regenerate-pairing` | Regenerate pairing codes |
| POST | `/api/admin/lanes/{sourceLaneId}/transfer-to/{targetLaneId}` | Transfer active session |
| GET | `/api/admin/lanes/{id}/upcoming-reservations` | Get upcoming bookings |
| GET | `/api/admin/lanes/venue/{venueId}/available-for-slot` | Get available lanes for time |

### Terminal Endpoints (No Auth)
| Method | Route | Purpose |
|:-------|:------|:--------|
| POST | `/api/lanes/terminals/pair` | Pair device to lane |

### SignalR Events (via LaneHub)
| Event | Direction | Purpose |
|:------|:----------|:--------|
| `OnLaneStateChanged(laneId, status)` | Server → Client | Lane status update |
| `OnHeartbeatReceived(laneId, type, battery, ts)` | Server → Client | Terminal heartbeat |
| `JoinLaneGroup(laneId)` | Client → Server | Subscribe to lane events |
| `JoinAdminGroup()` | Client → Server | Subscribe to admin events |
| `SendHeartbeat(laneId, type, battery)` | Client → Server | Terminal health check |

## 4. Testing Strategy

### Backend
- **Unit Tests**: `LaneManagementTests.cs`, `LaneActiveAndReassignmentTests.cs`, `LaneAllocationEngineTests.cs`
- **BDD**: `LaneOperations.feature`
- **Integration**: `SignalRTelemetryFunctionalTests.cs`

### Frontend
- **Tests**: `lane-session-start-and-tablet-lobby.test.ts`, `lane-upcoming-booking.test.ts`, `lane-schedule-matrix.test.ts`

## 5. Manual Verification
1. Open `http://localhost:5173/admin/lanes`
2. Verify lane grid shows all 8 seeded lanes with status indicators
3. Click `+ Start Session` on an idle lane → verify session launcher modal
4. Toggle a lane's active status → verify it shows as deactivated
5. Regenerate pairing codes → verify new codes appear

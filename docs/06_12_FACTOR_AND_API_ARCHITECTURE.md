# Module 06: 12-Factor System Architecture, APIs & SignalR Real-Time Telemetry

## 1. 12-Factor Architectural Compliance Guide

```
+-----------------------------------------------------------------------------------------+
|                                12-FACTOR ARCHITECTURE                                   |
+-----------------------------------------------------------------------------------------+
|  1. Codebase           Single git repository with modular .NET 10 API & Svelte 5 frontend |
|  2. Dependencies       Explicit NuGet packages & pnpm dependencies with strict lockfiles  |
|  3. Config             100% Environment Variables (ASPNETCORE_*, DB connection strings)  |
|  4. Backing Services   PostgreSQL, Redis, Stripe, S3 treated as attached URL resources   |
|  5. Build/Release/Run  Immutable Docker containers generated via multi-stage CI pipeline  |
|  6. Processes          Stateless API processes; WebSocket session state via Redis PubSub  |
|  7. Port Binding       Self-contained Kestrel web server on configurable PORT ($PORT)     |
|  8. Concurrency        Process model horizontal scaling behind reverse proxy / ingress    |
|  9. Disposability      Sub-2s startup & graceful SIGTERM handling with socket draining    |
| 10. Dev/Prod Parity    Identical PostgreSQL 17 + Redis container environment locally      |
| 11. Logs               Structured JSON events emitted directly to stdout / stderr         |
| 12. Admin Processes    One-off database migrations and seed scripts run via CLI container |
+-----------------------------------------------------------------------------------------+
```

---

## 2. Real-Time SignalR Lane Telemetry Protocol

### 2.1 LaneHub (`/hubs/lane`)
The SignalR hub maintains real-time synchronization between:
- Venue Admin Lane Monitor Dashboard
- In-Lane Thrower Tablet Console
- Overhead 4K/1080p TV Display Monitor

### 2.2 Hub Methods & Events Matrix
```
Tablet/Client                     SignalR Hub (LaneHub)                   Overhead TV & Admin
     |                                      |                                      |
     |--- JoinLaneGroup(laneId, token) ---->|                                      |
     |                                      |--- Send CurrentLaneState ----------->|
     |                                      |<-- JoinLaneGroup(laneId, token) -----|
     |                                      |                                      |
     |--- RecordThrow(throwPayload) ------->|                                      |
     |                                      |--- ScoreUpdated(throwEvent) -------->|
     |                                      |--- TargetHitVisualized(coords) ----->|
     |                                      |                                      |
     |--- CallClutch(playerId, side) ------>|                                      |
     |                                      |--- ClutchAlertTriggered ------------>|
     |                                      |                                      |
     |                                      |<-- SafetyStopTriggered (Admin) ------|
     |<-- EmergencyPauseActivated ----------|--- EmergencyPauseActivated --------->|
```

#### Client to Server Invocations
- `JoinLane(Guid laneId, string devicePairingToken)`: Joins the SignalR connection to the isolated group `lane_{laneId}`.
- `RecordThrow(ThrowInputDto input)`: Submits tap coordinates or score override.
- `CallClutch(Guid playerId, ClutchSide side)`: Activates the 7-point clutch for the upcoming throw.
- `UndoThrow(Guid matchId)`: Reverts the last throw in the current round.
- `SelectGame(Guid sessionId, string gameTypeId, GameConfigDto config)`: Initializes a new game mode.
- `UpdateRoster(Guid sessionId, List<PlayerDto> roster)`: Updates thrower names, avatars, and turn order.

#### Server to Client Broadcasts
- `OnLaneStateChanged(LaneStateDto state)`: Emits full lane status, timer countdown, and active game snapshot.
- `OnThrowRecorded(ThrowResultDto result)`: Broadcasts hit coordinates, score awarded, updated leaderboard, and next thrower turn.
- `OnClutchCalled(ClutchEventDto event)`: Triggers high-visibility audio/visual alert on Overhead TV screen.
- `OnBullseyeHit(BullseyeEventDto event)`: Triggers golden particle explosion VFX on Overhead TV screen.
- `OnMatchFinished(MatchSummaryDto summary)`: Emits podium rankings, match stats, and accuracy heatmap.
- `OnSafetyAlert(SafetyAlertDto alert)`: Immediate lock-out screen on tablet and TV.

---

## 3. REST API Contract Overview

### 3.1 Authentication & Profile (`/api/v1/auth`)
- `POST /api/v1/auth/login`: Authenticates venue owner / staff; returns JWT + HttpOnly refresh cookie.
- `POST /api/v1/auth/refresh`: Exchanges refresh token for a fresh JWT.
- `GET /api/v1/auth/me`: Current user profile, venue assignments, and role permissions.
- `POST /api/v1/auth/terminal/pair`: Exchanges 6-digit PIN for a scoped tablet/TV device token.

### 3.2 Booking Editor & Public Booking Widget (`/api/v1/bookings`)
- `GET /api/v1/public/venues/{venueSlug}/booking-page`: Retrieves public booking theme, packages, and custom fields.
- `GET /api/v1/public/venues/{venueSlug}/availability`: Real-time query returning available time slots for a given date & party size.
- `POST /api/v1/public/venues/{venueSlug}/checkout`: Creates Stripe PaymentIntent and tentative lane hold.
- `POST /api/v1/public/venues/{venueSlug}/confirm`: Confirms reservation post-payment; returns booking reference & waiver share link.
- `GET /api/v1/admin/venues/{venueId}/booking-config`: [Owner] Retrieves full booking page editor config.
- `PUT /api/v1/admin/venues/{venueId}/booking-config`: [Owner] Updates themes, pricing rules, and package catalog.

### 3.3 Digital Waivers (`/api/v1/waivers`)
- `GET /api/v1/public/waivers/template/{venueSlug}`: Fetches active legal waiver template.
- `POST /api/v1/public/waivers/sign`: Submits signature canvas, guardian data, and audit metadata.
- `GET /api/v1/admin/waivers/search`: [Staff] Fast lookup by name, email, phone, or booking ID.
- `GET /api/v1/admin/waivers/{id}/pdf`: Generates stamped audit PDF for insurance archives.

### 3.4 Lane Operations & Game Hub (`/api/v1/lanes`)
- `GET /api/v1/admin/venues/{venueId}/lanes`: Live status of all physical lanes.
- `POST /api/v1/admin/lanes/{laneId}/start-session`: Launches walk-in or booked session.
- `POST /api/v1/admin/lanes/{laneId}/extend`: Adds time to active session.
- `POST /api/v1/admin/lanes/{laneId}/safety-stop`: Triggers emergency safety freeze.

---

## 4. Svelte 5 Frontend Architecture & UI Design System

### 4.1 Svelte 5 Modern Architecture
- **Runes Reactivity**: Leveraging Svelte 5 `$state`, `$derived`, and `$effect` for ultra-lean, 60fps real-time score animations and target touch collision.
- **Route Structure**:
  - `/admin/*`: Venue Owner / Staff operations (Booking Editor, Waiver Search, Lane Floor Plan, Analytics).
  - `/book/[venueSlug]`: Public standalone and embeddable booking experience.
  - `/sign/[venueSlug]` & `/sign/w/[bookingToken]`: Public mobile-first digital waiver kiosk.
  - `/lane-tablet`: In-Lane thrower scorekeeper touch console.
  - `/lane-screen`: Overhead TV broadcast monitor display (full-screen 16:9 4K/1080p).

### 4.2 Aesthetics & Styling System
- **Design Tokens**:
  - Background Base: `#0a0c10` (Deep obsidian/carbon)
  - Surface Card: `#131722` (Subtle dark slate)
  - Border Tone: `#242b3d` (Crisp industrial border)
  - Primary Accent: `#f59e0b` (Electric amber / competition gold)
  - Neon Cyan / Clutch: `#06b6d4`
  - Target Red: `#ef4444`
  - Target Blue: `#3b82f6`
  - Text Primary: `#f8fafc` (High-contrast crisp white)
  - Text Muted: `#94a3b8`
- **Zero Cartoon / Zero Gimmick Rule**:
  - Precision typography (Chakra Petch / Inter / Outfit for sport-broadcast aesthetic).
  - Subtle glow effects, smooth cubic-bezier transitions, high-contrast target rings.
  - Tactile physical-button feedback for tablet touch screens.

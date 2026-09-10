# Module 03: Lane Management & Floor Operations Specification

## 1. Overview
The Lane Management subsystem is the mission-control dashboard for venue owners, managers, and lane coaches. It provides real-time visibility into all throwing lanes, active game sessions, upcoming bookings, remaining session times, and hardware terminal status (In-Lane Tablets and Overhead TV screens).

---

## 2. Lane Operations Center & Visual Floor Plan

### 2.1 Interactive Lane Grid & Floor Map View
- **Dual Display Modes**:
  1. **Tile / Card Grid Mode**: High-density view of all lanes (e.g. Lanes 1-16) sorted by status or number.
  2. **Floor Plan / Map Mode**: Customizable drag-and-drop layout reflecting the physical venue blueprint (including lane bays, bar area, check-in desk, and spectator seating).
- **Lane Card Attributes**:
  - Lane Number & Custom Name (e.g., `Lane 4 - "The Bullseye Bay"`).
  - Current Status (Color-coded indicator).
  - Active Booking / Guest Group Name (e.g., `Smith Party (6 Throwers)`).
  - Live Countdown Clock (`34:12 remaining`).
  - Progress Ring Bar (visual percentage of session elapsed).
  - Active Game Mode (e.g., `WATL Standard - Round 6/10`).
  - Paired Device Telemetry: Tablet Battery / Online Status 🟢 and Overhead Screen Connection 🟢.

### 2.2 Dedicated Admin Operations Architecture & Routing
The admin management portal has transitioned from monolithic tab-switching to full client-side deep routing with state preservation:
- `/admin` $\rightarrow$ automatically redirects to `/admin/lanes`
- `/admin/lanes`: Lane manager floor plan, active match status, and start/stop controls
- `/admin/schedule`: 24-hour horizontal lane schedule matrix timeline
- `/admin/bookings`: Reservations ledger and walk-in reservation modal
- `/admin/waivers`: Digital waiver vault and PDF signing records
- `/admin/editor`: Visual booking page theme & policy editor
- `/admin/emails`: SMTP server settings and test email dispatcher
- `/admin/staff`: Staff role management and invitations
- `/admin/settings`: Venue profile, address, and operating hours

Selected venue context is centrally managed via `venueState` and synchronized across all routes and browser page refreshes.

### 2.3 Simplified Lane Controls & Activation Toggle
- **Context-Sensitive Session Buttons**:
  - Confusing multi-state dropdowns are eliminated.
  - **Idle Lane**: Displays prominent `+ Start Session` button opening the match launcher modal.
  - **Active Lane**: Displays high-contrast red `⏹️ Stop Session` button with confirmation prompt, ending the match and releasing the lane immediately.
- **Lane Deactivation Toggle & Booking Exclusion Architecture**:
  - When idle, the `Available` status pill functions as an interactive toggle (`PUT /api/admin/lanes/{id}/toggle-active`).
  - Deactivating a lane sets `IsActive = false`.
  - **Multi-Layer Booking Prevention Enforcement**:
    1. **Public Availability & Capacity (`CheckAvailabilityAsync`)**: Queries `_uow.Lanes.GetByVenueIdAsync(venueId, includeInactive: false)`. Deactivated lanes are completely excluded from venue capacity, available slot counts, and proposed lane lists.
    2. **Public Guest Booking Creation (`CreateGuestBookingAsync`)**: Allocates only active lanes. If a contiguous block cannot be formed without the deactivated lane, allocation is safely rejected.
    3. **Contiguous Allocation Engine (`LaneAllocationEngine`)**: Strictly filters `allVenueLanes.Where(l => l.IsActive && !occupiedLaneIds.Contains(l.Id))`. Physical adjacency chains (`LaneNumber[k+1] == LaneNumber[k] + 1`) cannot cross or include a deactivated lane.
    4. **Admin Walk-In & Reservation Creation (`CreateAdminBookingAsync`)**: Validates that auto-allocated contiguous lanes or specified manual lane numbers (`request.SpecificLaneNumbers`) must satisfy `lane != null && lane.IsActive`. Deactivated lane numbers are rejected with an explicit warning log.
    5. **Timeslot Bookable Lanes (`GetAvailableLanesForTimeslotAsync`)**: Flags any deactivated lane with `isAvailable: false` and `conflictReason: "Lane is deactivated"`. The admin "+ New Reservation" modal dropdown strictly filters `slot.isAvailable`, completely hiding deactivated lanes from the selector.
    6. **Lane Reassignment (`ReassignBookingLaneAsync`)**: Throws `InvalidOperationException("Target lane does not exist or is deactivated.")` if an operator attempts to move a booking onto a deactivated bay.
    7. **Floor Card Controls**: Deactivated lane cards show a dimmed border, `⚠️ Lane Deactivated from Bookings` alert, and disable the `+ Start Session` button.
  - If a lane has upcoming reservations, a safety warning modal lists all affected booking dates, guests, and references before allowing deactivation confirmation (`GET /api/admin/lanes/{id}/upcoming-reservations`).

### 2.4 24-Hour Lane Schedule Matrix Timeline
- **Unified Lane Terminology**:
  - Deprecated "bays" nomenclature replaced with "lanes" across all user interfaces, DTOs, and controllers.
- **Real-Time Visualizer**:
  - Vertical high-contrast red `NOW` line indicator shows current local time against the operating timeline.
  - Quick-center `📍 Now` toolbar button instantly scrolls the horizontal timeline to the current time.
  - Fully scrollable timeline covering 24-hour past and future operating hours.
  - Auto-refreshes every 60 seconds (1 minute) to keep schedule synchronized with walk-ins and web bookings.
### 2.5 Upcoming Reservations Indicator on Lane Cards
- **Automatic Today & Future Window Detection**:
  - The backend resolves the venue's local timezone (`Venue.Timezone`) and computes the remainder of the current operating day (`UtcNow <= StartTime < EndOfTodayUtc`).
  - Queries active, non-cancelled bookings assigned to each lane (`LaneDto.NextBookingToday`).
- **Contextual Floor Card Display**:
  - When an upcoming booking is found for today, the lane card dynamically renders a high-visibility badge:
    - `📅 NEXT BOOKING TODAY`: Formatted start time (e.g. `10:08 PM`).
    - Lead guest name (e.g. `Sarah Connor`).
    - Party size and booking reference (e.g. `4 throwers • #WA-80358`).
  - Displayed on both idle lanes (giving staff advance notice of incoming arrivals) and active throwing lanes (alerting coaches when a match must conclude to avoid turnaround delays).
  - Lanes with zero future bookings today maintain a streamlined appearance without clutter.

---

## 4. Session Control Operations

### 4.1 Quick Dispatch Actions
- **Start Walk-in**:
  - One-click modal to enter party name, thrower count, duration (30, 60, 90m), and select payment method.
- **Assign Scheduled Booking**:
  - Displays list of expected arrivals for the current hour.
  - Automatically loads thrower roster and signed waivers into the lane session.
- **Add Time / Upsell Extension**:
  - Instantly add `+15 min`, `+30 min`, or `+60 min` with automatic card charge or POS balance posting.
- **Lane Transfer / Re-assignment**:
  - `POST /api/admin/lanes/{sourceLaneId}/transfer-to/{targetLaneId}`:
  - Move an active session from Lane 2 to Lane 5 (e.g. if Lane 2 target wood splinters); all scores, game history, and timer state migrate instantly via SignalR.
- **Match Rematch & Reset**:
  - `POST /api/lanes/operations/{laneId}/rematch`:
  - Starts a fresh match within the active session while preserving elapsed session countdown and booking linkage.
- **Graceful End Session**:
  - `POST /api/lanes/operations/{laneId}/end-session`:
  - Ends the active session, marks `SessionStatus.Completed`, transitions bay to `LaneStatus.Turnaround`, and notifies paired terminals.
- **Player Substitution**:
  - `POST /api/lanes/operations/{laneId}/substitute`:
  - Allows in-session live replacement of a thrower on the tablet console.

### 4.2 Multi-Lane & Corporate Group Tournament Mode
- **Group Bay Linking**:
  - Combine multiple lanes (e.g. Lanes 7, 8, 9, and 10) into a single corporate event hub.
  - Displays aggregate leaderboard on all paired overhead monitors.
  - Centralized tournament bracket management (Single Elimination, Round Robin, Battle Royale).

---

## 5. Hardware Device Pairing & Terminal Management

### 5.1 Zero-Touch Device Pairing Protocol
1. Open the tablet URL (`http://localhost:5173/tablet`) on any iPad, Android, or browser tablet.
2. The screen displays a dynamic pairing PIN (or uses seeded PINs, e.g. `AX101`).
3. Venue staff connects to the target lane.
4. Paired terminal stores token in persistent client state.
5. The tablet immediately transitions to the active lane thrower console without requiring username/password entry.
6. Overhead TV screens pair using the exact same flow via `/screen` (e.g. PIN `TV101`).

### 5.2 Device Health Monitoring & Automated Lifecycle Worker
- **Real-Time Heartbeat Tracking**:
  - Terminals transmit `SendHeartbeat` via `LaneHub` every 10 seconds.
  - Updates `Lane.LastHeartbeatAt` in the database.
  - Admin floor plan and lane cards display online/offline status indicators.
- **Session Lifecycle Background Service**:
  - `SessionLifecycleBackgroundService` runs as an ASP.NET Core `IHostedService` checking every 15 seconds.
  - Automatically transitions expired sessions (`ExpiresAt <= UtcNow`) to `Completed` and sets the lane to `Turnaround`.
  - Dispatches `OnLaneStateChanged` to all SignalR groups for immediate synchronized UI updates.

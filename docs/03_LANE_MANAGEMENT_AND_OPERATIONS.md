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

### 2.2 Lane State Lifecycle
```
+-----------------------------------------------------------------------------------+
|                                LANE STATE MACHINE                                 |
+-----------------------------------------------------------------------------------+

     [ Available / Idle ] <-----------------------+
              |                                   |
              | (Walk-in or Booking Check-In)     | (Reset / Clean Complete)
              v                                   |
       [ Assigned / Prep ]                        |
              |                                   |
              | (Safety Briefing & Game Launch)   |
              v                                   |
       [ Active Throwing ]                        |
              |                                   |
              | (Timer reaches 00:00)             |
              v                                   |
     [ Session Expired ]                          |
              |                                   |
              | (Extend Time or End Session)      |
              v                                   |
    [ Turnaround / Cleaning ] --------------------+
              ^
              | (Emergency or Maintenance toggle)
              v
    [ Maintenance / Out of Order ]
```

### 2.3 State Definitions & Triggers
1. **Available (Idle)**: Lane is clean, calibrated, and ready for immediate walk-ins or scheduled bookings.
2. **Reserved (Upcoming)**: A reservation is scheduled within the next 30 minutes; card displays arrival name and group size.
3. **Assigned / Prep**: Guests have arrived, waivers verified, coach assigned; tablet displays welcome screen.
4. **Active Throwing**: Timer running, games in progress, telemetry broadcasting to overhead screen.
5. **Time Warning**: Less than 5 minutes remaining; card flashes subtle amber alert; staff prompted to offer session extension.
6. **Session Expired**: Timer hits 00:00; tablet screen pauses games and shows session recap; TV screen switches to congratulatory/drink order screen.
7. **Turnaround / Cleaning**: 5-10 minute buffer period for axe inspection, target board wetting/brushing, and sanitization.
8. **Maintenance / Out of Order**: Lane blocked for timber replacement or target repairs.

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
  - Move an active session from Lane 2 to Lane 5 (e.g. if Lane 2 target wood splinters); all scores, game history, and timer state migrate instantly via SignalR.

### 4.2 Multi-Lane & Corporate Group Tournament Mode
- **Group Bay Linking**:
  - Combine multiple lanes (e.g. Lanes 7, 8, 9, and 10) into a single corporate event hub.
  - Displays aggregate leaderboard on all paired overhead monitors.
  - Centralized tournament bracket management (Single Elimination, Round Robin, Battle Royale).

---

## 5. Hardware Device Pairing & Terminal Management

### 5.1 Zero-Touch Device Pairing Protocol
1. Open the tablet URL (`https://lane.venueaxe.com`) on any iPad, Android, or browser tablet.
2. The screen displays a dynamic 6-character Pairing PIN (e.g. `AX-842`).
3. Venue staff enters the PIN in the Admin Lane Settings and chooses the target lane (e.g., `Lane 3 Tablet`).
4. Backend issues a scoped device credential stored in local persistent storage.
5. The tablet immediately transitions to the active lane thrower console without requiring username/password entry.
6. Overhead TV screens pair using the exact same flow via `https://screen.venueaxe.com`.

### 5.2 Device Health Monitoring
- Real-time heartbeat tracking: WebSocket ping every 10 seconds.
- Alerts staff if a tablet loses Wi-Fi connection, runs low on battery, or experiences browser focus loss.

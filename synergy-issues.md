# VenueAxe - Cross-System Synergy & Workflow Integration Report

**Documented:** September 2026  
**Audited Modules:** Booking Engine, Lane Allocation, Digital Waiver Vault, In-Lane Hardware Terminals, Overhead Broadcast Displays, and Operations Admin.  
**Author:** Automated Product Quality & Workflow Synergy Audit

---

## Executive Summary

A commercial venue management platform succeeds when its constituent subsystems (bookings, waivers, hardware terminals, scoring engines, and administrative schedules) behave as an integrated, reactive ecosystem. Inconsistencies or gaps where changes in one domain fail to propagate or enrich another domain lead to administrative double-handling, customer delays at check-in, and operational friction.

This document identifies key areas across VenueAxe where subsystems operate in isolation rather than leveraging natural synergies.

---

## 1. Digital Waivers $\longleftrightarrow$ In-Lane Tablet Roster Synergy

### Current Behavior:
- Customers complete signed digital waivers either online prior to arrival (`/sign/w/[bookingReference]`) or at the venue kiosk (`/sign/[venueSlug]`).
- The signed waiver captures legal participant names, emails, phone numbers, and minor endorsements in `WaiverVault`.
- When an Axe Coach or Lane Master launches a match session on `/tablet` or `/admin/lanes`, the session launcher presents a blank or generic text field (`playerNames: "Thrower 1, Thrower 2"`).

### Desired Synergy:
- **Automatic Roster Pre-population from Signed Waivers:**
  When a lane is assigned to an active booking (`VA-84920`), the tablet launcher and admin launcher should automatically query signed waivers linked to that `BookingId`.
  - Instead of retyping names, the tablet should show:  
    `Select Signed Throwers for this Bay:`  
    ☑️ Marcus Vance  
    ☑️ Sarah Vance  
    ☑️ Johnny Vance (Minor)  
  - 1-tap select imports their names into the match roster and marks their arrival in the booking audit log.

---

## 2. Lane Deactivation $\longleftrightarrow$ Public Booking Engine Availability Synergy

### Historical & Current Behavior:
- In earlier revisions, deactivating a lane (`IsActive = false`) did not prevent the public booking engine from allocating that bay to future slots.
- While the fix was introduced to prevent inactive lanes from being assigned, the public booking wizard does not communicate why capacity may be restricted.

### Desired Synergy:
- **Reactive Maintenance Lockout & Capacity Recalculation:**
  - When an admin marks a lane as inactive (e.g. for target board replacement or cage repairs), any pending reservations assigned to that lane should immediately flag a conflict alert in `/admin/bookings` with a 1-click **"Auto-reallocate to adjacent open bay"** action.
  - If no adjacent bays exist, the system should prompt the operator before finalizing deactivation rather than quietly dropping venue capacity.

---

## 3. Emergency Safety Stop $\longleftrightarrow$ Global Admin Navigation Synergy

### Current Behavior:
- Clicking `⚠️ Safety Stop` on Lane 01 in `/admin/lanes` dispatches SignalR messages to lock the tablet and overhead TV in a high-contrast red emergency warning screen.
- However, if the operator navigates away from `/admin/lanes` to `/admin/bookings`, `/admin/schedule`, or `/admin/editor`, there is **no persistent indicator** in the global admin navigation header indicating that a bay is actively frozen under safety stop.

### Desired Synergy:
- **Global Arena Safety Beacon in Top Navigation:**
  - A persistent, pulsing red indicator in the top navbar (`🚨 BAY 01 FROZEN - SAFETY STOP ACTIVE`) visible across *all* admin pages with a quick **"View / Clear Stop"** drawer.
  - Ensures staff members across the facility are immediately aware of safety interventions regardless of what screen they have open.

---

## 4. Lane Schedule Matrix $\longleftrightarrow$ Direct Booking & Session Launch Synergy

### Current Behavior:
- `/admin/schedule` displays a 2D timeline matrix of bays vs. operating hours.
- Clicking an empty slot displays a visual border highlight, but performs no action.
- To book or launch a session into that slot, the manager must leave the matrix, navigate to `/admin/bookings` or `/admin/lanes`, and re-select the lane and time manually.

### Desired Synergy:
- **Contextual Schedule Matrix Action Drawer:**
  - Clicking any open block on the matrix should open a contextual action slide-out:
    - `[+ New Reservation]` (pre-fills selected bay, date, and start time).
    - `[+ Quick Walk-In Session]` (launches immediate bay timer and pairs terminals).
    - `[🔧 Mark Bay Maintenance Window]` (blocks the bay for 30/60 minutes).

---

## 5. Rematch Session Action $\longleftrightarrow$ Booking Duration Remaining Synergy

### Current Behavior:
- On `/tablet`, clicking `🔁 Rematch` resets scores to 0 and starts a new match.
- It resets the match regardless of how much time remains on the customer's paid bay session (even if only 3 minutes remain on an hour-long reservation).

### Desired Synergy:
- **Remaining Time Awareness & Overtime Guard:**
  - When `Rematch` is tapped:
    - If remaining session time is $< 10$ minutes (the average time to throw a 10-round match), the tablet should prompt:  
      `⚠️ Only 6 minutes remaining on this bay reservation. Launch rematch anyway or request a 30-min extension?`  
    - Includes a `[Request +30 Min Extension]` button that notifies the front desk POS.

---

## 6. Balance Due / Deposit Status $\longleftrightarrow$ Session Launch Check Synergy

### Current Behavior:
- When a customer books online paying a deposit, their booking record has `PaymentStatus = "DepositPaid"` and a calculated `BalanceDueCents`.
- In `/admin/lanes`, the lane card shows an upcoming booking and a "Collect Balance" button.
- However, staff can click `+ Start Session` and launch axes into wood without any reminder or restriction that the customer still owes an unpaid balance.

### Desired Synergy:
- **Pre-Flight Payment Gate on Match Launch:**
  - If a lane session is being launched for a booking with an outstanding balance, the session launch modal should display an amber warning badge:  
    `⚠️ Outstanding Balance Due: $45.00`  
    `[Collect Cash / Card Now]` or `[Authorize Staff Override & Launch]`  
  - Prevents revenue leakage from walk-ins or group bookings walking out before paying the remainder of their tab.

---

## 7. Operating Hours Editor $\longleftrightarrow$ Live Booking & Matrix Time Range Synergy

### Current Behavior:
- When venue hours are updated in `/admin/settings` (e.g., closing at 10:00 PM instead of 11:00 PM on Sundays), changes are saved to the database JSON.
- However, open browser tabs on `/admin/schedule` and `/book/[venueSlug]` continue rendering time slots based on previously loaded configurations until a hard browser refresh.

### Desired Synergy:
- **Real-Time Operational Hours Propagation:**
  - Triggering hours changes should broadcast an update to the public booking engine and admin matrix, immediately adjusting the visible booking slot grid and preventing booking attempts for newly closed hours.

# VenueAxe - Ergonomic Issues & Interaction Friction Report

**Documented:** September 2026  
**Audited Surface:** In-Lane Tablet (`/tablet`), Overhead TV (`/screen`), Customer Booking (`/book/[slug]`), Digital Waiver Kiosk (`/sign/[slug]`), and Venue Admin Portal (`/admin/*`).  
**Status:** Items 1–6 Implemented & Verified. Zero compiler warnings. All test suites passing.

---

## Executive Summary

During the comprehensive end-to-end browser walkthrough of VenueAxe, all core workflows executed successfully with zero fatal exceptions. However, several user interactions and physical touch Ergonomic Friction Points were discovered. In a high-throughput, tactile environment like a commercial axe throwing venue—where throwers have chalk on their hands, tablets are mounted on timber posts, and lane masters manage high noise levels—ergonomic polish directly impacts revenue, safety, and operational turnaround time.

---

## 1. Customer Booking Wizard (`/book/[venueSlug]`)

### 1.1 Party Size Stepper Quick-Select Chips
- **Status:** ✅ **RESOLVED**
- **Observation:** In `/book/[venueSlug]`, changing group size from 2 to 16 required tapping the `+` stepper button 14 consecutive times.
- **Solution Implemented:** Added 1-tap quick-select chips (`2`, `4`, `6`, `8`, `12`, `16`) directly under the stepper with active state styling and instant capacity/timeslot re-fetch.

---

## 2. Digital Waiver Kiosk (`/sign/[venueSlug]`)

### 2.1 Native Date-of-Birth Input Friction on Touch Terminals
- **Observation:** The Date of Birth input relies on the native HTML5 `<input type="date">`.
- **Note:** Standard format maintained.

### 2.2 Signature Validation Feedback
- **Status:** ⏸️ **SKIPPED PER USER INSTRUCTION** ("don't worry about stroke validation").

---

## 3. In-Lane Tablet Console (`/tablet`)

### 3.1 Killshot Call Remaining Count Badge on Button
- **Status:** ✅ **RESOLVED**
- **Observation:** Players have an allotment of 2 Killshots per 10-throw match. While the button disabled once exhausted, the button label did not indicate how many remaining calls the active thrower had.
- **Solution Implemented:** Dynamic button label: `🎯 CALL KILLSHOT (8 PTS) [2 Left]` $\rightarrow$ `[1 Left]` $\rightarrow$ `🎯 KILLSHOTS EXHAUSTED [0 Left]`, with armed pulse indicator.

### 3.2 "Undo Throw" Header Tactile Chip
- **Status:** ✅ **RESOLVED**
- **Observation:** The `↩️ Undo Throw` button was located only in the bottom secondary controls.
- **Solution Implemented:** Added a prominent, tactile `↩️ Undo` chip directly adjacent to the active thrower's score box on the scoreboard header for instant 1-tap corrections.

### 3.3 Target Board Hit Feedback & Toast Timing
- **Status:** ✅ **RESOLVED**
- **Observation:** Rapid turn advancement gave throwers insufficient visual confirmation of points awarded.
- **Solution Implemented:** Added an animated high-contrast hit toast badge (`+6 BULLSEYE! 🎯`, `+8 KILLSHOT! 🎯`, `+X POINTS!`, `FAULT ⚠️`, `DROP ❌`, `MISS`) centered directly over the target board SVG on every throw event.

---

## 4. Overhead TV Broadcast Display (`/screen`)

### 4.1 Pairing Code Visibility & Auto-Connection
- **Status:** ✅ **RESOLVED**
- **Observation:** Displays mounted overhead need automatic or query-parameter pairing without physical peripherals.
- **Solution Implemented:** Overhead TV automatically checks for `?pin=...` query parameters (e.g. `/screen?pin=TV101`) and persists connection state in `localStorage`.

---

## 5. Venue Admin Portal (`/admin/*`)

### 5.1 Lane Schedule Matrix (`/admin/schedule`) Empty Slot Interaction
- **Status:** ✅ **RESOLVED**
- **Observation:** Clicking on empty schedule slots did not open an action modal.
- **Solution Implemented:** Added interactive hover buttons for each empty hour slot. Clicking opens an Open Slot Action dialog with 1-tap deep links:
  - `[📅 Reserve / Create New Booking]` $\rightarrow$ Pre-fills lane number, date, and timeslot in `/admin/bookings`.
  - `[🎯 Launch Walk-in Session Now]` $\rightarrow$ Pre-selects lane and launches session in `/admin/lanes`.

### 5.2 Filter Preservation Across Navigation (`/admin/bookings`)
- **Status:** ✅ **RESOLVED**
- **Observation:** Filtering bookings by date or customer in `/admin/bookings` reset to empty when switching tabs.
- **Solution Implemented:** Bi-directional synchronization between URL query parameters (`?q=...&date=...`) and Svelte reactive filters using `$effect` and `goto(..., { replaceState: true })`. Pre-fills and preserves active search states across all navigation.

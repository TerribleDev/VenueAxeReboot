# 07 - Bug Fixes & Ergonomic Enhancements Architecture & Implementation Guide

## Executive Overview
This document records the architectural improvements, defect resolutions, and human-computer interaction (HCI) ergonomic enhancements implemented in **VenueAxe** to eliminate failure pits, protect venue revenue, and ensure unassisted guest and coach operations.

---

## 1. Defect Resolutions & Technical Architecture

### 1.1 Multi-Tenant Repository Query Filter Bypasses (BUG-001, BUG-002, BUG-003)
- **Problem:** EF Core's global multi-tenant query filter (`e.TenantId == CurrentTenantId`) returned `null` or empty lists for unauthenticated endpoints. This prevented:
  1. In-lane tablet hardware terminals (`/tablet`) from calling `EndSessionAsync` without an owner cookie (BUG-001).
  2. Public visitors (`/book/[venueSlug]`) from finding lanes during availability queries, causing 0 available slots to be calculated (BUG-002).
  3. Public pre-arrival party waiver links (`/sign/w/[ref]`) from getting accurate counts of signed party members (BUG-003).
- **Architecture Remedy:**
  - Added `GetByIdIgnoreQueryFiltersAsync(Guid laneId)` and updated `GetByVenueIdAsync`, `GetUpcomingBookingsByLaneAsync`, and `CountSignedForBookingAsync` in `EfRepositories.cs` to explicitly apply `.IgnoreQueryFilters()`.
  - Unauthenticated hardware terminals and public guests can now read relevant lane/session/waiver telemetry while mutative operations continue to enforce domain consistency.

### 1.2 WATL Coordinate Collision & Line-Breaking Overrides (BUG-004)
- **Problem:** When an axe blade straddles a ring boundary ($\pm 0.015$ threshold), the tablet prompts the coach ("Award Higher" vs "Lower"). Previously, selecting "Award Higher" sent raw Euclidean coordinates; the backend recalculated the distance and downgraded the throw to the lower ring.
- **Architecture Remedy:**
  - `WatlTarget.svelte` now computes `higherManualZone` during line-break detection.
  - Selecting "Award Higher" forwards `manualZone: higherManualZone` to `LaneOperationsController.cs`.
  - The controller respects `manualZone`, bypassing Euclidean recalculation and granting the official higher ring value.

### 1.3 Touchscreen Double-Throw De-duplication (BUG-005)
- **Problem:** Touch devices fire both `touchstart` and a delayed synthetic `click` event (~300ms later), causing two consecutive throws to be recorded for successive players on a single tap.
- **Architecture Remedy:**
  - Removed redundant `ontouchstart` listener on the target SVG in `WatlTarget.svelte`.
  - Standardized all throw taps on pointer-aware `onclick` handlers with coordinate normalization.

### 1.4 Dynamic 3x3 Tic-Tac-Toe Grid Telemetry (BUG-006)
- **Problem:** When playing Axe Tic-Tac-Toe, `<WatlTarget />` on the tablet did not receive territory claims, displaying a standard target while the TV screen showed the grid.
- **Architecture Remedy:**
  - Derived `tttGrid` (array of 9 cells: `'X'`, `'O'`, or `null`) from `gameState.allThrows` and `gameState.players`.
  - Passed `tttGrid={tttGrid}` to `<WatlTarget />` across both `/tablet` and `/screen`, synchronizing real-time territory cell claims.

### 1.5 Arcade Objective HUDs & Scannable Attract Loop (BUG-007)
- **Problem:** Overhead TV monitor showed plain text without game-specific HUDs (Around-the-World ring milestones, Blackjack sums) and lacked scannable mobile links in idle mode.
- **Architecture Remedy:**
  - Built `QrCode.svelte` using native SVG matrix rendering for mobile phone scanning.
  - Implemented `.arcade-hud-bar` on `/screen` dynamically rendering ring targets (1 through 7 Bullseye), Blackjack 21 running hand sum / bust alerts, and Countdown 301 distance to zero.
  - Added scannable QR cards in the attract loop for instant smartphone waiver signing (`/sign/downtown`) and lane-side beer/food concessions.

### 1.6 Digital Waiver Intake Validation (BUG-008)
- **Problem:** Mobile keyboards on `/sign/w/[ref]` and `/sign/[venueSlug]` caused silent submission failures when date formats or offscreen checkboxes were missed.
- **Architecture Remedy:**
  - Added explicit client-side validation (`validateIntake()`) checking non-empty names, valid email, past date of birth, drawn signature, and terms acknowledgment.
  - Inline high-visibility error banners display immediate feedback if any requirement is unfulfilled.

### 1.7 Svelte 5 Nested Block Constraints & Admin Bookings 500 Resolution (BUG-009)
- **Problem:** Navigating to `/admin/bookings` resulted in a 500 server-side compilation exception. Svelte 5 enforces strict placement for `{@const}` tags: `{@const} must be the immediate child of {#snippet}, {#if}, {:else if}, {:else}, {#each}, {:then}, {:catch}, <svelte:fragment>, <svelte:boundary> or <Component>`. Placing `{@const balDueCents = ...}` inside a standard `<td>` and `{@const modalBalDue = ...}` inside a `<div>` crashed the component compiler. Additionally, OpenAPI-generated string/number union types triggered TypeScript subtraction errors.
- **Architecture Remedy:**
  - Relocated all `{@const}` expressions to be immediate root children of their enclosing `{#each filteredBookings}` and `{#if selectedBookingDetail}` blocks.
  - Introduced the strongly typed `getBookingFinancials(b)` utility ensuring deterministic numerical coercion (`Number(b.totalAmountCents) || 0`) across both table rows and detail modals.
  - Verified with `svelte-check --threshold error` (0 errors).

### 1.8 Arena Grid Tile Architecture for Lanes Overview (`/admin/lanes`) (BUG-010)
- **Problem:** When lane views were modularized into separate route endpoints, `.lanes-grid` and `.lane-card` lacked scoped CSS rules, collapsing the physical lane bay cards into full-width stacked rows rather than a multi-column sports arena card grid.
- **Architecture Remedy:**
  - Implemented responsive grid styling in `src/frontend/src/routes/admin/lanes/+page.svelte`:
    - `.lanes-grid`: `display: grid; grid-template-columns: repeat(auto-fill, minmax(350px, 1fr)); gap: 1.5rem;`
    - `.lane-card`: Glassmorphism panel styling with `backdrop-filter: blur(16px)`, border radius `var(--radius-lg)`, and animated hover elevation (`transform: translateY(-2px); box-shadow: 0 10px 30px rgba(0,0,0,0.5)`).
    - `.card-active`: Warm amber gradient pulse with glowing border indicating live matches.
    - Standardized `.lane-header`, `.session-box`, `.timer-display`, `.empty-lane-box`, and `.pair-row` components.

### 1.9 Reactive State Binding & Digital Waiver Checkbox Interaction (BUG-011)
- **Problem:** In `/sign/w/[bookingReference]`, `termsAccepted` was missing an explicit `$state(false)` declaration at the script root, causing the liability terms checkbox to fail to update reactively on touch/click.
- **Architecture Remedy:**
  - Added `let termsAccepted = $state(false);` to the top-level form state block in `src/frontend/src/routes/sign/w/[bookingReference]/+page.svelte`.
  - Upgraded the checkbox container touch targets with full flex width, `accent-color: var(--accent-amber)`, and `cursor: pointer` on both the input and text label for effortless guest interaction on mobile devices.

---

## 2. Ergonomic Failure Pit Resolutions

### 2.1 Pit 1: Ambiguous Deposit Balances & Uncollected Revenue
- **Solution:**
  - Updated `BookingDto` and `NextBookingSummaryDto` with `totalAmountCents`, `paidAmountCents`, and `paymentStatus`.
  - Added high-contrast badge: `🟡 DEPOSIT PAID — $250.00 DUE` in both `admin/bookings` and `admin/lanes`.
  - Added `PUT /api/admin/bookings/{id}/payment` and 1-click **"Collect Remaining Balance"** buttons in both the reservations table and lane overview cards.

### 2.2 Pit 2: Missing Waiver Chokepoint & Party Progress Checklist
- **Solution:**
  - In `/sign/w/[bookingReference]`, added the **Party Waiver Checklist**:
    - Real-time counter: e.g. `2 / 6 Waivers Signed` with colored completion bar.
    - Dynamic status badge: `⚠️ 4 more waivers needed` or `✓ Party 100% Cleared`.
    - **"🔗 Copy Shareable Waiver Link"** 1-click clipboard button for easy texting to group members.

### 2.3 Pit 3: In-Lane "Waiting for Session" Bay Deadlock
- **Solution:**
  - Replaced the passive "Waiting for Game Session" placeholder on `/tablet` with the interactive **Lane Pre-Session Lobby**.
  - Guests or lane coaches can enter thrower names, select from 6 game modes, and click **"🚀 Launch Match Session"** directly from the lane tablet without walking over to the front desk.

### 2.4 Pit 4: Destructive "End Session" Button Proximity
- **Solution:**
  - Replaced immediate session termination with the **Protected End Session Modal**.
  - Requires explicit confirmation and offers an optional Coach PIN (`1234`) to prevent accidental guest touches.

### 2.5 Pit 5: Mid-Session Locked Game Mode Trap
- **Solution:**
  - Added `POST /api/lanes/operations/{laneId}/switch-game` to `ILaneGameService` and `LaneControllers.cs`.
  - Added **"🎮 Switch Game"** button on the tablet opening a 6-card game catalog.
  - Players can transition from standard WATL to Axe Tic-Tac-Toe, Around-The-World, or Blackjack 21 while preserving elapsed session time and active rosters.

### 2.6 Pit 6: Abrupt Match Termination at Expiry
- **Solution:**
  - Updated `SessionLifecycleBackgroundService.cs` with a 3-minute grace period (`s.ExpiresAt.AddMinutes(3) <= now`) for matches with active throws.
  - Added overtime alert banner on TV monitor letting players finish their championship round.

### 2.7 Pit 7: 15–20 Foot Overhead TV Legibility
- **Solution:**
  - Implemented 10-foot UI styling across `/screen`:
    - Active thrower name: `3.2rem` font size.
    - Current score: `4.5rem` golden amber display.
    - Match leaderboard: high-contrast dark arena rows with glowing active border.

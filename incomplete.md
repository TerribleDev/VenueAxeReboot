# VenueAxe - Incomplete, Placeholder & TODO Feature Audit

**Generated:** September 2026  
**Target Platform:** .NET 10 Web API + PostgreSQL 17 + SvelteKit 2 (Svelte 5)  
**Reference Specifications:** [PRODUCT_SPECIFICATION.md](file:///d:/projects/VenueAxe/docs/PRODUCT_SPECIFICATION.md), [docs/](file:///d:/projects/VenueAxe/docs), [backlog.md](file:///d:/projects/VenueAxe/backlog.md)

---

## Executive Summary

An exhaustive audit of the VenueAxe codebase against the architectural standards ([GEMINI.md](file:///d:/projects/VenueAxe/GEMINI.md)) and modular system specifications ([docs/](file:///d:/projects/VenueAxe/docs)) identified several placeholder implementations, stubbed routines, missing endpoints, raw JSON inputs, and unfulfilled specification requirements.

The items below are cataloged with exact file references, line numbers, descriptions of what is currently present, and the full implementation needed to achieve compliance.

---

## Summary Matrix of Incomplete & Placeholder Features

| # | Feature / Area | File Reference | Current State | Missing / Incomplete Scope |
| :--- | :--- | :--- | :--- | :--- |
| **1** | **Square Payment Tokenization** | [`SquarePaymentElement.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/lib/components/SquarePaymentElement.svelte#L70-L80) | Custom HTML form returning hardcoded sandbox nonce `cnon:card-nonce-ok`. | Official Square Web Payments SDK integration (`Square.payments()`), Apple Pay, Google Pay, and buyer verification. |
| **2** | **Square Webhook Processing** | [`PublicBookingController.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Areas/Public/Controllers/PublicBookingController.cs#L58-L75) | Validates signature but ignores request body; returns `{ status = "received" }`. | Payload parsing (`payment.updated`), booking lookup by transaction reference, payment status update, and confirmation triggers. |
| **3** | **Deposit vs. Paid Status** | [`ApplicationServices.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/ApplicationServices.cs#L736-L743) | `PaymentStatus` is hardcoded to `"Paid"` even when only a partial deposit was collected. | Distinction between `"DepositPaid"` / `"Partial"` and `"PaidInFull"`; remaining balance calculation for check-in collection. |
| **4** | **Countdown Engine Undo** | [`StandardEngines.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/GameEngine/StandardEngines.cs#L508) | `UndoLastThrow` is a stub: `public GameStateSnapshot UndoLastThrow(GameStateSnapshot state) => state;`. | Turn reversal, score restoration from history, and bust state rollback. |
| **5** | **Countdown Scatter Heatmap** | [`StandardEngines.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/GameEngine/StandardEngines.cs#L480-L495) | Throws are not appended to `state.AllThrows`. | Throws must be recorded in `AllThrows` so the end-of-match scatter heatmap renders coordinates. |
| **6** | **Clutch Hunter Game Engine** | [`docs/04_LANE_GAMES_AND_WATL_SCORING.md`](file:///d:/projects/VenueAxe/docs/04_LANE_GAMES_AND_WATL_SCORING.md#L174-L176) | Specified in documentation, but missing in code. | `ClutchHunterEngine` class implementing `IGameEngine` where only Bullseye (6) and Clutch (7/8) score. |
| **7** | **Tic-Tac-Toe SVG Target Skin** | [`WatlTarget.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/lib/components/WatlTarget.svelte#L5-L10) | Supports only `'watl'` and `'iatf'` skins. | 3x3 territory grid overlay, team color markers (Red/Blue), and cell claim visual indicators. |
| **8** | **Overhead TV Arcade HUD** | [`screen/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/screen/+page.svelte#L170-L230) | TV only displays standard WATL rings and scalar points. | Specialized broadcast layouts for Tic-Tac-Toe 3x3 grid, Around-the-World quest progress, and Blackjack 21 totals. |
| **9** | **WATL Line-Breaking Toggle** | [`WatlTarget.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/lib/components/WatlTarget.svelte#L43-L77) | Tap coordinates strictly map to single radii without boundary assistance. | Calibrated boundary threshold detection and "Line Touch / Higher Value" toggle. |
| **10** | **Substitute Player Action** | [`tablet/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/tablet/+page.svelte#L340-L380) | Only "Skip Turn" is implemented. | "Substitute Player / Change Turn Order" dialog in the tablet scorekeeper dock. |
| **11** | **Rematch Session Handling** | [`tablet/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/tablet/+page.svelte#L203-L215) | Rematch creates a new `LaneSession` with hardcoded 60m duration. | Reset match within existing session, preserving elapsed session timer and booking reference. |
| **12** | **Graceful Session End Action** | [`LaneGameService.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/LaneGameService.cs) & [`LaneOperationsController.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Areas/Lanes/Controllers/LaneControllers.cs) | No `EndSession` or `CompleteSession` API method. | Explicit endpoint to end an active session, mark status `Completed`, and transition lane to `Turnaround`. |
| **13** | **Waiver Audit PDF Export** | [`BookingsWaiversAndConfigControllers.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Areas/Admin/Controllers/BookingsWaiversAndConfigControllers.cs#L60-L79) | Missing `GET /api/admin/waivers/{id}/pdf`. | PDF generation service rendering legal audit record, SHA-256 hash, and drawn signature image. |
| **14** | **Waiver Template Builder** | [`WaiversController.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Areas/Admin/Controllers/BookingsWaiversAndConfigControllers.cs) & [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte) | No template editor in Admin; no backend update endpoints. | Admin UI and API to edit Markdown legal text, insert dynamic variables (`{{VenueName}}`), and manage versioning. |
| **15** | **Pre-Arrival Waiver Route** | [`src/frontend/src/routes/sign/`](file:///d:/projects/VenueAxe/src/frontend/src/routes/sign) | Only `/sign/[venueSlug]` exists. | Route `/sign/w/[bookingToken]` for direct group signing without manual venue selection. |
| **16** | **Waiver Form `?ref=` Parameter** | [`sign/[venueSlug]/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/sign/%5BvenueSlug%5D/+page.svelte#L12-L16) | Does not read `page.url.searchParams.get('ref')`. | Auto-populate `bookingReference` when redirected from booking confirmation (`?ref=VA-XXXXX`). |
| **17** | **Floor Plan / Map Mode** | [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte) | Only Tile Grid and Schedule Timeline are present. | Interactive 2D drag-and-drop floor map view representing venue bays and physical layout. |
| **18** | **Lane Transfer / Re-assignment** | [`LaneService.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/ApplicationServices.cs#L357-L548) | No session migration across lanes. | Move active session, timers, rosters, and match state from Lane A to Lane B via SignalR. |
| **19** | **Corporate Tournament Bay Linking** | [`LaneGameService.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/LaneGameService.cs) | Lanes operate strictly in 1:1 isolation. | Multi-lane group bay linking with aggregate leaderboards and bracket management. |
| **20** | **Device Heartbeat & Health Monitoring** | [`ApplicationServices.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/ApplicationServices.cs#L541) & [`LaneHub.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Hubs/LaneHub.cs) | `LastHeartbeatAt` is only stamped on initial pairing. | Recurring 10s WebSocket heartbeat, battery status tracking, and offline warning indicators in Admin. |
| **21** | **Overhead TV Attract Loop** | [`screen/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/screen/+page.svelte#L231-L236) | Static card: `WELCOME TO {laneName}`. | Ambient animated logo, daily high score showcase, and dynamic QR code (digital drink menu & waivers). |
| **22** | **Booking Page Editor JSON Fields** | [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte#L1276-L1298) | Raw `<textarea>` elements for discount rules, booking types, add-ons, and packages. | Form-based editors with input validation for tiers, promo codes, booking types, and packages. |
| **23** | **Visual Theme & Preview Mode** | [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte#L1215-L1315) | No preview or visual customization tools. | Live side-by-side widget preview, color pickers with WCAG AA validation, font pairings, and hero asset controls. |
| **24** | **Background Worker (`IHostedService`)** | Whole Backend | No background services registered. | Periodic worker to transition expired sessions (00:00), flag offline terminals, and send scheduled reminders. |
| **25** | **Staff & User Management** | [`Program.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Program.cs#L43) & [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte) | `IUserRepository` registered, but no controllers or UI exist. | Admin Users tab and API to invite, list, and manage Lane Masters, Managers, and Owners. |
| **26** | **Venue Settings & Hours Editor** | [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte) | `putApiAdminVenuesById` generated but never invoked. | Admin tab/modal to edit venue profile, weekly operating hours, contact info, and branding. |
| **27** | **Calendar & Wallet Booking Links** | [`book/[venueSlug]/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/book/%5BvenueSlug%5D/+page.svelte#L230-L280) | Confirmation only shows plain text reference and waiver button. | "Add to Google Calendar", `.ics` download, Apple Wallet passes, and direct waiver share triggers. |

---

## Detailed Findings & Technical Requirements

### 1. Payment Processing & Square Checkout Integration

#### 1.1 Mock Card Tokenization in `SquarePaymentElement.svelte`
- **Location:** [`SquarePaymentElement.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/lib/components/SquarePaymentElement.svelte#L70-L80)
- **Current Behavior:** Card number, expiry, CVV, and postal code inputs are standard HTML inputs that bypass payment gateways. Clicking tokenize checks regex and directly returns `cnon:card-nonce-ok` (or `cnon:card-nonce-declined`).
- **Required Implementation:**
  1. Load official Square Web Payments SDK script (`https://web.squarecdn.com/v1/square.js`).
  2. Initialize `Square.payments(appId, locationId)`.
  3. Mount secure iframe card element (`await payments.card()`) to ensure PCI-DSS compliance.
  4. Mount digital wallet buttons for Apple Pay, Google Pay, and Square Pay when supported by the client browser.
  5. Call `card.tokenize()` to obtain real client nonces for backend processing.

#### 1.2 Empty Webhook Handler in `PublicBookingController.cs`
- **Location:** [`PublicBookingController.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Areas/Public/Controllers/PublicBookingController.cs#L58-L75)
- **Current Behavior:**
  ```csharp
  bool isValid = await squarePaymentService.VerifyWebhookSignatureAsync(body, signature, webhookUrl);
  if (!isValid) return Unauthorized(new { message = "Invalid webhook signature" });
  return Ok(new { status = "received" });
  ```
  The payload is verified, but never deserialized or processed.
- **Required Implementation:**
  1. Parse JSON payload and identify event type (`payment.updated`).
  2. Extract payment entity (`payment.id`, `payment.status`, `payment.reference_id`, `payment.order_id`).
  3. If status is `COMPLETED`, query `IBookingRepository` for booking matching `SquarePaymentId` or `BookingReference`.
  4. Transition booking status to `Confirmed`, update `PaymentStatus` to `"Paid"`, set `PaidAmountCents`, and persist.
  5. Trigger confirmation email if not already sent.

#### 1.3 Incomplete Partial Deposit Tracking
- **Location:** [`ApplicationServices.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/ApplicationServices.cs#L735-L743)
- **Current Behavior:** When a deposit is paid (e.g. $50 upfront on a $250 booking), `booking.PaymentStatus` is recorded as `"Paid"`.
- **Required Implementation:**
  1. Set `PaymentStatus = "DepositPaid"` when `PaidAmountCents < TotalAmountCents`.
  2. Expose `BalanceDueCents` in `BookingDto` and highlight balance due in the Admin Check-In list so staff can collect the remainder at reception.

---

### 2. Game Engines, Rules & Scoring Subsystem

#### 2.1 Missing `ClutchHunterEngine`
- **Location:** [`src/backend/VenueAxe/GameEngine/`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/GameEngine)
- **Specification:** [`docs/04_LANE_GAMES_AND_WATL_SCORING.md`](file:///d:/projects/VenueAxe/docs/04_LANE_GAMES_AND_WATL_SCORING.md#L174-L176)
- **Required Implementation:**
  1. Create `ClutchHunterEngine : IGameEngine` (`gameTypeId = "clutch_hunter"`).
  2. Rules: Bullseye scores 6 points, Clutch scores 7 points (or 8 for Killshot). All other rings (1 to 5) score 0 points.
  3. Register in `GameEngineRegistry._engines`.

#### 2.2 Stubbed `CountdownGameEngine.UndoLastThrow` & Missing `AllThrows`
- **Location:** [`StandardEngines.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/GameEngine/StandardEngines.cs#L480-L508)
- **Current Behavior:**
  - `UndoLastThrow` is an empty pass-through (`public GameStateSnapshot UndoLastThrow(GameStateSnapshot state) => state;`).
  - `RecordThrow` does not add throw records to `state.AllThrows`.
- **Required Implementation:**
  1. Append each throw record to `state.AllThrows` in `RecordThrow`.
  2. Implement `UndoLastThrow` to pop the last throw, restore the previous player's turn, add back subtracted points (or reverse bust), and decrement round counter when crossing round boundaries.

#### 2.3 Missing 3x3 Territory Grid in `WatlTarget.svelte` for `AxeTicTacToeEngine`
- **Location:** [`WatlTarget.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/lib/components/WatlTarget.svelte)
- **Current Behavior:** Only renders concentric circles for WATL (6 rings) or IATF (3 rings).
- **Required Implementation:**
  1. Support `targetType = 'tictactoe'`.
  2. Render 3x3 SVG grid partition lines over the target coordinates.
  3. Render team color fills (Red / Blue) and icons (X / O) for claimed cells based on `gameState.allThrows`.

#### 2.4 Missing TV Display Layouts for Arcade Modes
- **Location:** [`screen/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/screen/+page.svelte#L170-L230)
- **Current Behavior:** TV screen only shows scalar points and a standard WATL board, regardless of whether the active match is Tic-Tac-Toe, Around-The-World, or Blackjack.
- **Required Implementation:**
  1. For `axe_tictactoe`: Display prominent 3x3 board graphic with team territory dominance bar.
  2. For `around_the_world`: Display quest ring milestones (1 $\rightarrow$ 2 $\rightarrow$ 3 $\rightarrow$ 4 $\rightarrow$ 5 $\rightarrow$ Bullseye $\rightarrow$ Clutch) and active target for each player.
  3. For `blackjack_21`: Display current hand sum, points needed to reach 21, and animated "BUST!" badge when a throw exceeds 21.

#### 2.5 WATL "Line-Breaking" Rule Toggle
- **Location:** [`WatlTarget.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/lib/components/WatlTarget.svelte#L43-L77)
- **Specification:** [`docs/04_LANE_GAMES_AND_WATL_SCORING.md`](file:///d:/projects/VenueAxe/docs/04_LANE_GAMES_AND_WATL_SCORING.md#L64-L66)
- **Required Implementation:**
  1. When a tap occurs within $\pm 0.015$ of a ring perimeter boundary, prompt with a subtle "Line Breaking (+1 pt)" confirmation button.

#### 2.6 Rematch Resetting Active Session Timer
- **Location:** [`tablet/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/tablet/+page.svelte#L203-L215)
- **Current Behavior:** Clicking "Rematch" calls `postApiLanesOperationsByLaneIdStartSession` with `durationMinutes: 60`, which overwrites the lane's active session and resets the expiration clock.
- **Required Implementation:**
  1. Add endpoint `POST /api/lanes/operations/{laneId}/rematch`.
  2. Preserves the active `LaneSession`, elapsed timer, and booking association; appends a fresh `GameMatch` to the current session and broadcasts the initial game state.

#### 2.7 Missing Session End / Complete Action
- **Location:** [`LaneGameService.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/LaneGameService.cs) & [`LaneOperationsController.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Areas/Lanes/Controllers/LaneControllers.cs)
- **Current Behavior:** Sessions can only be ended implicitly by starting another session over them.
- **Required Implementation:**
  1. Add `POST /api/lanes/operations/{laneId}/end-session`.
  2. Sets `LaneSession.Status = SessionStatus.Completed`, `EndedAt = UtcNow`, sets `Lane.CurrentStatus = LaneStatus.Turnaround`, and broadcasts `OnLaneStateChanged`.

---

### 3. Digital Waiver Management System

#### 3.1 Missing Waiver Audit PDF Generation
- **Location:** [`WaiversController.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Areas/Admin/Controllers/BookingsWaiversAndConfigControllers.cs)
- **Specification:** `GET /api/v1/admin/waivers/{id}/pdf` ([docs/06_12_FACTOR_AND_API_ARCHITECTURE.md](file:///d:/projects/VenueAxe/docs/06_12_FACTOR_AND_API_ARCHITECTURE.md#L91))
- **Required Implementation:**
  1. Integrate PDF document generation (e.g., QuestPDF or SkiaSharp).
  2. Endpoint `GET /api/admin/waivers/{id}/pdf` returning `application/pdf`.
  3. Include venue header, participant details, date of birth, minors list, exact agreement Markdown, SHA-256 hash stamp, timestamp, IP address, and drawn vector signature PNG.
  4. Add "Download PDF" action in Admin Waiver Vault.

#### 3.2 Missing Waiver Template Builder & Editor
- **Location:** [`WaiversController.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Areas/Admin/Controllers/BookingsWaiversAndConfigControllers.cs) & [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte)
- **Specification:** [`docs/02_WAIVER_MANAGEMENT.md`](file:///d:/projects/VenueAxe/docs/02_WAIVER_MANAGEMENT.md#L8-L23)
- **Required Implementation:**
  1. Add Admin endpoints:
     - `GET /api/admin/waivers/templates/venue/{venueId}`
     - `PUT /api/admin/waivers/templates/{templateId}`
  2. When updated, compute SHA-256 hash of Markdown content, increment version number, and persist.
  3. Provide Markdown editor in the Admin portal with placeholder inserters (`{{VenueName}}`, `{{SignerFullName}}`, etc.).

#### 3.3 Missing Pre-Arrival Direct Waiver Route
- **Location:** [`src/frontend/src/routes/sign/`](file:///d:/projects/VenueAxe/src/frontend/src/routes/sign)
- **Specification:** [`docs/02_WAIVER_MANAGEMENT.md`](file:///d:/projects/VenueAxe/docs/02_WAIVER_MANAGEMENT.md#L67) (`https://sign.venueaxe.com/w/[booking-token]`)
- **Required Implementation:**
  1. Create route `src/frontend/src/routes/sign/w/[bookingReference]/+page.svelte`.
  2. Resolves booking reference, locks venue automatically, displays party details, and links signed waiver directly to `booking.Id`.

#### 3.4 Unread `?ref=` Query Parameter on Public Waiver Kiosk
- **Location:** [`sign/[venueSlug]/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/sign/%5BvenueSlug%5D/+page.svelte#L12-L16)
- **Current Behavior:** Clicking "Sign Digital Waiver Now" from booking confirmation passes `?ref=VA-XXXXX`, but line 12 only reads `kiosk`.
- **Required Implementation:**
  ```typescript
  let bookingReference = $state(page.url.searchParams.get('ref') ?? '');
  ```

---

### 4. Venue Floor Operations & Hardware Pairing

#### 4.1 Missing 2D Floor Plan / Map Mode
- **Location:** [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte)
- **Specification:** [`docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md`](file:///d:/projects/VenueAxe/docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md#L11-L14)
- **Required Implementation:**
  1. Add layout mode toggle: `Grid | Timeline | Floor Map`.
  2. Floor Map renders custom 2D canvas/SVG where lanes are positioned as bays across venue floor coordinates.

#### 4.2 Missing Lane Transfer / Re-assignment Action
- **Location:** [`LaneService.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/ApplicationServices.cs#L357-L548)
- **Specification:** [`docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md`](file:///d:/projects/VenueAxe/docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md#L74-L76)
- **Required Implementation:**
  1. Add endpoint `POST /api/admin/lanes/{sourceLaneId}/transfer-to/{targetLaneId}`.
  2. Migrates active session, throws, and timers to the target lane; updates `Lane.CurrentStatus` on both lanes; broadcasts `OnLaneStateChanged` over SignalR.

#### 4.3 Missing Corporate Tournament Bay Linking
- **Location:** [`LaneGameService.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/LaneGameService.cs)
- **Specification:** [`docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md`](file:///d:/projects/VenueAxe/docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md#L78-L82)
- **Required Implementation:**
  1. Allow grouping contiguous lanes (e.g. Bays 1-4) into a tournament group.
  2. Broadcast unified tournament leaderboard across all paired TV monitors.

#### 4.4 Incomplete Device Heartbeat Tracking
- **Location:** [`ApplicationServices.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/Services/ApplicationServices.cs#L541) & [`LaneHub.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web/Hubs/LaneHub.cs)
- **Specification:** [`docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md`](file:///d:/projects/VenueAxe/docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md#L95-L98)
- **Current Behavior:** `LastHeartbeatAt` is only written when a terminal is first paired.
- **Required Implementation:**
  1. Add `SendHeartbeat(Guid laneId, string terminalType, int? batteryLevel)` on `LaneHub`.
  2. Tablets and TVs invoke heartbeat every 10 seconds.
  3. Admin lane cards display online/offline status badge based on `LastHeartbeatAt < UtcNow - 30s`.

#### 4.5 Static TV Attract Loop
- **Location:** [`screen/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/screen/+page.svelte#L231-L236)
- **Current Behavior:** Displays static text: `WELCOME TO {terminalAuth.laneName}`.
- **Required Implementation:**
  1. Render venue logo and ambient floating particles.
  2. Display venue-wide high score leaderboard of the day.
  3. Display dynamic QR code pointing to public waiver signing (`/sign/{venueSlug}`) and venue digital menu.

---

### 5. Booking Page Editor & Visual Customizer

#### 5.1 Raw JSON Textarea Form Controls
- **Location:** [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte#L1276-L1298)
- **Current Behavior:**
  - Discount Rules: `<textarea bind:value={discountRulesJson}>`
  - Booking Types: `<textarea bind:value={bookingTypesJson}>`
  - Add-ons: `<textarea bind:value={addonsJson}>`
  - Packages: `<textarea bind:value={packagesJson}>`
- **Required Implementation:**
  1. Replace raw JSON textareas with structured card-based builders:
     - Add-on item builder (Name, price, price type [flat / per-person], icon).
     - Package builder (Name, price, duration, inclusions).
     - Discount rule builder (Tier party threshold, coupon code, discount percentage).
     - Booking type builder (Standard, Corporate, Private Buyout, off-hours toggle).

#### 5.2 Missing Visual Theme Customizer & Side-by-Side Preview
- **Location:** [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte#L1215-L1315)
- **Specification:** [`docs/01_BOOKING_ENGINE_AND_EDITOR.md`](file:///d:/projects/VenueAxe/docs/01_BOOKING_ENGINE_AND_EDITOR.md#L10-L18)
- **Required Implementation:**
  1. Interactive color pickers (Primary, Accent, Background, Surface) with real-time WCAG AA contrast ratio validation.
  2. Hero banner image uploader / URL input with overlay opacity slider.
  3. Header & body font selectors.
  4. Live side-by-side preview pane reflecting changes instantly.

---

### 6. Enterprise Operations, Staff & Background Automation

#### 6.1 Missing Background Worker (`IHostedService`)
- **Location:** [`src/backend/VenueAxe.Web/`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web)
- **Current Behavior:** No background services run in the web host.
- **Required Implementation:**
  1. Create `SessionLifecycleBackgroundService : BackgroundService` running every 15–30 seconds.
  2. Detect active sessions where `ExpiresAt <= UtcNow`.
  3. Update `LaneSession.Status = SessionStatus.Completed` and `Lane.CurrentStatus = LaneStatus.Turnaround`.
  4. Broadcast `OnLaneStateChanged` to notify TV, Tablet, and Admin dashboards immediately.

#### 6.2 Missing Staff / User Management in Admin Portal
- **Location:** [`VenueAxe.Web`](file:///d:/projects/VenueAxe/src/backend/VenueAxe.Web) & [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte)
- **Current Behavior:** `users` table and `IUserRepository` exist, but there is no `UsersController` or Admin Staff tab.
- **Required Implementation:**
  1. Add `UsersController` with endpoints:
     - `GET /api/admin/users/venue/{venueId}`
     - `POST /api/admin/users` (invite Lane Master or Manager)
     - `PUT /api/admin/users/{id}/role`
     - `DELETE /api/admin/users/{id}`
  2. Add "Staff & Roles" tab in `admin/+page.svelte`.

#### 6.3 Missing Venue Settings & Operating Hours Editor
- **Location:** [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte)
- **Current Behavior:** `putApiAdminVenuesById` is generated in the SDK, but never used in the UI. Venue operating hours and address cannot be updated post-registration.
- **Required Implementation:**
  1. Add "Venue Profile & Hours" tab/modal in Admin.
  2. Weekly day-by-day operating hours editor (Monday through Sunday open/close times and closed day toggles).

#### 6.4 Missing Calendar & Wallet Action Buttons
- **Location:** [`book/[venueSlug]/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/book/%5BvenueSlug%5D/+page.svelte#L230-L280)
- **Specification:** [`docs/01_BOOKING_ENGINE_AND_EDITOR.md`](file:///d:/projects/VenueAxe/docs/01_BOOKING_ENGINE_AND_EDITOR.md#L87-L88)
- **Required Implementation:**
  1. "Add to Google Calendar" button generating web link with venue coordinates and reservation window.
  2. "Download .ics Calendar Event" file generator.
  3. WhatsApp and direct link one-click share buttons for the waiver signing link.

# VenueAxe - Product Roadmap & Groomed Deliverables Backlog

**Version:** 1.0.0  
**Target Platform:** .NET 10 Web API + SvelteKit 2 (Svelte 5) + PostgreSQL 17  
**Architecture Standard:** 12-Factor Multi-Tenant Application  
**Reference Document:** [PRODUCT_SPECIFICATION.md](file:///d:/projects/VenueAxe/docs/PRODUCT_SPECIFICATION.md)

---

## 1. Executive Implementation Status

```
===================================================================================
                         VENUEAXE IMPLEMENTATION STATUS
===================================================================================
[✅ COMPLETE] PostgreSQL 17 + EF Core 10 Multi-Tenant Foundation (UUIDv7, Cookie Auth)
[✅ COMPLETE] Real-Time Lane Telemetry Hub (SignalR Sub-50ms Sync)
[✅ COMPLETE] Venue Admin Operations Center (Lanes Grid, CRUD, Safety Stops, PINs)
[✅ COMPLETE] Reservations List & Check-In Management Table
[✅ COMPLETE] Digital Waiver Vault (Legal capture, signature canvas, real-time search)
[✅ COMPLETE] Visual Booking Page Configuration Editor
[✅ COMPLETE] In-Lane Tablet Scorekeeper Console (Interactive Target, Rapid Keypad)
[✅ COMPLETE] Overhead TV 4K/1080p Broadcast Display (Leaderboard, VFX, Attract Loop)
[✅ COMPLETE] Public Customer Booking Flow (/book/[venueSlug])
[✅ COMPLETE] Public Digital Waiver Signing Kiosk (/sign/[venueSlug])
[✅ COMPLETE] WATL Killshot (8 pts) & IATF Clutch (7 pts) Engine & Target Math
===================================================================================
```

---

## 2. Groomed Deliverables Backlog

### Epic 1: League Rules & Dual-Screen Match Experience

#### **DELIV-1.1: Interactive IATF Target Board Skin**
- **Priority:** High
- **Type:** Frontend / Graphics
- **Description:** Implement an SVG rendering skin for the official International Axe Throwing Federation (IATF) regulation board: 3 concentric scoring rings (Bullseye: 5 pts, Middle Ring: 3 pts, Outer Ring: 1 pt) with two Clutch targets (7 pts).
- **Technical Scope:**
  - Update [`WatlTarget.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/lib/components/WatlTarget.svelte) to toggle between `watl` (6 rings, 1–6 pts) and `iatf` (3 rings, 1/3/5 pts) based on the active match engine.
  - Render high-contrast clutch markers with cyan armed glow.
- **Acceptance Criteria:**
  - Selecting an IATF match renders the authentic 3-ring target layout.
  - Tapping center scores 5 points; middle ring scores 3 points; outer ring scores 1 point; corner clutch scores 7 points when called.

#### **DELIV-1.2: Match Score Correction & Undo System**
- **Priority:** High
- **Type:** Fullstack (SignalR + UI)
- **Description:** Allow lane throwers or lane masters to undo accidental taps or score disputes without restarting the match.
- **Technical Scope:**
  - Connect the "Undo Last Throw" button on [`tablet/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/tablet/+page.svelte) to backend endpoint `POST /api/lanes/operations/{laneId}/undo`.
  - Revert the throw history array, restore the previous thrower's turn, update remaining throws, and broadcast the updated `GameStateSnapshot` via `LaneHub`.
  - Add "Skip Turn / Substitute Player" action in the Tablet dock.
- **Acceptance Criteria:**
  - Clicking "Undo Last Throw" decrements the player's score, restores the previous player turn, and syncs immediately to the Overhead TV.

#### **DELIV-1.3: End-of-Match Podium Summary & Scatter Heatmap**
- **Priority:** Medium
- **Type:** Frontend / Visuals
- **Description:** Display a celebratory podium results screen at match completion featuring player accuracy statistics and an axe throw coordinate scatter heatmap.
- **Technical Scope:**
  - Create `MatchSummaryModal.svelte` and `TVPodiumSummary.svelte`.
  - Plot normalized `(x, y)` coordinate pins across the SVG board for all throws taken during the match.
  - Display Bullseye %, Killshot/Clutch conversion %, and average score per round.
  - Add "Rematch (Same Players)", "Switch Game Mode", or "Return to Attract Loop".
- **Acceptance Criteria:**
  - When the 10th round completes, both Tablet and TV transition into the post-match summary view with podium animations and scatter map.

#### **DELIV-1.4: Pluggable Arcade Game Engines Catalog**
- **Priority:** Medium
- **Type:** Backend / Domain
- **Description:** Implement additional commercial game engine state machines:
  1. `AroundTheWorldEngine`: Sequential ring progression (1 $\rightarrow$ 2 $\rightarrow$ 3 $\rightarrow$ 4 $\rightarrow$ 5 $\rightarrow$ Bullseye $\rightarrow$ Killshot).
  2. `AxeTicTacToeEngine`: 3x3 interactive board grid with team territory claiming.
  3. `Blackjack21Engine`: Target exactly 21 points with bust penalties.
- **Technical Scope:**
  - Implement engines implementing `IGameEngine` in [`StandardEngines.cs`](file:///d:/projects/VenueAxe/src/backend/VenueAxe/GameEngine/StandardEngines.cs).
  - Register in `GameEngineRegistry`.
- **Acceptance Criteria:**
  - All game modes selectable in the Admin start session modal and Tablet lobby, each enforcing their custom victory conditions.

---

### Epic 2: Customer Booking Engine & Embeddable Widget

#### **DELIV-2.1: Stripe Payment Element Integration**
- **Priority:** High
- **Type:** Fullstack (Stripe API + Frontend)
- **Description:** Enable live online credit card and digital wallet (Apple Pay, Google Pay) processing for booking deposits and full payments.
- **Technical Scope:**
  - Implement `IStripePaymentService` in backend to generate Stripe `PaymentIntent` tokens based on venue deposit policy (`FullPayment`, `FixedDeposit`, `PerPersonDeposit`).
  - Embed `@stripe/stripe-js` Payment Element into [`book/[venueSlug]/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/book/[venueSlug]/+page.svelte).
  - Webhook listener `POST /api/public/webhooks/stripe` to mark bookings as `Confirmed` upon `payment_intent.succeeded`.
- **Acceptance Criteria:**
  - Customers can complete checkout using credit card, Apple Pay, or Google Pay.
  - Booking status transitions to `Confirmed` and generates a customer confirmation receipt.

#### **DELIV-2.2: Dynamic Auto-Resizing `<iframe>` Widget Script**
- **Priority:** High
- **Type:** Frontend / Embed SDK
- **Description:** Create an embeddable script allowing venue owners to embed their VenueAxe booking wizard into WordPress, Squarespace, Wix, or custom websites with zero scrollbars.
- **Technical Scope:**
  - Create lightweight embed loader script `venueaxe-widget.js`.
  - Implement window `postMessage` protocol communicating dynamic height changes from child iframe to parent window.
  - Support URL query overrides (`?theme=dark`, `?package=glow`).
- **Acceptance Criteria:**
  - Embedded iframe resizes height seamlessly on step transitions without double scrollbars.

#### **DELIV-2.3: Custom Booking Intake Fields & Add-ons Engine**
- **Priority:** Medium
- **Type:** Fullstack
- **Description:** Render dynamic packages, merchandise add-ons, drinks packages, coaching vouchers, and custom text questions during public booking checkout.
- **Technical Scope:**
  - Parse `BookingConfig.CustomFieldsJson` and `BookingConfig.PackagesJson`.
  - Calculate add-on line items in total price calculation.
  - Store selected responses in `Booking.CustomIntakeResponsesJson`.
- **Acceptance Criteria:**
  - Add-ons selected by the customer dynamically adjust the order total and appear in the Admin reservation details.

---

### Epic 3: Digital Waiver Management & Automated Check-In

#### **DELIV-3.1: Reception Kiosk Standalone Mode with Auto-Reset**
- **Priority:** High
- **Type:** Frontend / Kiosk UI
- **Description:** Provide a locked kiosk mode for check-in tablets positioned at venue reception desks that auto-clears and resets after each guest signs.
- **Technical Scope:**
  - Add `?kiosk=true` URL parameter to [`sign/[venueSlug]/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/sign/[venueSlug]/+page.svelte).
  - When in kiosk mode, hide navigation headers and start a 10-second countdown timer after signature submission that resets the form.
  - Add a manual "Sign Next Waiver" button.
- **Acceptance Criteria:**
  - Kiosk resets automatically to a blank waiver ready for the next guest without staff intervention.

#### **DELIV-3.2: Automated Booking-to-Waiver Linking & Check-In Validation**
- **Priority:** High
- **Type:** Backend / Services
- **Description:** Automatically link submitted waivers to upcoming party reservations via booking reference code or signer email.
- **Technical Scope:**
  - Update `WaiverService.SubmitWaiverAsync` to match active bookings by `BookingReference` or email.
  - Increment `Booking.SignedWaiverCount`.
  - In Admin Reservations table, highlight bookings in green when `signedWaiverCount >= partySize`.
- **Acceptance Criteria:**
  - Signing a waiver with a booking reference immediately updates the waiver counter on the Admin dashboard.

---

### Epic 4: Venue Floor Operations & Lane Management

#### **DELIV-4.1: Admin Walk-In & Phone Reservation Creator**
- **Priority:** High
- **Type:** Fullstack
- **Description:** Add a "+ New Reservation" modal in the Admin Reservations tab allowing venue staff to manually record walk-in parties or phone bookings.
- **Technical Scope:**
  - Implement modal with party size, customer contact, date/time slot picker, and manual payment method selector (Cash, POS Terminal, Comp).
  - Commits directly via `IBookingService.CreateAdminBookingAsync`.
- **Acceptance Criteria:**
  - Staff can create reservations directly in the admin portal and assign lanes immediately.

#### **DELIV-4.2: One-Click Lane Session Time Extensions**
- **Priority:** Medium
- **Type:** Fullstack (SignalR + UI)
- **Description:** Allow staff to extend active lane matches by +15m or +30m with a single click.
- **Technical Scope:**
  - Add quick action buttons `+15 Min` and `+30 Min` on active lane cards in [`admin/+page.svelte`](file:///d:/projects/VenueAxe/src/frontend/src/routes/admin/+page.svelte).
  - Calls `POST /api/lanes/operations/{laneId}/extend` which adjusts `LaneSession.ExpiresAt` and sends `OnSessionExtended` over SignalR.
- **Acceptance Criteria:**
  - Clicking `+15m` updates the session timer on the Admin dashboard, Tablet HUD, and TV screen in real-time.

---

## 3. Implementation Phasing Matrix

| Phase | Milestone Focus | Target Deliverables | Estimated Scope |
| :--- | :--- | :--- | :--- |
| **Phase 1** | **League Match Engine & Score Controls** | DELIV-1.1, DELIV-1.2, DELIV-4.2 | 2-3 Days |
| **Phase 2** | **Kiosk Automation & Booking Linking** | DELIV-3.1, DELIV-3.2, DELIV-4.1 | 2-3 Days |
| **Phase 3** | **Stripe Checkout & Embeddable Widget** | DELIV-2.1, DELIV-2.2, DELIV-2.3 | 3-4 Days |
| **Phase 4** | **Podium Summaries & Arcade Game Modes** | DELIV-1.3, DELIV-1.4 | 2-3 Days |

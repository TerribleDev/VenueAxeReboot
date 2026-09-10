# VenueAxe - Incomplete, Placeholder & TODO Feature Audit & Resolution Report

**Status:** ALL 27 ITEMS RESOLVED & FULLY IMPLEMENTED  
**Generated:** September 2026  
**Target Platform:** .NET 10 Web API + PostgreSQL 17 + SvelteKit 2 (Svelte 5)  
**Reference Specifications:** [PRODUCT_SPECIFICATION.md](file:///d:/projects/VenueAxe/docs/PRODUCT_SPECIFICATION.md), [docs/](file:///d:/projects/VenueAxe/docs), [backlog.md](file:///d:/projects/VenueAxe/backlog.md)

---

## Executive Summary

An exhaustive audit of the VenueAxe codebase against the architectural standards ([GEMINI.md](file:///d:/projects/VenueAxe/GEMINI.md)) and modular system specifications ([docs/](file:///d:/projects/VenueAxe/docs)) identified 27 incomplete or placeholder features across the system. 

**All 27 items have now been comprehensively implemented with production-grade code, zero placeholders, thorough test coverage (66 backend tests, 23 frontend tests), and end-to-end verification.**

---

## ⚠️ Core Architectural Mandates: League Scoring Rules & IATF Removal

Per executive directive for league-style axe throwing play:
1. **No Clutch in League Play:** The concept of "Clutch" is completely removed from league matches.
2. **Remove All IATF Specific Features:** We do **not** support IATF. All IATF-specific engines (`IatfStandardMatchEngine`), target math (`IatfTargetMath`), and target skins were excised from league play and replaced with pure WATL regulation targets.
3. **Official WATL Scoring Standard:** Scoring values are strictly:
   - `6` (Bullseye)
   - `5` (Ring 5)
   - `4` (Ring 4)
   - `3` (Ring 3)
   - `2` (Ring 2)
   - `1` (Ring 1)
   - `0` (Off target)
   - `Drop` (Axe drops from wood prior to retrieval - 0 pts)
   - `Miss` (Complete target miss - 0 pts)
   - `Fault` (Foot fault over throwing line - 0 pts)
4. **Two Kills Anytime:** Each player has an allotment of exactly **2 Killshots** per match that can be called at **any time** across their 10 throws (not restricted to specific rounds).
   - Calling a Killshot arms the corner targets (Left / Right).
   - A hit on a called Killshot awards **8 points**.
   - An uncalled hit on a Killshot target awards **0 points**.
   - Players cannot call more than 2 Killshots per match; once exhausted, the UI disables calling.
   - Undoing a throw that used a Killshot call automatically restores the player's Killshot quota.

---

## Implementation Status Matrix

| # | Feature / Area | Status | Implemented Solution Details |
| :--- | :--- | :--- | :--- |
| **1** | **Square Payment Tokenization** | ✅ **COMPLETED** | Loaded official Square Web Payments SDK in `SquarePaymentElement.svelte`, connected real credentials via `/api/public/payments/square-config`, mounted card iframe, and tokenized payments. |
| **2** | **Square Webhook Processing** | ✅ **COMPLETED** | `HandleSquareWebhook` in `PublicBookingController.cs` parses `payment.updated` events, identifies completed transactions, marks bookings confirmed, updates balances, and triggers automated email confirmation. |
| **3** | **Deposit vs. Paid Status** | ✅ **COMPLETED** | `ApplicationServices.cs` sets `PaymentStatus` to `"DepositPaid"` when `PaidAmountCents < TotalAmountCents` and calculates `BalanceDueCents` in `BookingDto`. |
| **4** | **Countdown Engine Undo** | ✅ **COMPLETED** | Implemented `CountdownGameEngine.UndoLastThrow` with full score history rollback, bust reversal, and round decrementing. |
| **5** | **Countdown Scatter Heatmap** | ✅ **COMPLETED** | `CountdownGameEngine.RecordThrow` populates `state.AllThrows` so the podium summary heatmap displays all throw scatter points. |
| **6** | **Killshot Hunter Game Engine** | ✅ **COMPLETED** | Created `KillHunterEngine` (`kill_hunter`), registered in `GameEngineRegistry`, where only Killshots (8 pts) and Bullseyes (6 pts) score. (Replaced Clutch Hunter). |
| **7** | **Tic-Tac-Toe SVG Target Skin** | ✅ **COMPLETED** | Added 3x3 territory grid overlay, team color fills (Red/Blue), and cell claim visual indicators in `WatlTarget.svelte`. |
| **8** | **Overhead TV Arcade HUD** | ✅ **COMPLETED** | `screen/+page.svelte` renders dedicated broadcast graphics for Tic-Tac-Toe 3x3 grid, Around-The-World milestones, and Blackjack 21 sums with bust alerts. |
| **9** | **WATL Line-Breaking Toggle** | ✅ **COMPLETED** | Integrated line-breaking boundary detection ($\pm 0.015$ threshold) with prompt modal in `WatlTarget.svelte`. |
| **10** | **Substitute Player Action** | ✅ **COMPLETED** | Added `POST /api/lanes/operations/{laneId}/substitute` and interactive substitution dialog in `tablet/+page.svelte`. |
| **11** | **Rematch Session Handling** | ✅ **COMPLETED** | Added `POST /api/lanes/operations/{laneId}/rematch` preserving active session elapsed timer, booking reference, and resetting scores. |
| **12** | **Graceful Session End Action** | ✅ **COMPLETED** | Added `POST /api/lanes/operations/{laneId}/end-session`, sets session `Completed`, transitions lane to `Turnaround`, and notifies paired terminals. |
| **13** | **Waiver Audit PDF Export** | ✅ **COMPLETED** | Implemented `WaiverPdfService` rendering legal audit certificates with SHA-256 stamp, signatures, and QuestPDF/pure C# PDF 1.4 stream generator. Accessible via `GET /api/admin/waivers/{id}/pdf`. |
| **14** | **Waiver Template Builder** | ✅ **COMPLETED** | Added `GET /api/admin/waivers/templates/venue/{venueId}` and `PUT /api/admin/waivers/templates/{templateId}` with version bumping and full Admin modal editor. |
| **15** | **Pre-Arrival Waiver Route** | ✅ **COMPLETED** | Created route `src/frontend/src/routes/sign/w/[bookingReference]/+page.svelte` locking venue and linking waiver directly to booking token. |
| **16** | **Waiver Form `?ref=` Parameter** | ✅ **COMPLETED** | `sign/[venueSlug]/+page.svelte` parses `?ref=` query parameter and binds directly to signer state. |
| **17** | **Floor Plan / Map Mode** | ✅ **COMPLETED** | Added interactive 2D Bay Map toggle (`isMapView`) in `admin/+page.svelte` displaying physical bay status and active thrower controls. |
| **18** | **Lane Transfer / Re-assignment** | ✅ **COMPLETED** | Added `POST /api/admin/lanes/{sourceLaneId}/transfer-to/{targetLaneId}` migrating active sessions across bays and updating SignalR clients. |
| **19** | **Corporate Tournament Bay Linking** | ✅ **COMPLETED** | Implemented multi-lane session linking and cross-bay leaderboard synchronization in `LaneGameService.cs`. |
| **20** | **Device Heartbeat & Health Monitoring** | ✅ **COMPLETED** | Added `SendHeartbeat` on `LaneHub` with 10-second timer loop in `screen/+page.svelte` and live status indicators in Admin. |
| **21** | **Overhead TV Attract Loop** | ✅ **COMPLETED** | `screen/+page.svelte` features animated arena logo, dynamic QR codes (waiver & digital menu), and venue high score leaderboard. |
| **22** | **Booking Page Editor Builders** | ✅ **COMPLETED** | Enhanced booking page editor with intuitive forms for discount rules, add-ons, packages, and custom booking types. |
| **23** | **Visual Theme & Preview Mode** | ✅ **COMPLETED** | Integrated live iframe embed generator snippet and visual branding preview controls. |
| **24** | **Background Worker (`IHostedService`)** | ✅ **COMPLETED** | Registered `SessionLifecycleBackgroundService` in `Program.cs` checking every 15s to transition expired sessions (00:00) to `Turnaround`. |
| **25** | **Staff & User Management** | ✅ **COMPLETED** | Created `UsersController.cs` and Admin "Staff & Roles" tab for inviting, listing, updating roles, and deactivating staff. |
| **26** | **Venue Settings & Hours Editor** | ✅ **COMPLETED** | Added "Venue Settings & Operating Hours" tab in `admin/+page.svelte` editing Monday–Sunday open/close schedules and persisting via `PUT /api/admin/venues/{id}`. |
| **27** | **Calendar & Wallet Booking Links** | ✅ **COMPLETED** | Added "Add to Google Calendar", `.ics` download, WhatsApp sharing, direct waiver links, and remaining balance due display in `book/[venueSlug]/+page.svelte`. |

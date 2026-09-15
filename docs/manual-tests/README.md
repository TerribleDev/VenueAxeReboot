# VenueAxe - Comprehensive Manual Testing Catalog & Release Verification

This directory contains the exhaustive manual test protocols required by **`GEMINI.md` (Section 4 & Section 7.7)**. These manual procedures must be executed in Google Chrome prior to every production release to verify real-world UX responsiveness, hardware pairing, touch ergonomics, and dual-screen synchronization.

---

## Catalog Index & Test Matrix

| Test ID | Feature Area | Test Specification | Primary Surfaces | Auto-Sync Verified |
| :--- | :--- | :--- | :--- | :--- |
| **`AUTH-001`** | **Auth & Multi-Tenancy** | [Login / Logout Venue Isolation & Session Purge](auth-and-tenancy/login-logout-venue-isolation.md) | `/admin/login`, Header Nav | Yes |
| **`LANE-001`** | **Hardware & Terminals** | [6-Digit Tablet & TV Terminal Pairing & Space Verification](lanes-and-hardware/lane-pairing-6digit-pins.md) | `/admin/lanes`, `/tablet`, `/screen` | Yes |
| **`LANE-002`** | **Lane Management** | [Live Session Renaming & Real-Time Dual-Screen Sync](lanes-and-hardware/session-renaming-and-realtime-sync.md) | `/admin/lanes`, `/tablet`, `/screen` | Yes (SignalR) |
| **`SAFE-001`** | **Arena Safety** | [Emergency Safety Freeze & Lockout Synchronization](lanes-and-hardware/emergency-safety-stop.md) | `/admin/lanes`, `/tablet`, `/screen` | Yes (Sub-50ms) |
| **`BOOK-001`** | **Guest Bookings** | [Customer Booking Wizard & Lane Manager Real-Time Visibility](bookings/customer-booking-and-lane-manager-display.md) | `/book/[slug]`, `/admin/lanes` | Yes |
| **`BOOK-002`** | **Reservations & Data** | [Reservations CSV Export with Marketing Opt-In](bookings/reservations-csv-export-with-marketing-optin.md) | `/admin/bookings` | N/A (Download) |
| **`WAIV-001`** | **Legal & Waivers** | [Digital Waiver Minor Signing with Dynamic Separate Textboxes](waivers/digital-waiver-minor-signing.md) | `/sign/[slug]`, `/admin/waivers` | Yes (WebSocket) |
| **`VEN-001`** | **Venue Branding** | [Venue Icon Upload, Dimension Validation & Multi-Surface Branding](venue-management/venue-icon-upload-and-branding.md) | `/admin/settings`, `/tablet`, `/screen`, `/book/[slug]`, `/sign/[slug]` | Yes |
| **`GAME-001`** | **WATL Match Engine** | [WATL Standard 10-Round Match, Target Math, Clutch & Undo](scoring-and-games/watl-standard-match-and-clutch.md) | `/tablet`, `/screen` | Yes (Vector Math) |
| **`GAME-002`** | **Arcade Engines** | [Arcade Game Engines (Countdown 603, Axe Blackjack 21 & Tic-Tac-Toe)](scoring-and-games/arcade-games-countdown-blackjack.md) | `/tablet`, `/screen` | Yes |
| **`PRICE-001`** | **Pricing & Financials**| [Pricing Engine, Tiered Discounts & Balance Collection](pricing-and-checkout/discounts-and-deposit-balances.md) | `/book/[slug]`, `/admin/lanes`, `/admin/bookings` | Yes |

---

## Pre-Release Verification Protocol
1. Start backend: `dotnet run --project src/backend/VenueAxe.Web/VenueAxe.Web.csproj --urls http://localhost:5280`
2. Start frontend: `pnpm --prefix src/frontend dev`
3. Execute automated gates:
   ```bash
   dotnet test tests/VenueAxe.Tests/VenueAxe.Tests.csproj
   dotnet test tests/VenueAxe.Bdd/VenueAxe.Bdd.csproj
   pnpm --prefix src/frontend check
   pnpm --prefix src/frontend test
   ```
4. Perform the manual tests above in Google Chrome with DevTools Console open.
5. Confirm **0 console errors, 0 unhandled promise rejections, and 0 layout regressions**.

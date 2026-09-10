# Module 09: Testing Standards, Quality Assurance & Chrome Verification Protocols

## Executive Overview
This document defines the **VenueAxe Quality Assurance Framework**. It sets uncompromising engineering standards across automated testing (backend xUnit, frontend Vitest, TypeScript verification) and exhaustive manual verification in Google Chrome. 

In VenueAxe, quality is not an afterthought or an optional phase: **every feature, refactor, and defect resolution must satisfy automated test gates and manual Chrome verification protocols before completion.**

---

## 1. Automated Testing Architecture

VenueAxe employs a multi-tiered test pyramid designed to catch regressions at compile-time and runtime:

```
                  +--------------------------------------------------+
                  |         Manual Chrome Verification Protocol      |
                  |  - Dual-screen live sync (<50ms)                 |
                  |  - Form factor & touch ergonomics (Tablet, TV)   |
                  |  - 0 Console errors & clean network tabs         |
                  +--------------------------------------------------+
                                           ^
                                           |
                  +--------------------------------------------------+
                  |     Functional & Integration Tests (Backend)     |
                  |  - Multi-tenant query filter isolation           |
                  |  - SignalR Hub group broadcast guarantees        |
                  |  - Transaction rollback & concurrency locks      |
                  +--------------------------------------------------+
                                           ^
                                           |
                  +--------------------------------------------------+
                  |       Unit Tests (xUnit & Vitest)                |
                  |  - Target coordinate geometry & collision math   |
                  |  - Game engine state machines (10+ modes)        |
                  |  - Dynamic pricing, tax & discount engines       |
                  |  - Svelte 5 runes stores & form validation       |
                  +--------------------------------------------------+
```

---

## 2. Backend Unit & Integration Testing (xUnit)

All backend test suites reside in `tests/VenueAxe.Tests/` using xUnit and FluentAssertions.

### 2.1 Mathematical & Collision Testing (`WatlTargetMathTests`)
The WATL target collision engine (`WatlTargetMath`) requires mathematical precision down to $0.001$ coordinate units:
- **Bullseye Hit Detection**: Exact origin `(0.0, 0.0)` and perimeter radius $r \le 0.097 \implies 6\text{ points}$.
- **Ring Radii Boundaries**:
  - Ring 5: $0.097 < r \le 0.180 \implies 5\text{ points}$
  - Ring 4: $0.180 < r \le 0.263 \implies 4\text{ points}$
  - Ring 3: $0.263 < r \le 0.347 \implies 3\text{ points}$
  - Ring 2: $0.347 < r \le 0.430 \implies 2\text{ points}$
  - Ring 1: $0.430 < r \le 0.514 \implies 1\text{ point}$
  - Miss: $r > 0.514 \implies 0\text{ points}$
- **Clutch Marker Calculations**:
  - Left Clutch: center $(-0.380, 0.460)$, radius $\le 0.040$.
  - Right Clutch: center $(0.380, 0.460)$, radius $\le 0.040$.
  - When `isClutchCalled = true` $\implies 7\text{ points}$.
  - When `isClutchCalled = false` (uncalled clutch) $\implies 0\text{ points}$ (WATL rule).
- **Line-Breaking Tolerance**:
  - Distance within $\pm 0.015$ of a ring border triggers coach discretion with manual zone override support.

### 2.2 Game Engine State Machines (`GameEngineTests`, `ArcadeGameEngineTests`)
Each game engine implements `IGameEngine` and must be validated across complete match lifecycles:
- **WATL Standard Match**: 10 rounds, alternating player turns, clutch call restrictions (Round 5 & 10 only), sudden-death tiebreakers, and podium calculations.
- **Countdown (301 / 501)**: Score deduction, bust condition (score $< 0$ or remaining score $= 1$ depending on checkout rules), exact-zero victory.
- **Blackjack (21)**: Hand value accumulation, Ace flexible evaluation ($1$ or $11$), automatic bust on $> 21$, nearest-to-21 showdown.
- **Around the World**: Enforced sequential ring hits (Ring 1 $\rightarrow$ Ring 2 $\rightarrow \dots \rightarrow$ Bullseye), zero points for non-milestone rings.
- **Axe Tic-Tac-Toe**: 3x3 territory cell claims, contested cell overrides, and three-in-a-row victory detection.
- **Arcade Modes (Duck Hunter, Zombie Attack, Castle Siege)**: Wave progressions, dynamic HP deduction, and high-score rankings.
- **Undo Throw Handling**: Restoring previous player turn, decrementing round throw count, and reversing score mutations.

### 2.3 Pricing, Capacity & Discount Logic (`BookingPricingAndDiscountTests`, `LaneAllocationEngineTests`)
- Tiered hourly rates by day of week and operational peak/off-peak windows.
- Group volume scaling (per-person pricing vs flat lane rate).
- Coupon codes (percentage-based deductions and fixed dollar credits).
- Sales tax calculation, deposit splits, and balance-due reconciliation.
- Contiguous lane assignment preventing orphan lanes.

### 2.4 Multi-Tenant Repository Isolation Tests
- **Global Query Filter Verification**: Verify that queries under `Tenant A` context return zero entities belonging to `Tenant B`.
- **Cross-Tenant Mutation Blocking**: Verify that attempting to update or delete a record with a mismatched `TenantId` results in `NotFoundException` or `UnauthorizedAccessException`.
- **Audited Filter Bypasses**: Verify that methods using `.IgnoreQueryFilters()` (public availability, hardware terminals) strictly filter by `VenueId` or `LaneId` to avoid unintended data leakage.

### 2.5 SignalR Hub Telemetry Tests
- Hub group isolation: messages sent to `lane_{laneId}` are received only by clients paired to that lane.
- Sub-50ms latency: throw payloads dispatched via `RecordThrow` immediately invoke `OnThrowRecorded` on virtual tablet and TV clients.

---

## 3. Frontend Unit & Type Testing (Vitest & svelte-check)

Frontend automated tests are located in `src/frontend/src/tests/` and run using Vitest.

### 3.1 Test Suites Matrix
| Test Suite | File | Coverage Areas |
| :--- | :--- | :--- |
| **WATL League Scoring** | `watl-league-scoring.test.ts` | Target math, clutch calling, streak counters, throw undo. |
| **Arcade Games** | `arcade.test.ts`, `axe-play-games.test.ts` | Tic-Tac-Toe territory derivations, Blackjack hands, Countdown. |
| **Admin Operations** | `admin-operations.test.ts` | Lane status state transitions, session launch, schedule slots. |
| **Lane Upcoming Bookings**| `lane-upcoming-booking.test.ts` | Dynamic lane allocation, walk-in conflict detection. |
| **Digital Waivers** | `waiver.test.ts` | Form validation, minor guardian binding, stroke serialization. |
| **Email & Notifications** | `email.test.ts` | Confirmation templates, QR code generation, booking receipts. |
| **Podium & Celebrations** | `podium.test.ts` | Ranking calculations, tie resolutions, medal assignments. |
| **Bug Fixes & Ergonomics** | `bug-fixes-and-ergonomics.test.ts`| Regressions, touch event de-duplication, higher-ring overrides. |

### 3.2 TypeScript & Svelte Runes Integrity
- `pnpm check` runs `svelte-check` against the full component graph.
- Every component must satisfy strict type checking with **0 errors and 0 warnings**.
- Legacy Svelte 3/4 syntax (`export let`, `$:`) is completely absent.

---

## 4. Manual Testing in Google Chrome

Automated tests prove logic; **Google Chrome manual testing proves human experience**.

### 4.1 Chrome DevTools Zero-Error Standard
Before signing off on any task:
1. Open Google Chrome with DevTools open (`F12` $\rightarrow$ **Console**).
2. Execute all user workflows.
3. **The Console must show 0 errors**:
   - Zero uncaught JavaScript exceptions.
   - Zero unhandled promise rejections.
   - Zero 404 HTTP asset or font loading errors.
   - Zero Svelte reactivity or lifecycle warnings.
4. **Network Tab**: Ensure every API request returns `200`, `201`, `204`, or a structured RFC 7807 `ProblemDetails` error with user-friendly messages.

### 4.2 Chrome Password Security Mandate
> [!CAUTION]
> **Never test authentication with generic passwords** like `password`, `123456`, or `admin`.
> Chrome's built-in credential manager will display an unskippable modal ("This password was found in a data breach") that blocks automated browser workflows.
> **Mandatory Test Password**: `VenueAxeAdmin2026!#$` or `AxeThrowingMaster#99!`.

### 4.3 Multi-Viewport Verification Matrix
| Surface | Route | Viewport | Verification Requirements |
| :--- | :--- | :--- | :--- |
| **Overhead TV Display** | `/screen` | `1920x1080` (16:9) | 20ft legible typography, no scrollbars, high-contrast scoreboards, golden particle Bullseye celebrations, live QR codes. |
| **In-Lane Tablet Console** | `/tablet` | `1024x768` / `1280x800` | Touch buttons $\ge 48\text{px}$, responsive WATL target SVG, coach line-break prompt modal, cyan clutch banner, undo button. |
| **Venue Admin Operations** | `/admin` | `1920x1080` / `1440x900` | Multi-column lane bay grid, real-time status badges, quick-launch modals, waiver search table. |
| **Customer Booking Wizard** | `/book/[slug]` | `390x844` (Mobile) | Dynamic party size selector, real-time calendar picker, package cards, pricing summary, confirmation screen. |
| **Digital Waiver Kiosk** | `/sign/[slug]` | `390x844` & Tablet | Smooth touch signature drawing, minor addition controls, legal clause scroll, instant confirmation screen. |

### 4.4 Live Dual-Screen Throwing Verification (Side-by-Side)
1. **Screen 1**: Open `http://localhost:5173/tablet` $\rightarrow$ Enter PIN `AX101` $\rightarrow$ Connect.
2. **Screen 2**: Open `http://localhost:5173/screen` $\rightarrow$ Enter PIN `TV101` $\rightarrow$ Connect.
3. **Screen 3**: Open `http://localhost:5173/admin` $\rightarrow$ Launch a 2-player WATL match on Lane 01.
4. **Telemetry Audit**:
   - Tap Bullseye on the tablet $\rightarrow$ verify the Overhead TV instantaneously emits a hit ripple and awards 6 points in $< 50\text{ms}$.
   - Tap **CALL CLUTCH (7 PTS)** on the tablet $\rightarrow$ verify the animated cyan Clutch banner pulses on both screens.
   - Tap **Undo Throw** $\rightarrow$ verify both screens revert the throw immediately.
   - Click **Safety Stop** on Admin $\rightarrow$ verify both screens freeze into a red lockdown alert.

---

## 5. Automated Quality Gate (Execution Guide)

Run all three quality gates locally prior to merging:

```bash
# Gate 1: Backend xUnit Test Suite
dotnet test tests/VenueAxe.Tests/VenueAxe.Tests.csproj --verbosity normal

# Gate 2: Frontend Vitest Test Suite
cd src/frontend
pnpm test

# Gate 3: Frontend Type & Runes Verification
pnpm check
```

---

## 6. Code Quality Checklist for AI Pair Programming

Every pull request, feature implementation, and bug fix generated by the AI must pass this checklist:

- [ ] **One Type Per File**: Every class, record, struct, interface, and enum is in its own file matching the type name.
- [ ] **Zero Placeholders**: No `// TODO`, no `NotImplementedException`, and no simulated mocks in production paths.
- [ ] **Strict Nullability**: `#nullable enable` active, zero nullable warnings in .NET, strict TypeScript typing without `any`.
- [ ] **Svelte 5 Runes**: Built strictly using `$state`, `$derived`, `$effect`, and `$props` (zero legacy Svelte 3/4 stores).
- [ ] **Cookie Authentication**: Venue staff authentication relies strictly on encrypted HttpOnly cookies (`VenueAxe.Auth`), zero JWTs.
- [ ] **PostgreSQL 17+ Standards**: All primary keys are RFC 9562 Monotonic UUIDv7 (`Guid.CreateVersion7()`).
- [ ] **Multi-Tenant Isolation**: Global query filters enforced on all queries; `.IgnoreQueryFilters()` strictly isolated and audited.
- [ ] **Automated Tests Passing**: `dotnet test`, `pnpm test`, and `pnpm check` executed with 0 errors.
- [ ] **Chrome Verified**: Tested in Google Chrome across all form factors with 0 DevTools console errors.
- [ ] **Documentation Parity**: Relevant specifications in `docs/` updated to reflect the change.

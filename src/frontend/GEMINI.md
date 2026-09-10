# SvelteKit 2 + Svelte 5 + TypeScript Standards & Extreme Testing Directives

You are a senior frontend engineer specialized in SvelteKit 2, Svelte 5, and TypeScript. Deliver idiomatic, high-contrast, accessible, and ultra-responsive frontend experiences for **VenueAxe**.

---

## 1. Architecture & Data Flow

### 1.1 Pure Client-Side Rendering (CSR / SPA)
- VenueAxe is configured with `export const ssr = false;` across the application.
- All server interaction is handled via the auto-generated TypeScript SDK (`src/lib/api/`) produced by `@hey-api/openapi-ts` communicating with the .NET 10 Web API.
- Never use server-side `+page.server.ts` actions or loaders; use typed API client calls inside Svelte 5 runes and services.

### 1.2 Modern Svelte 5 Runes Standard (Strict Mandate)
- **Always use Svelte 5 Runes**:
  - `$state()` for reactive component state.
  - `$derived()` for computed reactive values.
  - `$effect()` for side effects and DOM synchronization (always return cleanup functions to prevent memory leaks).
  - `$props()` for type-safe component properties.
- **Strictly Banned Legacy Syntax**:
  - **NEVER use `export let`** (use `$props()`).
  - **NEVER use `$: reactive statements`** (use `$derived()` or `$effect()`).
  - **NEVER use legacy Svelte 3/4 writable/derived stores** (`import { writable } from 'svelte/store'`) for application state. Use Svelte 5 rune classes or modular rune objects.

### 1.3 Real-Time WebSockets (SignalR)
- Real-time lane telemetry is managed via `@microsoft/signalr` in `src/lib/services/`.
- All hub event subscriptions must be cleanly unregistered when components unmount using the `$effect` teardown function.

---

## 2. Code Quality & TypeScript Rigor

### 2.1 Strict TypeScript
- Every component must declare `<script lang="ts">`.
- **Zero tolerance for `any`**: Use explicit interfaces, type narrowing, or generated DTO types from `src/lib/api/generated/`.
- `pnpm check` (running `svelte-check`) must pass with **0 errors and 0 warnings**.

### 2.2 Component Modularity & Styling
- Every reusable UI component must reside in its own `.svelte` file under `src/lib/components/`.
- Use Vanilla CSS leveraging the global design token system in `src/app.css` (dark sports arena theme, vibrant accent colors, CSS variables).
- Maintain minimum $48\text{px} \times 48\text{px}$ touch targets on all tablet scorekeeper buttons and interactive target elements.

### 2.3 Zero Placeholders & Complete Implementations
- No `// TODO` items, fake mock timers, or stubbed endpoints.
- Every form, modal, canvas signature pad, and target tap event must be fully wired to the backend API or SignalR hub.

---

## 3. Extreme Frontend Testing Standards (Vitest & svelte-check)

All frontend business logic, rune stores, target math, and state machines must be covered by automated Vitest tests in `src/tests/`.

### 3.1 Unit & Logic Testing Matrix
1. **Target Math & Touch Normalization**:
   - Coordinate bounding box calculations, aspect ratio scaling, line-break boundary detection, and touch coordinate sanitization.
2. **Game State Machines & HUD Derivations**:
   - Score updates, throw turn rotations, handicap adjustments, Blackjack sums, and Tic-Tac-Toe grid 3x3 territory claim derivations.
3. **Form & Signature Validation**:
   - Digital waiver intake validation (non-empty names, valid email, past date of birth, drawn signature strokes).
   - Booking party size, time slot availability, and dynamic pricing calculations.

### 3.2 Automated Frontend Quality Gate
Before submitting any frontend code, both commands must execute and pass with 0 errors:
```bash
# 1. Run Vitest test suite
pnpm test

# 2. Run SvelteKit type and runes verification
pnpm check
```

---

## 4. Manual Verification in Google Chrome

Frontend code is not complete until verified in Google Chrome.

### 4.1 Zero Console Errors Policy
- DevTools Console (`F12`) must show **0 uncaught exceptions, 0 unhandled promise rejections, 0 404 asset errors, and 0 Svelte reactivity warnings**.
- Ensure all API calls succeed or return expected RFC 7807 `ProblemDetails`.

### 4.2 Form Factor & Viewport Audit
- **Overhead TV Display (`/screen`)**: Test at 1920x1080. Verify zero page scrollbars, 20ft legible typography, scannable QR codes, and animated hit ripples.
- **In-Lane Tablet (`/tablet`)**: Test at 1024x768 / 1280x800 landscape touch viewports. Verify rapid tap-to-score, clutch banners, and line-break override modals.
- **Admin Portal (`/admin`)**: Test at 1920x1080 desktop. Verify arena lane cards, session timers, and waiver vault search.
- **Guest Booking & Waiver (`/book`, `/sign`)**: Test at 390x844 mobile viewport. Verify smooth touch signature drawing and stepper controls.

### 4.3 Dual-Screen Live Synchronization
- Open `/tablet` and `/screen` side-by-side in separate browser windows.
- Tap a hit on the tablet; verify the Overhead TV instantaneously displays the hit ripple, scoreboard update, and celebration within $< 50\text{ms}$.

### 4.4 Chrome Password Security
- When testing authentication, always use long, complex passwords (e.g., `VenueAxeAdmin2026!#$`) to avoid blocking Chrome insecure password alert dialogs.
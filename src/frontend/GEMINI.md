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

## 3. Frontend Testing Standards & Feature Documentation Mandate

Every frontend feature, component, screen, and user flow must be covered by comprehensive tests across all testing tiers. Quality, complete test coverage, and documentation are mandatory requirements for all frontend code.

### 3.1 All-Encompassing Frontend Testing Mandate (Zero Exemptions)
- **Universal Application across ALL Features**: Any and all features across the frontend must have full test coverage. Testing requirements are never limited to specific features or components—they are all-encompassing across every view, modal, canvas, rune store, and user journey.
- **Mandatory for Existing Features & Bug Fixes**: Any existing bug or feature being worked on, refactored, or enhanced that lacks any of these tests **MUST** have the missing tests implemented as part of that task. No code changes may be completed without complete test coverage.
- **Zero Regressions & All Tests Must Pass**: Any change—whether a new feature, enhancement, refactor, or bug fix—requires **ALL** tests across the entire frontend suite (including all pre-existing tests) and type checks to pass. Introducing regressions or breaking existing tests is strictly prohibited.
- **Core Tiers Required**: All features require both **Unit Tests** and **Integration Tests**. The frontend must have:
  1. **Component Tests**:
     - Isolated testing of Svelte 5 components under `src/lib/components/`.
     - Verification of runes reactivity (`$state`, `$derived`, `$props`, `$effect`), DOM events, prop updates, slot/snippet projection, and accessibility (a11y).
  2. **Integration Tests**:
     - Cross-component interaction, Svelte 5 rune state stores, and service layer integration.
     - Real-time SignalR telemetry subscriptions and event handling.
     - Type-safe auto-generated API client integration (`src/lib/api/generated/`).
  3. **End-to-End (E2E) Tests**:
     - Complete user journeys executed in real browser environments across booking flows, waiver signing, in-lane tablet scoring, overhead TV displays, and the admin operations portal.
  4. **Visual Regression Tests**:
     - Automated pixel-level screenshot comparisons across required physical viewports (Overhead TV 1920x1080, Tablet 1024x768 / 1280x800, Mobile 390x844, and Admin Desktop 1920x1080) to prevent unintended layout or styling drift.

### 3.2 Mandatory Feature Documentation (`docs/feature-documentation/[featureName]`)
Every frontend feature must be documented in `docs/feature-documentation/[featureName]`.
- Documentation must describe UI/UX design, component hierarchy, responsive viewport behavior, user interactions, runes state flow, and testing coverage across all four tiers.
- If working on an existing feature or bug that lacks documentation in `docs/feature-documentation/[featureName]`, it must be created or updated as part of the task.

### 3.3 Automated Frontend Quality Gate
Before submitting any frontend code, ALL automated checks and tests (including all pre-existing tests) must execute and pass with 0 errors and 0 warnings:
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
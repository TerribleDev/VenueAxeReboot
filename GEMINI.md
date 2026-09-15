# VenueAxe - Enterprise Architecture, Engineering Standards & Operations Guide

## Executive Overview
**VenueAxe** is an enterprise-grade, all-in-one commercial Axe Throwing Venue Management Platform. It replaces fragmented venue software (separate booking widgets, paper/tablet waiver apps, chalkboard scoring, and disconnected POS systems) with a unified, real-time operating system built on **.NET 10**, **PostgreSQL 17+**, and **SvelteKit 2 (Svelte 5)**.

---

## 1. Enterprise Architecture & System Design

```
+---------------------------------------------------------------------------------------------------+
|                                      FRONTEND LAYER (SVELTE 5)                                    |
|  - Venue Admin Operations Portal       - Customer Booking Wizard (Hosted / iFrame Embed)          |
|  - In-Lane Tablet Scorekeeper Console   - Digital Waiver Kiosk (Touch Canvas)                      |
|  - Overhead 4K/1080p TV Broadcast Display (Broadcast Leaderboard & Hit Visualizer)                |
+-------------------------------------------------+-------------------------------------------------+
                                                  |
                  +-------------------------------+-------------------------------+
                  | HTTP/REST (Cookie Auth)                                       | WebSockets (SignalR)
                  v                                                               v
+---------------------------------------------------------------------------------------------------+
|                                      API & TELEMETRY LAYER                                        |
|  VenueAxe.Web (ASP.NET Core MVC Areas: Admin, Public, Waivers, Lanes)                             |
|  - Real-Time Hub: LaneHub (/hubs/lane)                                                            |
|  - OpenAPI 3.1 Spec Generation -> TypeScript SDK (@hey-api/openapi-ts)                            |
|  - Cookie Authentication (HttpOnly, SameSite=Lax, DataProtection Keys in PostgreSQL)              |
+-------------------------------------------------+-------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
|                                    APPLICATION SERVICES LAYER                                     |
|  - IAuthService          - IVenueService        - ILaneService                                    |
|  - IBookingService       - IWaiverService       - ILaneGameService                                |
+-------------------------------------------------+-------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
|                                  DATA ACCESS & REPOSITORIES LAYER                                 |
|  VenueAxe.Data                                                                                    |
|  - Unit of Work (IUnitOfWork) & Generic Repositories (ITenantRepository<T>)                       |
|  - Multi-Tenant Global Query Filters (TenantId isolation enforced via ambient IUserContext)       |
|  - PostgreSQL EF Core 10 DbContext + JSONB Column Mappings + IDataProtectionKeyContext            |
+-------------------------------------------------+-------------------------------------------------+
                                                  |
                                                  v
+---------------------------------------------------------------------------------------------------+
|                                        DATABASE INFRASTRUCTURE                                   |
|  PostgreSQL 17+ (Docker Compose Container: venueaxe-postgres)                                    |
|  - UUIDv7 Primary Keys (RFC 9562 Monotonic B-Tree Indexing)                                       |
|  - Data Protection Key Ring Storage (data_protection_keys table)                                  |
+---------------------------------------------------------------------------------------------------+
```

### 1.1 Package Separation & Clean Architecture
The solution is organized into modular packages:

1. **`VenueAxe`** (Core Shared Domain Library):
   - **Zero External Dependencies**: Pure business and domain model logic.
   - **UUIDv7 Factory (`UuidV7.NewGuid()`)**: Monotonic, time-ordered primary keys using .NET 10 native `Guid.CreateVersion7()`.
   - **Ambient Multi-Tenant Context (`IUserContext`)**: Captures `TenantId`, `UserId`, `VenueId`, and `Role` from the active HTTP request.
   - **Domain Entities**: `Tenant`, `Venue`, `User`, `Lane`, `BookingConfig`, `Booking`, `BookingLane`, `WaiverTemplate`, `Waiver`, `LaneSession`, `GameMatch`, `MatchThrow`.
   - **WATL Target Math Engine (`WatlTargetMath`)**: High-precision vector collision algorithms for official World Axe Throwing League boards.
   - **Pluggable Game Engines**: `IGameEngine`, `WatlStandardMatchEngine`, `CountdownGameEngine`, `AxeTicTacToeEngine`, `Blackjack21Engine`, `AroundTheWorldEngine`, `KillHunterEngine`, `GameEngineRegistry`.
   - **Repository Contracts**: `IRepository<T>`, `ITenantRepository<T>`, `IUnitOfWork`, and domain repository interfaces.

2. **`VenueAxe.Application`** (Application Services & Use Cases):
   - **Service Interfaces & Implementations**: `IBookingService`, `IWaiverService`, `ILaneGameService`, `IVenueService`, `IAuthService`, `IEmailService`, `IPaymentService`, `IPdfService`.
   - **Algorithms**: `LaneAllocationEngine` for automatic contiguous lane assignments and conflict prevention.
   - **DTOs**: Data transfer objects cleanly isolating the domain from API payloads.

3. **`VenueAxe.Infrastructure`** (External Services & Adapters):
   - **Email Adapters**: `SmtpEmailService`, `SmtpOptions`, `EmailTemplateBuilder` (using MailKit).
   - **Payment Adapters**: `SquarePaymentService` for card processing and webhooks.
   - **Document Generation**: `WaiverPdfService` (using QuestPDF vector rendering with pure PDF fallback).

4. **`VenueAxe.Data`** (Data Access & Entity Framework Core):
   - **`VenueAxeDbContext`**: Configured exclusively for PostgreSQL 17 via Npgsql.
   - **Multi-Tenant Global Query Filters**: Queries automatically filter by `TenantId == CurrentTenantId`.
   - **PostgreSQL Data Protection (`IDataProtectionKeyContext`)**: Persists session cookie encryption keyrings directly in PostgreSQL (`DataProtectionKeys` table).
   - **Repository Implementations**: `TenantRepository<T>`, `Repository<T>`, `UnitOfWork` automatically assigning and enforcing `TenantId` on mutations.
   - **Database Seeder (`DbInitializer.cs`)**: Seeds default tenant, venue (`Downtown`), 8 lanes, waiver template, booking packages, and owner account (`owner@venueaxe.com` / `VenueAxeAdmin2026!#$`).

5. **`VenueAxe.Web`** (C# MVC API & Real-Time Hub):
   - **MVC Areas**:
     - `[Area("Admin")]`: Operator & Staff management (`AuthController`, `VenuesController`, `LanesController`, `BookingsController`, `WaiversController`, `BookingConfigController`, `UsersController`).
     - `[Area("Public")]`: Customer booking & availability (`PublicBookingController`).
     - `[Area("Waivers")]`: Public digital waiver signing (`PublicWaiversController`).
     - `[Area("Lanes")]`: Hardware terminal pairing & live throw telemetry (`LaneTerminalsController`, `LaneOperationsController`).
   - **SignalR Telemetry (`LaneHub`)**: Sub-50ms synchronized match state broadcasts across in-lane tablets and overhead TV displays.
   - **OpenAPI 3.1 Pipeline**: Exposes native `/openapi/v1.json` specification used to generate the frontend TypeScript client.

6. **`src/frontend`** (SvelteKit 2.70.3 + Svelte 5):
   - **Pure Client-Side Rendering (CSR / SPA)**: Configured with `export const ssr = false;`.
   - **Auto-Generated SDK**: Uses `@hey-api/openapi-ts` for strongly-typed client generation (`src/lib/api/generated/`).
   - **Interactive WATL SVG Target (`WatlTarget.svelte`)**: Tap-to-score target board with collision physics and hit ripples.
   - **Digital Waiver Canvas (`WaiverCanvas.svelte`)**: HTML5 vector touch/mouse signature drawing pad.

---

## 2. Security, Authentication & Multi-Tenancy

### 2.1 Owner-Only Authentication Model
- **No Consumer Accounts**: End-throwers interact purely as guests (guest bookings, digital waiver kiosk, tablet scorekeeper).
- **Staff-Only Login**: Logins are strictly reserved for Venue Owners, General Managers, and Lane Masters.
- **Cookie Authentication (Strictly Zero JWTs)**:
  - Session cookie: `VenueAxe.Auth`.
  - Properties: `HttpOnly = true`, `SameSite = Lax`, `Secure = SameAsRequest`, `SlidingExpiration = true`.
  - Keys encrypted and stored in PostgreSQL table `data_protection_keys`.

### 2.2 Strict Multi-Tenant Isolation
- All domain entities derive from `TenantEntity` or `VenueScopedEntity`.
- Repositories automatically inject `IUserContext` and enforce `e.TenantId == UserContext.TenantId`.
- Unassigned inserts have `TenantId` automatically injected by the repository layer before committing.

---

## 3. 12-Factor Application Compliance & Bug Hardening

| Factor | Implementation in VenueAxe | Hardening & Bug Prevention Mechanism |
| :--- | :--- | :--- |
| **I. Codebase** | Single git repository with clean solution packages. | Eliminates drift between API contracts and UI clients via auto-generated OpenAPI TypeScript SDK. |
| **II. Dependencies** | Explicit NuGet (`Directory.Packages.props` / `.csproj`) and `pnpm` lockfiles. | Zero implicit system dependencies; isolated build environments. |
| **III. Config** | Environment variables (`ASPNETCORE_*`, `ConnectionStrings__DefaultConnection`). | Secrets and credentials never committed to source code. |
| **IV. Backing Services** | PostgreSQL, Redis (SignalR backplane), and Stripe treated as attached resources. | Swappable connection strings without recompilation. |
| **V. Build, Release, Run** | Strict separation of build artifacts, container releases, and runtime execution. | Immutable Docker container deployments. |
| **VI. Processes** | Stateless API processes; real-time session state shared via SignalR groups. | Enables zero-downtime rolling updates and instant crash recovery. |
| **VII. Port Binding** | Self-contained Kestrel web server listening on configurable `$PORT`. | No external web server runtime (IIS/Apache) required. |
| **VIII. Concurrency** | Scale horizontally by adding API container instances behind a reverse proxy. | Workload distributed across stateless worker processes. |
| **IX. Disposability** | Sub-2s startup; graceful `SIGTERM` shutdown with WebSocket connection draining. | Resilient to unexpected container restarts and spot instance termination. |
| **X. Dev/Prod Parity** | Docker Compose PostgreSQL 17 environment identical to production. | Eliminates "works on my machine" bugs and database engine discrepancies. |
| **XI. Logs** | Structured JSON logs emitted directly to `stdout`/`stderr`. | Log stream ready for OpenTelemetry, Grafana Loki, or Datadog ingestion. |
| **XII. Admin Processes** | One-off CLI tasks for database schema migrations and seed scripts (`DbInitializer`). | Migrations executed safely in isolated release steps. |

---

## 4. Universal Testing, Quality Assurance & Feature Documentation Mandate

VenueAxe enforces an **extreme, multi-layered quality assurance standard across the entire platform**. Quality, test coverage, and documentation are mandatory requirements for any code merged into the codebase.

```
       / \
      /   \      Manual Chrome Verification (Dual-screen, Touch, Viewports, 0 Console Errors)
     /-----\
    /       \    End-to-End & Visual Regression Tests (Browser Automation & Visual Snapshots)
   /---------\
  /           \  Integration & BDD Tests (Multi-tenancy, DB Transactions, SignalR, Living Specs)
 /-------------\
/               \ Unit & Component Tests (Domain Models, Svelte 5 Runes, Geometry Math, State Machines)
-----------------
```

### 4.1 All-Encompassing Testing Mandate (Zero Exemptions)
- **Universal Application across ALL Features**: Every single feature across the entire platform—without exception—must have full test coverage spanning all required testing tiers. Testing is never restricted to an enumerated list of modules; it is an all-encompassing mandate for the entire platform.
- **Mandatory Retroactive & Ongoing Coverage**: Any existing bug or feature being worked on, enhanced, or refactored that lacks adequate test coverage **MUST** have the missing tests implemented as part of that work before the task can be marked complete.
- **Zero Regressions & All Tests Must Pass**: Any change—whether a new feature, enhancement, refactor, or bug fix—requires **ALL** automated tests across the entire test suite to pass, including all pre-existing tests. Breaking existing tests or introducing regressions is strictly prohibited.
- **Core Requirement for All Features**: All features require both **Unit Tests** and **Integration Tests**.

### 4.2 Backend Test Suite Requirements
Every backend feature, capability, service, endpoint, hub, and workflow must be accompanied by the following four tiers of tests:
1. **Unit Tests (xUnit)**:
   - High-precision isolation testing of domain entities, value objects, mathematical calculations, business rules, allocation engines, pricing logic, and state machines.
   - Comprehensive edge-case handling (boundaries, invalid inputs, overflow, concurrency).
2. **Behavior-Driven Development (BDD) Tests (Reqnroll / Gherkin)**:
   - Human-readable living specifications authored in Gherkin (`tests/VenueAxe.Bdd/Features/`).
   - Strongly typed step definitions in `tests/VenueAxe.Bdd/StepDefinitions/` validating user stories, acceptance criteria, domain invariants, and operational workflows in natural language.
3. **Integration Tests**:
   - Boundary verification between application services, PostgreSQL database transactions, Unit of Work, and repository implementations.
   - Multi-tenant data isolation verification (ensuring EF Core global query filters strictly prevent cross-tenant data leakage).
   - SignalR telemetry hub testing (real-time broadcast dispatch, group isolation, client reconnect resilience).
4. **End-to-End (E2E) Tests**:
   - Complete API workflow executions testing request pipelines, middleware, HttpOnly cookie authentication (`VenueAxe.Auth`), authorization roles, and structured RFC 7807 `ProblemDetails` error reporting.

### 4.3 Frontend Test Suite Requirements
Every frontend feature, view, component, modal, canvas, and user interaction must be accompanied by the following four tiers of tests:
1. **Component Tests**:
   - Isolated verification of Svelte 5 components in `src/lib/components/`.
   - Verification of runes reactivity (`$state`, `$derived`, `$props`, `$effect`), DOM bindings, accessibility, and user event dispatching.
2. **Integration Tests**:
   - Validation of cross-component workflows, Svelte 5 rune state stores, client services, SignalR telemetry subscriptions, and auto-generated API client integrations.
3. **End-to-End (E2E) Tests**:
   - Real browser automation testing full user journeys across all application flows (booking wizard, waiver signing kiosk, tablet scoring, overhead TV broadcast, and admin portal).
4. **Visual Regression Tests**:
   - Automated screenshot comparison tests across all physical form factor viewports (Overhead TV 1920x1080, Tablet 1024x768 / 1280x800, Mobile 390x844, and Admin Desktop 1920x1080).
   - Guarantees zero unintentional layout drift, pixel misalignment, or style regressions across themes and screen sizes.

### 4.4 Mandatory Feature Documentation (`docs/feature-documentation/[featureName]`)
Every feature in VenueAxe must be documented in `docs/feature-documentation/[featureName]`.
- **Scope & Coverage**: Any newly developed feature, or any existing feature or bug fix being worked on, must have its documentation created or updated in `docs/feature-documentation/[featureName]`.
- **Required Documentation Content**:
  1. **Overview & Business Value**: Purpose of the feature, target user roles (Owner, GM, Lane Master, Guest thrower), and core use cases.
  2. **Technical Architecture & Data Model**: Entities, database schema/JSONB structures, application services, and frontend components involved.
  3. **API & Telemetry Specifications**: Endpoints, DTO contracts, SignalR events, and security/multi-tenancy constraints.
  4. **Testing Strategy**: Pointers to corresponding backend (Unit, BDD, Integration, E2E) and frontend (Component, Integration, E2E, Visual Regression) tests.
  5. **Manual Verification & Viewport Guide**: Step-by-step instructions for testing and validating in Google Chrome across target form factors.

### 4.5 Automated Quality Gates (Must Pass Before Completion)
Before any task, feature, or bug fix is marked done, **ALL tests (including all existing pre-existing tests across the entire platform) must execute and pass with 0 errors, 0 failures, and 0 warnings**:
```bash
# 1. Backend Unit & Integration Test Suite (xUnit)
dotnet test tests/VenueAxe.Tests/VenueAxe.Tests.csproj --verbosity normal

# 2. Backend BDD Feature Test Suite (Reqnroll xUnit)
dotnet test tests/VenueAxe.Bdd/VenueAxe.Bdd.csproj --verbosity normal

# 3. Frontend Test Suite (Vitest)
pnpm --prefix src/frontend test

# 4. Frontend Type & Runes Verification (svelte-check)
pnpm --prefix src/frontend check
```

---

## 5. Manual Testing in Google Chrome (Extreme Depth & Rigorous Protocols)

Automated tests guarantee mathematical and logical correctness, but **manual testing in Google Chrome is mandatory** to prove real-world visual aesthetics, UX responsiveness, touch ergonomics, and dual-screen synchronization.

### 5.1 Service Startup Sequence
Ensure all infrastructure is running prior to testing:
1. **Start PostgreSQL 17**:
   ```bash
   docker compose up -d
   ```
2. **Run .NET 10 Web API**:
   ```bash
   dotnet run --project src/backend/VenueAxe.Web/VenueAxe.Web.csproj --urls http://localhost:5280
   ```
3. **Run SvelteKit Frontend**:
   ```bash
   cd src/frontend
   pnpm dev
   ```

---

### 5.2 Mandatory Chrome Verification Rules

#### 1. Zero Console Errors Policy
- Open Chrome DevTools (`F12` $\rightarrow$ **Console**).
- **The console must remain completely clean during all interactions.**
- 0 uncaught exceptions, 0 unhandled promise rejections, 0 404 asset failures, and 0 Svelte reactivity warnings.
- All network requests in the **Network** tab must return valid HTTP status codes (`200 OK`, `201 Created`, `204 No Content`, or expected structured `4xx ProblemDetails`).

#### 2. Strict Chrome Password Policy
- **Never test with simple passwords** like `password123` or `admin`.
- Chrome triggers unskippable, blocking security alerts ("A data breach exposed this password", "Weak password") that interrupt automated browser testing.
- **Always use long, complex test passwords**: `VenueAxeAdmin2026!#$` or `AxeThrowingMaster#99!`.

#### 3. Multi-Viewport & Form Factor Verification Matrix
Every feature must be verified at its target physical device viewport:
| Surface | URL | Target Viewport | Verification Checklist |
| :--- | :--- | :--- | :--- |
| **Overhead TV Display** | `/screen` | 1920x1080 (16:9 1080p/4K TV) | Zero scrollbars, high-visibility typography visible from 20ft, scannable QR codes, live HUD animations, podium celebration. |
| **In-Lane Tablet Console** | `/tablet` | 1024x768 or 1280x800 (Landscape) | Touch targets $\ge 48\text{px}$, responsive WATL target SVG, line-break override modal, clutch banner, undo button. |
| **Venue Admin Portal** | `/admin` | 1920x1080 / 1440x900 (Desktop) | Arena grid lane cards, session launchers, pricing rules, waiver search table, real-time status pills. |
| **Guest Booking Flow** | `/book/[slug]` | 390x844 (Mobile) & Desktop | Party size stepper, date picker, real-time slot grid, package selection, confirmation screen. |
| **Digital Waiver Kiosk** | `/sign/[slug]` | 390x844 (Mobile) & Tablet | Touch canvas signature drawing, smooth stroke rendering, minor add/remove, clear error banners. |

---

### 5.3 Live Dual-Screen Throwing Verification (Side-by-Side)
This is the core real-time telemetry user journey and must be tested whenever game engine, target math, or SignalR changes occur:

1. **Window 1 (In-Lane Tablet)**: Open `http://localhost:5173/tablet`.
   - Enter Tablet PIN: `AX101` $\rightarrow$ Click **Connect to Lane Terminal**.
2. **Window 2 (Overhead TV Display)**: Open `http://localhost:5173/screen`.
   - Enter TV PIN: `TV101` $\rightarrow$ Click **Connect Overhead TV Display**.
3. **Window 3 (Venue Admin Portal)**: Open `http://localhost:5173/admin`.
   - Log in with `owner@venueaxe.com` / `VenueAxeAdmin2026!#$`.
   - On **Lane 01**, click **+ Start Session** $\rightarrow$ Enter players: `Sarah, Marcus` $\rightarrow$ Select game: `WATL Standard` $\rightarrow$ Click **Launch Match**.
4. **Live Synchronization Audit**:
   - On the Tablet window, tap the interactive WATL target Bullseye.
   - **Verify**: The Tablet advances to the next thrower, awards 6 points, and the Overhead TV window *instantaneously* displays the hit ripple, scoreboard update, and golden particle celebration flash!
   - On the Tablet window, tap **CALL CLUTCH (7 PTS)** $\rightarrow$ **Verify**: The animated cyan Clutch banner pulses on both the Tablet and the Overhead TV screen.
   - On the Tablet window, tap the outer edge of Clutch $\rightarrow$ **Verify**: 7 points awarded on both screens.
   - On the Tablet window, tap **Undo Throw** $\rightarrow$ **Verify**: Both screens revert the last throw and restore the previous thrower's turn.
5. **Safety Emergency Freeze Audit**:
   - In the Venue Admin window, click **Safety Stop (Lane 01)**.
   - **Verify**: Both Tablet and TV screens immediately display high-contrast red warning screens locking all inputs until cleared by staff.

---

### 5.4 Customer Booking & Digital Waiver Flow Verification
1. Open `http://localhost:5173/book/downtown`.
   - Select party size: `6 Throwers`.
   - Select date and time slot $\rightarrow$ verify real-time price calculation updates dynamically.
   - Fill guest details $\rightarrow$ click **Complete Reservation**.
   - Verify booking reference is generated (e.g. `VA-84920`) with a scannable waiver QR code.
2. Open `http://localhost:5173/sign/downtown`.
   - Verify legal clauses load from the database template.
   - Enter signer information and add a minor (`Alex Jr.`).
   - Draw signature on the HTML5 touch canvas $\rightarrow$ verify smooth vector stroke rendering.
   - Click **Submit Signed Waiver** $\rightarrow$ verify instant confirmation and legal reference.
3. In Venue Admin (`http://localhost:5173/admin/waivers`):
   - Verify the signed waiver appears in the Waiver Vault in real-time with verified status, signature thumbnail, and booking association.

---

### 5.5 Regression Sweep Protocol
Whenever you modify a shared service, repository, component, or API endpoint:
1. Re-run all automated tests (`dotnet test`, `pnpm test`, `pnpm check`).
2. Manually test **at least two adjacent user flows** that interact with the changed component.
   - Example: If changing the `Booking` entity, test both the public booking wizard (`/book`) AND the admin booking table (`/admin/bookings`).
   - Example: If changing `WatlTarget.svelte`, test both the Tablet scoring console (`/tablet`) AND the TV visualizer (`/screen`).
3. If any defect or visual glitch is discovered, resolve it immediately and re-run the verification sweep.

---

## 6. Project Directory Blueprint

```
VenueAxe/
├── docker-compose.yml              # PostgreSQL 17 container definition
├── GEMINI.md                       # Enterprise architecture & operations guide
├── PRODUCT_SPECIFICATION.md        # Master product specification
├── README.md                       # Repository overview
├── VenueAxe.slnx                   # .NET 10 solution file
├── docs/                           # Modular specifications
│   ├── 01_BOOKING_ENGINE_AND_EDITOR.md
│   ├── 02_WAIVER_MANAGEMENT.md
│   ├── 03_LANE_MANAGEMENT_AND_OPERATIONS.md
│   ├── 04_LANE_GAMES_AND_WATL_SCORING.md
│   ├── 05_DATABASE_SCHEMA_AND_DATA_MODEL.md
│   ├── 06_12_FACTOR_AND_API_ARCHITECTURE.md
│   ├── 07_BUG_FIXES_AND_ERGONOMIC_ENHANCEMENTS.md
│   ├── 08_AXE_PLAY_ARCADE_GAMES_AND_REQUIREMENTS.md
│   └── 09_TESTING_AND_QUALITY_ASSURANCE_STANDARDS.md
├── src/
│   ├── backend/
│   │   ├── VenueAxe/               # Core Shared Library (UUIDv7, Entities, WATL Engine, Contracts)
│   │   ├── VenueAxe.Data/          # EF Core 10 PostgreSQL, Repositories, UnitOfWork, DataProtection
│   │   └── VenueAxe.Web/           # MVC Areas (Admin, Public, Waivers, Lanes), SignalR Hub, Cookie Auth
│   └── frontend/
│       ├── package.json            # SvelteKit 2.70.3 + Svelte 5 + @hey-api/openapi-ts
│       ├── openapi-ts.config.ts    # OpenAPI TypeScript client generation config
│       ├── src/
│       │   ├── app.css             # Vanilla CSS design tokens (Dark sports arena theme)
│       │   ├── lib/
│       │   │   ├── api/            # Auto-generated typed client SDK
│       │   │   ├── components/     # WatlTarget.svelte, WaiverCanvas.svelte, QrCode.svelte
│       │   │   ├── services/       # SignalR client connection service
│       │   │   └── stores/         # Svelte 5 runes auth and lane stores
│       │   ├── routes/             # SvelteKit CSR routes (/admin, /tablet, /screen, /book, /sign)
│       │   └── tests/              # Frontend Vitest test suites
└── tests/
    └── VenueAxe.Tests/             # xUnit unit & integration test suites
```

---

## 7. Code Quality & Architectural Standards

### 7.1 Single Responsibility & File Structure
- **One Type Per File Mandate**: Every C# class, record, struct, interface, and enum MUST reside in its own dedicated file matching the type name (e.g., `WatlTargetMath.cs`, `IUserContext.cs`, `LaneState.cs`).
  - **Strictly forbid dumping multiple classes or interfaces into a single file.**
- **Frontend Modularity**: Every Svelte component must reside in its own `.svelte` file with scoped styles. Utility functions and TypeScript interfaces must reside in dedicated `.ts` files under `src/lib/`.

### 7.2 Clean Architecture & Layer Boundaries
- **Core Domain Layer (`VenueAxe`)**: Zero dependencies on EF Core, ASP.NET Core, or external libraries. Houses pure domain entities, vector math, game engines, and service contracts.
- **Data Access Layer (`VenueAxe.Data`)**: Manages `DbContext`, repository implementations, and migrations. Never expose EF Core `IQueryable` directly to UI or controllers; encapsulate queries in repository methods.
- **Web API Layer (`VenueAxe.Web`)**: Controllers and SignalR hubs are thin coordinators. They validate inputs, delegate business logic to services, and return typed DTOs or standard RFC 7807 `ProblemDetails`.
- **Frontend Layer (`src/frontend`)**: Pure client-side rendering (CSR) using SvelteKit. All API communication is routed through the auto-generated typed SDK (`@hey-api/openapi-ts`).

### 7.3 Zero Placeholders & Zero TODOs
- **Full Implementations Only**: Never write placeholder code, stubbed methods that throw `NotImplementedException`, or `// TODO` comments for required features.
- Every feature, endpoint, and UI element must be fully connected, properly validated, error-handled, and persisted to the database.
- Mock data in production code paths is strictly prohibited.

### 7.4 Strict Type Safety & Nullability
- **Backend**: `#nullable enable` is enforced across all projects. All reference types must explicitly state nullability (`string?` vs `string`). Zero compiler warnings tolerated.
- **Frontend**: Strict TypeScript with `noImplicitAny`. Every API call and component property must have strong type definitions. Never use `any` as an escape hatch.

### 7.5 Modern Svelte 5 Runes Standard
- **Exclusively Svelte 5 Runes**: Always use `$state()`, `$derived()`, `$effect()`, and `$props()`.
- **Strictly Banned**: Legacy Svelte 3/4 reactive syntax (`export let`, `$: statement`, legacy writable stores) is strictly forbidden in new code.
- Components must manage lifecycle cleanly and tear down timers, intervals, and SignalR subscriptions in `$effect` teardown blocks.

### 7.6 Database Rigor & Multi-Tenancy
- **PostgreSQL 17+ Best Practices**: Use RFC 9562 Monotonic UUIDv7 for all primary keys (`Guid.CreateVersion7()`).
- **Index Optimization**: Explicit indexes on all foreign keys, tenant identifiers (`tenant_id`), status flags, and timestamps (`created_at`).
- **JSONB Strategy**: Use PostgreSQL `JSONB` with GIN indexing for flexible schemas (booking page custom themes, arcade game rules, waiver audit blobs).
- **Multi-Tenant Filter Verification**: Every entity query must enforce `TenantId == CurrentTenantId` via EF Core global filters, with documented justification for any `.IgnoreQueryFilters()` call.

### 7.7 Documentation Parity Mandate
- Whenever a feature is created, create dedicated documentation in the `docs/` folder.
- Whenever an existing feature is modified, immediately update its corresponding documentation in `docs/`.
- Ensure all technical documentation, API contracts, and user guides remain in 100% lockstep with the running codebase.
- Whenever a bug is fixed build whatever tests you need to ensure it won't happen again.
- Any and all changes require ALL tests to pass across the entire suite (including all existing unit, BDD, integration, e2e, component, and visual regression tests). Regressions and broken existing tests are strictly prohibited.
- Whenever a feature is created or enhanced, author or update corresponding BDD feature specifications (Reqnroll/Gherkin) in `tests/VenueAxe.Bdd/Features/` to guarantee executable living documentation for all business rules.
- Whenever you do manual tests in chrome. Document how the test is performed in `docs/manual-tests/[feature]/[test-name].md`. We'll use this prior to release to confirm features work as expected. Make sure you include what to look for and what assertions that should be made.
- Whenever you update existing features, or fix bugs. Update any manual tests that are relevant and run through the applicable tests. If a test fails, then fix the code until it works (or fix the manual test plan if applicable).
- Don't run through all the manual tests unless asked. Instead, just be smart about what you changed and what tests should be ran through.
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
   - **UUIDv7 Factory (`UuidV7.NewGuid()`)**: Monotonic, time-ordered primary keys using .NET 10 native `Guid.CreateVersion7()`.
   - **Ambient Multi-Tenant Context (`IUserContext`)**: Captures `TenantId`, `UserId`, `VenueId`, and `Role` from the active HTTP request.
   - **Domain Entities**: `Tenant`, `Venue`, `User`, `Lane`, `BookingConfig`, `Booking`, `BookingLane`, `WaiverTemplate`, `Waiver`, `LaneSession`, `GameMatch`, `MatchThrow`.
   - **WATL Target Math Engine (`WatlTargetMath`)**: High-precision vector collision algorithms for official World Axe Throwing League boards.
   - **Pluggable Game Engines**: `IGameEngine`, `WatlStandardMatchEngine`, `CountdownGameEngine`, `GameEngineRegistry`.
   - **Enterprise Repository & Service Contracts**: `IRepository<T>`, `ITenantRepository<T>`, `IUnitOfWork`, and domain service interfaces.

2. **`VenueAxe.Data`** (Data Access & Entity Framework Core):
   - **`VenueAxeDbContext`**: Configured exclusively for PostgreSQL 17 via Npgsql.
   - **Multi-Tenant Global Query Filters**: Queries automatically filter by `TenantId == CurrentTenantId`.
   - **PostgreSQL Data Protection (`IDataProtectionKeyContext`)**: Persists session cookie encryption keyrings directly in PostgreSQL (`DataProtectionKeys` table).
   - **Repository Implementations (`EfRepositories.cs`)**: `TenantRepository<T>` automatically assigns and enforces `TenantId` on mutations.
   - **Database Seeder (`DbInitializer.cs`)**: Seeds default tenant, venue (`Downtown`), 8 lanes, waiver template, booking packages, and owner account (`owner@venueaxe.com` / `password123`).

3. **`VenueAxe.Web`** (C# MVC API & Real-Time Hub):
   - **MVC Areas**:
     - `[Area("Admin")]`: Operator & Staff management (`AuthController`, `VenuesController`, `LanesController`, `BookingsController`, `WaiversController`, `BookingConfigController`).
     - `[Area("Public")]`: Customer booking & availability (`PublicBookingController`).
     - `[Area("Waivers")]`: Public digital waiver signing (`PublicWaiversController`).
     - `[Area("Lanes")]`: Hardware terminal pairing & live throw telemetry (`LaneTerminalsController`, `LaneOperationsController`).
   - **SignalR Telemetry (`LaneHub`)**: Sub-50ms synchronized match state broadcasts across in-lane tablets and overhead TV displays.
   - **OpenAPI 3.1 Pipeline**: Exposes native `/openapi/v1.json` specification used to generate the frontend TypeScript client.

4. **`src/frontend`** (SvelteKit 2.70.3 + Svelte 5):
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

## 4. Testing & Quality Assurance Plan

### 4.1 Unit Testing Strategy
Unit tests target core business logic and algorithms in isolation without database dependencies:
1. **WATL Target Coordinate & Collision Physics (`WatlTargetMathTests`)**:
   - Verify Bullseye hit detection: exact center `(0.0, 0.0)` and perimeter $\le 0.097 \implies 6\text{ points}$.
   - Verify ring boundary thresholds: Ring 5 ($0.097 < r \le 0.180 \implies 5\text{ pts}$), Ring 4 ($4\text{ pts}$), Ring 3 ($3\text{ pts}$), Ring 2 ($2\text{ pts}$), Ring 1 ($1\text{ pt}$).
   - Verify Left Clutch $(-0.380, 0.460)$ and Right Clutch $(0.380, 0.460)$:
     - When `isClutchCalled = true` $\implies 7\text{ points}$.
     - When `isClutchCalled = false` (uncalled clutch per WATL rules) $\implies 0\text{ points}$.
   - Verify off-target throws ($r > 0.514 \implies 0\text{ points}$, Zone: `Miss`).
2. **Game Engine State Machines (`GameEngineTests`)**:
   - `WatlStandardMatchEngine`: Validate 10-round progression, player turn rotation, consecutive bullseye streak counters, and automatic winner declaration.
   - `CountdownGameEngine`: Validate starting score deduction (301/501), bust handling when points exceed remaining score, and exact-zero victory condition.
3. **Cryptographic Security Tests**:
   - Verify PBKDF2/SHA-256 password salt generation, constant-time verification, and invalid password rejection.
   - Verify UUIDv7 sequential ordering: ensure newer IDs are chronologically greater than older IDs.

### 4.2 Functional & Integration Testing Strategy
1. **Multi-Tenant Repository Isolation**:
   - Verify that an authenticated user for `Tenant A` cannot read or modify bookings, waivers, or lanes belonging to `Tenant B`.
2. **Booking Engine Capacity & Lane Allocation**:
   - Simulate simultaneous booking requests for identical date/time slots; verify that capacity checks lock adjacent lanes and prevent overbooking.
3. **Waiver Legal Audit Trail**:
   - Verify that submitting a waiver generates an immutable SHA-256 legal hash, captures IP address/user-agent metadata, and links to the active booking.
4. **SignalR Lane Telemetry Protocol**:
   - Connect virtual Tablet and TV clients to group `lane_{laneId}`; verify that invoking `RecordThrow` dispatches `OnThrowRecorded` to all paired terminals in $< 50\text{ms}$.

---

## 5. Manual Testing in Google Chrome

To manually verify all user journeys in Chrome:

### 5.1 Step 1: Start Infrastructure & Services
1. **Start PostgreSQL**:
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

### 5.2 Step 2: Test Dual-Screen Lane Throwing (Side-by-Side)
1. Open Chrome Window 1: Navigate to `http://localhost:5173/tablet`.
   - Enter Tablet PIN: `AX101` $\rightarrow$ Click **Connect to Lane Terminal**.
2. Open Chrome Window 2: Navigate to `http://localhost:5173/screen`.
   - Enter TV PIN: `TV101` $\rightarrow$ Click **Connect Overhead TV Display**.
3. Open Chrome Window 3: Navigate to `http://localhost:5173/admin` (Login with `owner@venueaxe.com` / `password123`).
   - On **Lane 01**, click **+ Start Session** $\rightarrow$ Enter player names: `Sarah, Marcus` $\rightarrow$ Click **Launch Match**.
4. **Verify Real-Time Synchronization**:
   - On the Tablet window, tap the interactive WATL target Bullseye.
   - Observe that the Tablet rotates to the next thrower, updates points, and the Overhead TV window instantaneously displays the hit ripple, scoreboard update, and Bullseye celebration flash!
   - Tap **CALL CLUTCH (7 PTS)** on the tablet $\rightarrow$ observe the animated cyan Clutch banner pulse on the TV screen.

### 5.3 Step 3: Test Customer Booking & Waiver Flow
1. Navigate to `http://localhost:5173/book/downtown`.
   - Select party size: `6 Throwers` $\rightarrow$ Pick available time slot $\rightarrow$ Fill guest details $\rightarrow$ Click **Complete Reservation**.
   - Verify instant confirmation screen with booking reference (e.g. `VA-84920`).
2. Click **Sign Digital Waiver Now** (or navigate to `http://localhost:5173/sign/downtown`).
   - Read liability clauses $\rightarrow$ Fill signer details $\rightarrow$ Draw signature on touch canvas $\rightarrow$ Submit.
   - Verify green checkmark verification confirmation.
3. In the Venue Admin portal (`http://localhost:5173/admin`), navigate to **Waiver Vault** and verify the new signature appears in real-time.

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
│   └── 06_12_FACTOR_AND_API_ARCHITECTURE.md
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
│       │   │   ├── components/     # WatlTarget.svelte, WaiverCanvas.svelte
│       │   │   ├── services/       # SignalR client connection service
│       │   │   └── stores/         # Svelte 5 runes auth store
│       │   └── routes/             # SvelteKit CSR routes (/admin, /tablet, /screen, /book, /sign)
└── tests/
    └── VenueAxe.Tests/             # xUnit unit & integration test suites
```
## Additional workflow notes
- Whenever you build a feature. Test the entire feature in chrome. If any bugs are found, fix the code and try again until the feature works flawlessly. Test any other features you could have touched while building the new feature and fix those bugs too.
- When developing features you should always make unit tests and any functional tests both in the backend and the frontend
- When testing passwords in chrome, use complex long passwords. Otherwise chrome throws an alert you can't skip telling you your passwords aren't secure enough.
- Do not build any "placeholder" or "todo" items or features. Build the feature properly. Your output should be a full implementation.
- If you have built a new feature. Document the feature in `docs` if you have altered a feature update the docs in `docs` and if documentation doesn't already exist then create the documentation.
# VenueAxe - Comprehensive Product Specification & Architecture Document

**Version:** 1.0.0-PROD-SPEC  
**Target Framework:** .NET 10  
**Database:** PostgreSQL 17+  
**Frontend:** Svelte 5 (SvelteKit)  
**Real-Time Subsystem:** ASP.NET Core SignalR  
**Architecture Standard:** 12-Factor Application  

---

## Executive Summary & System Vision

**VenueAxe** is an all-in-one commercial Axe Throwing Venue Management platform engineered for venue owners, lane masters, and operators. The platform replaces fragmented point solutions (separate booking widgets, paper/tablet waiver apps, manual chalkboard scoring, and disconnected POS systems) with a unified, real-time operating system.

### Core Pillars
1. **Zero-Friction Venue Onboarding & Multi-Tenancy**: Isolated multi-tenant architecture supporting single-location operators up to enterprise multi-location franchises.
2. **Owner-Only Authentication**: Secure management and lane operations without requiring consumer accounts. End-customers interact purely as guests via booking widgets, digital waiver kiosks, and lane tablet sessions.
3. **Dynamic Booking Page Builder & Embeddable Widget**: Fully customizable online booking flow embeddable via `<iframe>` or hosted standalone with real-time lane capacity checks and Stripe payment processing.
4. **Legally Robust Digital Waiver Vault**: Digital signing with guardian/minor support, signature audit metadata, QR quick-sign, and automated association with bookings and lane sessions.
5. **Real-Time Lane Operations Center**: Live floor map, lane status monitoring, session timers, and one-click session dispatching.
6. **Dual-Screen Interactive Lane Games Platform**:
   - **Thrower Tablet Console (In-Lane)**: High-contrast touch UI with an interactive World Axe Throwing League (WATL) target board outline allowing throwers/coaches to tap exact hit points.
   - **Overhead Lane Display (TV / Monitor)**: Broadcast-quality 4K/1080p real-time game board displaying live scores, player turns, clutch calls, hit visualizers, and venue attract loops.
7. **12-Factor & Cloud-Native Foundation**: Stateless .NET 10 API, PostgreSQL backing store, container-ready configuration, zero-downtime scalability, and bi-directional WebSockets/SignalR telemetry.

---

## 1. Multi-Tenancy & Venue Hierarchy

### 1.1 Tenant Isolation Model
- **Isolation Strategy**: Logical tenant isolation via `TenantId` partitioning with Entity Framework Core Global Query Filters and optional PostgreSQL Row-Level Security (RLS) enforcement.
- **Tenant Resolution**:
  - Subdomain routing: `[venue-slug].venueaxe.com`
  - Custom domain CNAME mapping: `book.customaxevenue.com`
  - JWT Claims extraction for authenticated venue staff (`tenant_id`).
  - X-Tenant-Key / Public Venue ID header for external iframe widgets and waiver kiosks.

### 1.2 Venue & Location Hierarchy
- **Tenant / Organization**: Top-level business entity (billing, brand defaults, overall admin accounts).
- **Venues / Locations**: Physical storefronts under the organization.
  - Name, physical address, geo-coordinates, contact details.
  - Operational timezone (critical for scheduling calculations).
  - Business operating hours (regular weekly schedule, holiday exceptions, seasonal overrides).
  - Custom branding: Primary/Secondary accent colors, venue logo (light & dark mode), background hero images, custom CSS variables.
  - Payment credentials: Stripe Connect / custom merchant keys per location.
  - Tax rates and currency configuration.

---

## 2. Authentication & Access Control (Staff & Venue Owners Only)

> **Core Principle**: No player/consumer accounts. Throwers are treated as session guests. Logins are strictly reserved for venue operators, managers, axe coaches, and lane staff.

### 2.1 Role-Based Access Control (RBAC)
| Role | Scope | Permissions & Capabilities |
| :--- | :--- | :--- |
| **System Admin (Superadmin)** | Platform-wide | Tenant provisioning, billing oversight, platform health, global game catalog management. |
| **Venue Owner** | Organization & Venues | Full access: billing, staff management, booking page editor, lane layouts, pricing rules, waiver templates, analytics. |
| **General Manager** | Single/Assigned Venues | Booking management, refund processing, staff scheduling, lane configuration, waiver inspection, daily revenue reporting. |
| **Lane Master / Staff** | Assigned Venue | Lane dashboard, check-in guests, assign lanes, override game scores, trigger safety stops, monitor tablet/TV pairings. |
| **Terminal / Kiosk Service Account** | Device / Lane Pair | Restricted API key token for in-lane tablets and TV overhead displays (auto-reconnects, cannot access admin/financial data). |

### 2.2 Security & Session Management
- ASP.NET Core Identity with PostgreSQL storage.
- JWT tokens with short expiry (15 mins) and sliding refresh tokens stored in HttpOnly secure cookies.
- Device Pairing Code flow for Lane Tablets and Overhead Monitors (6-digit pairing PIN entered at setup to bind device to `VenueId` + `LaneId`).

---

## 3. Booking Engine & Booking Page Editor

### 3.1 Visual Booking Page Editor (Owner Portal)
Venue owners can design and customize their public booking experience without writing code:
- **Theme & Layout Customizer**:
  - Color palette selection (Primary, Background, Surface, Text, Accent).
  - Typography selection (Modern Sans, Sport Bold, Minimal Industrial).
  - Hero banner upload, logo placement, venue description, social links.
  - Custom announcement banners (e.g., "Special Holiday Hours" or "League Night Tonight").
- **Booking Flow Configuration**:
  - Step order: Date/Time First vs Package First vs Party Size First.
  - Slot durations: 30m, 60m, 90m, 120m, 180m increments.
  - Turnaround buffer time: 5m to 15m cleaning/safety briefing buffer between bookings.
  - Minimum and Maximum party size rules per booking (e.g., 2 to 24 throwers).
  - Maximum throwers per single lane (e.g., max 6 throwers/lane; 7-12 throwers automatically claims 2 adjacent lanes).
- **Pricing & Payment Settings**:
  - Pricing models: Per Person / Per Lane / Flat Group Rate.
  - Day & Time matrix: Off-peak pricing vs Peak pricing (e.g., Friday/Saturday evening rates).
  - Add-ons engine: Drinks packages, coaching session, custom target posters, merchandise, celebration cakes.
  - Deposit policy: Require 100% full payment, fixed deposit (e.g., $50 deposit), or per-person deposit at checkout.
  - Refund & Cancellation policies displayed prior to checkout.
- **Custom Intake Fields**:
  - Event type (Birthday, Corporate, Bachelor/ette, Date Night, Casual).
  - Custom text questions (e.g., "Where did you hear about us?", "Special accessibility accommodations").

### 3.2 External Booking Page (Hosted & Embeddable)
- **Standalone URL**: `https://book.venueaxe.com/[venue-slug]` or custom CNAME.
- **Responsive Embed Mode (`<iframe>`)**:
  - Lightweight embedded widget bundle.
  - Seamless `postMessage` protocol: Communicates dynamic height changes to parent host window (zero scrollbars inside iframe).
  - Cross-origin secure event messaging for Google Analytics, Meta Pixel, and conversion tracking.
- **Booking Steps**:
  1. **Select Experience / Package**: Casual Throwing, Private Lane Rental, Glow Axe Night, Corporate Package.
  2. **Select Party Size**: Dynamic lane counter indicator.
  3. **Select Date & Available Time Slot**: Live availability query preventing double bookings.
  4. **Select Add-ons**: Beverages, merchandise, dedicated Axe Coach.
  5. **Contact Information**: Primary booker Name, Email, Phone Number.
  6. **Waiver Pre-Signing Prompt**: Displays shareable link / QR code for party guests to sign before arrival.
  7. **Checkout & Payment**: Stripe Elements / Payment Request API (Apple Pay, Google Pay, Credit Card).
  8. **Confirmation & Calendar Export**: Instant confirmation screen + Add to Apple/Google Calendar + automated email dispatch.

---

## 4. Digital Waiver Management System

### 4.1 Waiver Builder & Versioning
- Customizable legal liability contract text with dynamic merge tags (`{{VenueName}}`, `{{GuestName}}`, `{{DOB}}`, `{{Date}}`).
- Mandatory minor liability clauses with Guardian details requirement for participants under legal age (e.g., 18).
- Semantic versioning: When legal terms change, new version is created; system tracks which version each customer signed.

### 4.2 Signing Channels
1. **Online Post-Booking Flow**: Primary booker receives a party waiver link to distribute to all attendees.
2. **On-Site QR Kiosk**: QR codes displayed at venue reception or entrance allow walk-ins to scan and sign on their mobile devices.
3. **Reception / Check-In Tablet**: Standalone kiosk mode with auto-reset after signing.

### 4.3 Signature Capture & Legal Audit Log
- Touch/stylus/mouse HTML5 Canvas signature capture.
- Stored audit record:
  - Full legal name, Date of Birth, Email, Phone number.
  - Minor participants listed under guardian.
  - Digital signature PNG + SVG vector data.
  - Signed timestamp (UTC), IP address, User Agent, Waiver Version Hash.
- Expiration rules: Configurable validity (e.g., valid for 24 hours, 30 days, or 1 year).

### 4.4 Waiver Verification & Association
- Staff search console: Search by Name, Phone, Email, or QR Code scan.
- Automatic link to active Booking: Checked-in throwers turn green on the Lane Manager screen once their waiver is verified.

---

## 5. Lane Management & Floor Operations

### 5.1 Real-Time Lane Operations Grid
- **Interactive Venue Map & Grid View**:
  - Live status indicators for all physical lanes (e.g., Lanes 1 through 12).
  - Status states:
    - `Available` (Ready for walk-in or assignment)
    - `Reserved` (Upcoming booking within 30 minutes)
    - `Active / Throwing` (Game session in progress with countdown timer)
    - `Time Expiring` (< 5 minutes remaining, orange alert)
    - `Turnaround / Cleaning` (Session ended, awaiting lane reset)
    - `Maintenance / Out of Order`
- **Session Control Actions**:
  - **Quick Start Walk-in**: Instant assignment with duration picker.
  - **Launch Booking**: Auto-populates guest list and pre-signed waivers from online booking.
  - **Add Time / Extend**: Add +15m, +30m with one-click upsell billing.
  - **Transfer Session**: Move group from Lane 3 to Lane 4 seamlessly without losing game progress.
  - **Safety Stop / Pause Timer**: Instantly pauses game on Tablet & TV screen with visual warning.
  - **Lane Pairing / Group Mode**: Sync multiple lanes together for a shared corporate tournament leaderboard.

---

## 6. Lane Games System (Tablet UI & Overhead TV Monitor)

### 6.1 System Architecture & Telemetry
```
                     +----------------------------------+
                     |         .NET 10 Backend          |
                     |  - GameEngineState Hub (SignalR) |
                     |  - REST APIs (EF Core / Postgres)|
                     +-----------------+----------------+
                                       |
                   Bi-Directional Real-Time WebSockets
                                       |
            +--------------------------+--------------------------+
            |                                                     |
            v                                                     v
+-----------------------+                             +-----------------------+
|   Lane Tablet UI      |                             | Overhead TV Display   |
| (In-Lane Console)     |                             | (Above Lane Monitor)  |
| - Game Selection      |                             | - 4K/1080p Broadcast  |
| - Player Roster       |                             | - Live Match Scores   |
| - WATL Target Pad     |                             | - Target Hit Mirror   |
| - Score Overrides     |                             | - Attract & Ad Screen |
+-----------------------+                             +-----------------------+
```

### 6.2 Thrower Tablet UI (Scorekeeper Console)
- **Ergonomics & Design**: High-contrast, dark-mode, anti-glare typography with large touch targets designed for rugged Android/iPad/browser tablets mounted at the lane thrower station.
- **Workflow**:
  1. **Lobby / Setup Screen**:
     - Displays session remaining time countdown.
     - Add/Edit Throwers: Quick name input, guest selection from signed waivers, player avatar color picker.
     - Choose Game Mode from the venue’s catalog.
     - Configure match parameters (e.g., Number of Rounds: 5, 10, or Custom; Singles or Teams).
  2. **Interactive WATL Target Board Scoring Input**:
     - **Authentic WATL Regulation Board**:
       - 6-Ring (Bullseye - Black inner circle): **6 points**
       - 5-Ring (Red inner ring): **5 points**
       - 4-Ring (Blue ring): **4 points**
       - 3-Ring (Red outer ring): **3 points**
       - 2-Ring (Blue outer ring): **2 points**
       - 1-Ring (Black perimeter ring): **1 point**
       - Clutch Targets (Left & Right top corners, blue circles): **7 points** (Active only when called on designated throws, e.g., throw 5 & 10, or in custom game modes).
       - Fault / Drop / Miss Area: **0 points**.
     - **Interactive Tap Scoring**:
       - Thrower/Coach simply taps where the axe stuck on the SVG target board.
       - Vector collision detection instantly determines point value and Clutch eligibility.
       - Visual hit pin dropped on target with axe icon.
       - System auto-scores and rotates to the next thrower in sequence.
     - **Controls & Overrides**:
       - Quick buttons: Bullseye (6), Clutch Left/Right (7), 5, 4, 3, 2, 1, Miss (0), Fault (0).
       - Call Clutch Button (arms the clutch target for the upcoming throw).
       - "Undo Last Throw" / "Edit Score" modal for dispute resolution.
       - "Next Thrower" / "Skip Turn" manual override.
  3. **End of Match & Summary**:
     - Winner declaration, final scorecard, stats (Bullseye accuracy %, Clutch conversion %, total score).
     - "Play Again", "Switch Game", or "Return to Lobby".

### 6.3 Overhead Lane Display UI (Monitor Screen)
- **Visual Design**: Broadcast-inspired sports graphics (ESPN/PGA tour styling), 16:9 widescreen layout, ultra-clear visibility from 15–20 feet away.
- **Live Match View**:
  - **Header Bar**: Lane Number, Session Time Remaining Clock, Match Title (e.g., "WATL Standard - Round 7/10").
  - **Active Thrower Card**: Huge name banner, current round score, throws remaining, streak indicator (e.g., "🔥 3x Bullseye").
  - **Live Target Visualizer**: SVG Target board showing exact coordinates of all throws in the current round with animated hit flashes.
  - **Scoreboard Leaderboard**: Dynamic table of all players ranked with cumulative scores and round-by-round point breakdown.
  - **Clutch Alert Banner**: Flashes high-visibility amber/neon warning when a thrower calls a Clutch attempt.
- **Excitement & Celebration Animations**:
  - Full-screen animated VFX on Bullseye, Clutch Hit, and Victory.
- **Idle / Attract Mode (When no game is active)**:
  - Venue custom branding & sponsor logos.
  - QR Code to scan for drink/food menu, waiver signing, or social tag.
  - Venue high score records / Daily leaderboard.

### 6.4 Pluggable Game Engine Specification
The game system utilizes a generic state-machine engine allowing endless game modes:
- **Base Game Engine Interface**:
  - `Initialize(config, players)`
  - `ProcessThrow(throwEvent: { playerId, hitCoordinates, targetRegion, isClutchCalled })`
  - `CalculateScore(throwEvent) -> ScoreResult`
  - `CheckWinCondition(gameState) -> WinResult`
  - `GetNextTurn(gameState) -> PlayerTurn`
- **Standard Included Games**:
  1. **WATL Standard Match**: 10 throws per player, 5 throws per half, Clutch enabled on throw 5 and 10. Highest total points wins.
  2. **Around the World**: Players must hit 1 -> 2 -> 3 -> 4 -> 5 -> Bullseye -> Clutch in sequence. First to complete wins.
  3. **Count-Up / Open Throw**: Freeform practice mode tracking hit heatmaps and average score per round.
  4. **Axe Tic-Tac-Toe**: Target divided into a 3x3 grid; hitting a grid sector claims that square for your team.
  5. **21 / Blackjack**: Players accumulate points trying to reach exactly 21 without busting over.
  6. **Clutch Hunter**: Only Clutch and Bullseye score points; all other rings score zero.

---

## 7. 12-Factor Application Architecture

| Factor | Implementation in VenueAxe |
| :--- | :--- |
| **I. Codebase** | Single git repository tracking backend (.NET 10) and frontend (Svelte 5) with modular micro-clean architecture. |
| **II. Dependencies** | Explicit dependencies via `Directory.Packages.props` / NuGet for .NET and `pnpm` / `package.json` for frontend. Zero implicit system tools. |
| **III. Config** | 100% environment-driven configuration (Postgres connection string, Redis URL, JWT secrets, Stripe API keys, CORS origins) loaded via `appsettings.json` + `ASPNETCORE_*` env variables. |
| **IV. Backing Services** | PostgreSQL, Redis (for distributed SignalR / cache), S3/Blob storage (waiver signatures/PDF exports), and Stripe treated as attached URL resources. |
| **V. Build, Release, Run** | Strict separation: Multi-stage Docker builds -> Image tag release -> Container execution with immutable artifacts. |
| **VI. Processes** | Web API and SignalR hubs are completely stateless. Session state is persisted to PostgreSQL and synchronized across instances via Redis backplane. |
| **VII. Port Binding** | Self-contained ASP.NET Core Kestrel HTTP/HTTPS port binding (`PORT` / `ASPNETCORE_URLS`). |
| **VIII. Concurrency** | Scale horizontally by spinning up additional .NET container instances behind a reverse proxy (Nginx / Traefik / AWS ALB). |
| **IX. Disposability** | Fast startup (< 1.5s), graceful shutdown listening to `SIGTERM` / `SIGINT` with connection draining on active SignalR lane sockets. |
| **X. Dev/Prod Parity** | Docker Compose environment running PostgreSQL 17 matching production database engines identically. |
| **XI. Logs** | Structured JSON logs written directly to `stdout`/`stderr` using Serilog / Microsoft.Extensions.Logging, ready for OpenTelemetry / Datadog / Grafana Loki ingestion. |
| **XII. Admin Processes** | EF Core database migrations, seed data generation, and maintenance routines run as one-off CLI commands (`dotnet run -- --migrate`). |

---

## 8. Technology Stack & Directory Blueprint

### 8.1 Technology Components
- **Backend Framework**: .NET 10.0 (C# 13) Web API
- **ORM & Data Access**: Entity Framework Core 10.0 + Npgsql (PostgreSQL provider)
- **Real-Time Communication**: ASP.NET Core SignalR with Redis Backplane support
- **API Documentation**: OpenAPI / Scalar / Swagger (.NET 10 native OpenAPI)
- **Frontend Framework**: Svelte 5 with SvelteKit (Client-side routing, high-performance runes reactivity)
- **Styling Architecture**: Vanilla CSS + Design Token System (Dark, industrial-sleek, sports venue aesthetic, zero bulky bloated UI kits)
- **Database**: PostgreSQL 17+ with JSONB support for dynamic game state and waiver metadata

### 8.2 Solution Architecture Blueprint
```
VenueAxe/
├── docs/
│   ├── SPECIFICATION.md
│   ├── DATABASE_SCHEMA.md
│   ├── API_CONTRACTS.md
│   └── GAME_ENGINE_SPEC.md
├── src/
│   ├── backend/
│   │   ├── VenueAxe.Api/               # .NET 10 Web API, SignalR Hubs, Endpoints, Controllers
│   │   ├── VenueAxe.Core/              # Domain Models, Enums, Interfaces, Game Engine Rules
│   │   ├── VenueAxe.Application/       # CQRS / Services, DTOs, Validators, Business Logic
│   │   └── VenueAxe.Infrastructure/    # EF Core DbContext, Migrations, Postgres Repositories, Stripe, S3
│   └── frontend/
│       ├── src/
│       │   ├── lib/
│       │   │   ├── components/
│       │   │   │   ├── admin/          # Venue Settings, Booking Editor, Waiver Manager, Lane Grid
│       │   │   │   ├── booking/        # Embeddable Booking Widget & Checkout Flow
│       │   │   │   ├── games/          # Target Board SVG, Scorecards, Animation Overlays
│       │   │   │   ├── lane-tablet/    # In-Lane Thrower Scorekeeper Console
│       │   │   │   ├── lane-screen/    # Overhead TV Broadcast Display
│       │   │   │   └── ui/             # Design System Tokens, Modals, Buttons, Inputs
│       │   │   ├── stores/             # SignalR Client, Auth Store, Session Store
│       │   │   └── api/                # Typed REST Clients
│       │   └── routes/                 # SvelteKit Routes (Admin, Book, Lane Tablet, Lane TV)
│       └── package.json
├── docker-compose.yml
├── Dockerfile.backend
├── Dockerfile.frontend
└── README.md
```

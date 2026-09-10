# VenueAxe - All-in-One Axe Venue Management Platform

A high-performance, commercial axe throwing venue operating system built on **.NET 10**, **PostgreSQL 17+**, and **Svelte 5**.

## 📖 Product Specifications & Architecture

Full modular product and technical specifications are available in the [`docs/`](./docs) directory:

- 📄 **[Master Product Specification](./docs/PRODUCT_SPECIFICATION.md)** - System vision, 12-factor architecture, role matrix, and directory layout.
- 🎯 **[01 - Booking Engine & Custom Page Editor](./docs/01_BOOKING_ENGINE_AND_EDITOR.md)** - Visual editor, dynamic pricing, capacity allocation, embeddable iframe widget, and Stripe checkout.
- ✍️ **[02 - Digital Waiver Management](./docs/02_WAIVER_MANAGEMENT.md)** - Template builder, canvas signature capture, guardian/minor handling, SHA-256 audit log, and QR quick-sign.
- 🏟️ **[03 - Lane Management & Floor Operations](./docs/03_LANE_MANAGEMENT_AND_OPERATIONS.md)** - Real-time floor plan, lane state machine, countdown timers, walk-in dispatch, and hardware terminal pairing.
- 🪓 **[04 - Lane Games & Interactive WATL Scoring](./docs/04_LANE_GAMES_AND_WATL_SCORING.md)** - Dual-screen system (Tablet Console + Overhead TV Screen), WATL SVG target board geometry with tap collision detection, and pluggable game engine.
- 🗄️ **[05 - PostgreSQL Database Schema & Data Models](./docs/05_DATABASE_SCHEMA_AND_DATA_MODEL.md)** - Complete DDL tables, multi-tenant partitioning, JSONB configs, and ERD diagrams.
- ⚡ **[06 - 12-Factor System Architecture & SignalR Telemetry](./docs/06_12_FACTOR_AND_API_ARCHITECTURE.md)** - 12-Factor compliance, real-time WebSocket protocol (`LaneHub`), REST endpoints, and Svelte 5 design tokens.
- 🛠️ **[07 - Bug Fixes & Ergonomic Enhancements](./docs/07_BUG_FIXES_AND_ERGONOMIC_ENHANCEMENTS.md)** - Multi-tenant query filter bypass architecture, touch de-duplication, and arena layout fixes.
- 🎮 **[08 - Axe Play Arcade Games & Requirements](./docs/08_AXE_PLAY_ARCADE_GAMES_AND_REQUIREMENTS.md)** - Interactive party game rules, Blackjack, Tic-Tac-Toe, Zombie Attack, and Duck Hunter.
- 🧪 **[09 - Testing Standards & Chrome Verification Protocols](./docs/09_TESTING_AND_QUALITY_ASSURANCE_STANDARDS.md)** - Automated test pyramid, xUnit, Vitest, and mandatory Chrome multi-screen manual testing checklist.

---

## 🧪 Automated Testing & Quality Gates

VenueAxe enforces an extreme quality standard. All automated tests and type checks must pass with **0 errors and 0 warnings** prior to merging or deploying:

```bash
# 1. Run Backend Unit & Integration Tests (xUnit)
dotnet test tests/VenueAxe.Tests/VenueAxe.Tests.csproj --verbosity normal

# 2. Run Frontend Unit Tests (Vitest)
pnpm --prefix src/frontend test

# 3. Run Frontend Runes & TypeScript Verification (svelte-check)
pnpm --prefix src/frontend check
```

---

## 🚀 Quick Start (Development Scripts)

### Option 1: Start Everything (All-in-One)
To launch PostgreSQL 17, the .NET 10 API, and the SvelteKit frontend in separate terminal windows:
- **PowerShell**: `.\scripts\start-all.ps1`
- **Command Prompt**: `scripts\start-all.cmd`

### Option 2: Start Services Individually
- **Backend & Database**:
  - PowerShell: `.\scripts\start-backend.ps1`
  - Command Prompt: `scripts\start-backend.cmd`
- **Frontend Dev Server**:
  - PowerShell: `.\scripts\start-frontend.ps1`
  - Command Prompt: `scripts\start-frontend.cmd`

### Option 3: Database Migrations
The web application automatically applies all pending migrations on startup (`await db.Database.MigrateAsync()`).
To manually manage migrations via CLI:
- **Apply Migrations**: `.\scripts\migrate-db.ps1` / `scripts\migrate-db.cmd`
- **Add New Migration**: `.\scripts\add-migration.ps1 "MigrationName"` / `scripts\add-migration.cmd`

### Access Points
- **Frontend Portal Launchpad**: `http://localhost:5173`
- **Venue Admin Operations**: `http://localhost:5173/admin` (Demo Login: `owner@venueaxe.com` / `password123` or `VenueAxeAdmin2026!#$`)
- **In-Lane Tablet Console**: `http://localhost:5173/tablet` (Demo PIN: `AX101`)
- **Overhead TV Broadcast**: `http://localhost:5173/screen` (Demo PIN: `TV101`)
- **Public Booking Flow**: `http://localhost:5173/book/downtown`
- **Digital Waiver Kiosk**: `http://localhost:5173/sign/downtown`
- **Backend API & Swagger UI**: `http://localhost:5280/swagger`



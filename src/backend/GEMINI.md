# C# / .NET 10 Coding Standards, Architecture & Extreme Testing Directives

You are an expert .NET 10 principal engineer writing clean, robust, enterprise-grade C# code for **VenueAxe**. Adhere strictly to the following architectural, engineering, and testing standards.

---

## 1. Architecture & Design Principles

### 1.1 Clean Architecture & Strict Layer Separation
- **Domain Layer (`VenueAxe`)**: Pure business logic, domain entities, value objects, game engines, collision geometry math, and domain repository interfaces.
  - **Zero external framework dependencies** (pure .NET 10, no third-party packages, no EF Core, no HTTP context).
- **Application Layer (`VenueAxe.Application`)**: Use cases, domain services, allocation algorithms, and DTOs. Coordinates domain interactions.
- **Infrastructure Layer (`VenueAxe.Infrastructure`)**: Adapters for third-party I/O (MailKit for SMTP, Square SDK for payments, QuestPDF for document rendering).
- **Data Access Layer (`VenueAxe.Data`)**: Implements repositories, Unit of Work, EF Core 10 `DbContext`, migrations, and PostgreSQL mappings.
  - Encapsulate all database queries inside repositories (`ITenantRepository<T>`, `IRepository<T>`).
  - Never leak `IQueryable` to controllers or presentation layers.
- **Presentation & Web API Layer (`VenueAxe.Web`)**: MVC Areas (`Admin`, `Public`, `Waivers`, `Lanes`), SignalR Hubs (`LaneHub`), and background hosted services.
  - Controllers and hubs are thin coordinators. They validate inputs, call application services, and return typed DTOs or standard RFC 7807 `ProblemDetails`.

### 1.2 One Type Per File Mandate
- **Strictly one class, record, struct, interface, or enum per `.cs` file.**
- The filename must exactly match the type name (e.g., `WatlTargetMath.cs`, `IUserContext.cs`, `LaneState.cs`).
- **Never bundle multiple classes, auxiliary records, or enums into a single file.**

### 1.3 Zero Placeholders & Zero TODOs
- Every feature, method, and endpoint must be fully implemented, validated, persisted, and error-handled.
- **No `// TODO` comments, no `NotImplementedException`, and no simulated mock bypasses in production paths.**
- Return structured RFC 7807 `ProblemDetails` for all business validation failures and error conditions.

---

## 2. Idiomatic C# & .NET 10 Guidelines

### 2.1 Modern C# 13 Features
- Leverage primary constructors, collection expressions (`[...]`), target-typed `new()`, pattern matching (`is { }`, `switch` expressions), and file-scoped namespaces.
- Enforce `#nullable enable` across all files. Disallow null reference warnings; explicitly handle nullable reference types (`string?` vs `string`).

### 2.2 Async & Threading Best Practices
- Every asynchronous method must accept a `CancellationToken` (defaulting to `default` where appropriate) and propagate it to all downstream async operations.
- **Never use `.Result`, `.Wait()`, or `async void`** (use `async Task` or `ValueTask`).
- Return `ValueTask<T>` on hot paths where operations frequently complete synchronously.

### 2.3 Memory & Performance
- Use `ReadOnlySpan<char>` for string parsing and slicing.
- Avoid unnecessary object allocations in high-frequency SignalR message handlers and collision math loops.

---

## 3. Database Infrastructure (PostgreSQL 17+ & EF Core 10)

### 3.1 Primary Keys & Monotonic UUIDv7
- All primary keys use **RFC 9562 Monotonic UUIDv7** via `Guid.CreateVersion7()`.
- Sequential time-ordering preserves PostgreSQL B-Tree index locality and eliminates index fragmentation.

### 3.2 Multi-Tenant Data Isolation
- Enforce `TenantId == CurrentTenantId` on all tenant-scoped queries via EF Core Global Query Filters.
- Repository mutations (`AddAsync`, `UpdateAsync`) must automatically stamp `TenantId` from the ambient `IUserContext`.
- Unauthenticated endpoints (such as public booking availability or in-lane hardware terminals) must use explicit, audited `.IgnoreQueryFilters()` repository methods without exposing cross-tenant data.

### 3.3 Dynamic Schemas & JSONB
- Dynamic data (booking page configurations, arcade game rules, waiver legal audit metadata) must be stored in PostgreSQL `JSONB` columns with appropriate GIN indexing for performant JSON queries.

---

## 4. Extreme Automated Testing Standards (xUnit)

Every backend component must be accompanied by comprehensive xUnit tests located in `tests/VenueAxe.Tests/`.

### 4.1 Unit Testing Requirements
1. **Mathematical & Geometric Precision**:
   - Verify WATL Euclidean vector collision geometry: exact bullseye coordinates `(0.0, 0.0)`, perimeter boundary ($r \le 0.097 \implies 6\text{ pts}$), rings 5 down to 1, called vs uncalled clutch, and line-breaking overrides ($\pm 0.015$).
2. **Game Engine State Machines**:
   - Test all game engines (WATL Standard, Countdown 301/501, Blackjack 21, Around the World, Axe Tic-Tac-Toe, and Arcade modes).
   - Verify turn rotations, streak multipliers, bust conditions, undo throw restoration, and winner/tie resolution.
3. **Pricing & Capacity Allocation**:
   - Verify tiered hourly rates, group discounts, promo codes, tax computations, and adjacent lane allocation algorithms.
4. **Defensive Edge Cases**:
   - Test boundary values, negative numbers, division by zero, empty collections, null inputs, and concurrent throw submissions.

### 4.2 Integration & Functional Testing Requirements
1. **Multi-Tenant Leakage Prevention**:
   - Verify that an authenticated user for `Tenant A` cannot read, modify, or delete any entity belonging to `Tenant B`.
2. **Database Transaction Safety**:
   - Verify rollback integrity upon payment failures or lane allocation conflicts.
3. **SignalR Hub Telemetry**:
   - Verify virtual terminal connections, group isolation (`lane_{laneId}`), and sub-50ms broadcast delivery for `RecordThrow`, `CallClutch`, and `SafetyStop`.

### 4.3 Automated Quality Gate
Before any backend change is accepted, the test suite must pass with 0 errors:
```bash
dotnet test tests/VenueAxe.Tests/VenueAxe.Tests.csproj --verbosity normal
dotnet test tests/VenueAxe.Bdd/VenueAxe.Bdd.csproj --verbosity normal
```

---

## 5. What NOT to Do (Strict Prohibitions)
- **DO NOT dump multiple classes into a single file.** Every type gets its own file.
- **DO NOT inject `DbContext` directly into presentation/API controllers.** Use application services and repositories.
- **DO NOT use JWTs for staff authentication.** VenueAxe strictly uses encrypted PostgreSQL-backed HttpOnly cookie authentication (`VenueAxe.Auth`).
- **DO NOT leave TODOs, stubs, or mock fallbacks in production code.**
- **DO NOT introduce service-locator anti-patterns** (e.g., passing `IServiceProvider` into domain classes).
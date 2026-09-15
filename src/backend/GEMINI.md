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

## 4. Backend Testing Standards & Feature Documentation Mandate

Every backend feature and bug fix must be accompanied by comprehensive tests across all testing tiers. Quality, complete test coverage, and documentation are mandatory requirements for any backend code.

### 4.1 All-Encompassing Backend Testing Mandate (Zero Exemptions)
- **Universal Application across ALL Features**: Any and all features across the backend must have full test coverage. Testing requirements are never limited to specific features or modules—they are all-encompassing across every service, entity, controller, hub, and workflow.
- **Mandatory for Existing Features & Bug Fixes**: Any existing bug or feature being worked on, refactored, or enhanced that lacks any of these tests **MUST** have the missing tests implemented as part of that task. No code changes may be completed without complete test coverage.
- **Zero Regressions & All Tests Must Pass**: Any change—whether a new feature, enhancement, refactor, or bug fix—requires **ALL** tests across the entire suite (including all pre-existing tests) to pass. Introducing regressions or breaking existing tests is strictly prohibited.
- **Core Tiers Required**: All features require both **Unit Tests** and **Integration Tests**. The backend must have:
  1. **Unit Tests (xUnit)**:
     - Thorough isolation testing of domain models, value objects, math calculations, business rules, algorithms, and state machines.
     - Complete edge-case coverage: boundary values, division by zero, null safety, concurrency, and invalid inputs.
  2. **Behavior-Driven Development (BDD) Tests (Reqnroll / Gherkin)**:
     - Human-readable living documentation in `tests/VenueAxe.Bdd/Features/*.feature`.
     - Strongly typed step definitions in `tests/VenueAxe.Bdd/StepDefinitions/` validating user journeys, acceptance criteria, and domain rules.
  3. **Integration Tests**:
     - Database persistence and transaction rollback safety across repositories and Unit of Work.
     - Multi-tenant data isolation: verifying EF Core global query filters prevent cross-tenant data leakage.
     - Real-time SignalR telemetry hub testing: client group management, broadcast dispatching, and disconnection handling.
  4. **End-to-End (E2E) Tests**:
     - Complete API execution pathways validating controllers, middleware, HttpOnly cookie authentication (`VenueAxe.Auth`), authorization policies, and RFC 7807 `ProblemDetails` error reporting.

### 4.2 Mandatory Feature Documentation (`docs/feature-documentation/[featureName]`)
Every backend feature must be documented in `docs/feature-documentation/[featureName]`.
- Documentation must describe feature behavior, domain models, application services, API contracts, security rules, and testing coverage.
- If working on an existing feature or bug that lacks documentation in `docs/feature-documentation/[featureName]`, it must be created or updated as part of the task.

### 4.3 Automated Quality Gate
Before any backend change is accepted, ALL test suites (including all pre-existing tests) must execute and pass with 0 errors and 0 warnings:
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
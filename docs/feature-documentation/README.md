# VenueAxe Feature Documentation

This directory contains executable feature documentation for all VenueAxe platform features.

## Mandate & Standard
Per the VenueAxe Engineering Guidelines:
- **Every feature** in the platform must have corresponding documentation in `docs/feature-documentation/[featureName]/` (or `docs/feature-documentation/[featureName].md`).
- Any new feature being developed, or any existing feature or bug fix being worked on that lacks documentation, **must** have its documentation created or brought up to date before completion.

## Documentation Structure Template
Each feature document should contain:
1. **Overview & Business Purpose**: Clear description of what the feature does, target roles (Owner, GM, Lane Master, Thrower), and operational goals.
2. **Technical Architecture & Data Model**: Entities, EF Core mappings, PostgreSQL JSONB schemas, application services, and frontend Svelte 5 components.
3. **API & Real-Time Telemetry**: Controllers, endpoints, request/response DTOs, and SignalR hub events/groups.
4. **Testing Matrix**:
   - **Backend**:
     - Unit Tests (`tests/VenueAxe.Tests/Unit/`)
     - BDD Living Specifications (`tests/VenueAxe.Bdd/Features/*.feature` and `StepDefinitions/`)
     - Integration Tests (`tests/VenueAxe.Tests/Functional/` or integration suites)
     - End-to-End (E2E) Tests
   - **Frontend**:
     - Component Tests (`src/frontend/src/tests/`)
     - Integration Tests
     - End-to-End (E2E) Tests
     - Visual Regression Tests (Overhead TV 1920x1080, Tablet 1024x768 / 1280x800, Mobile 390x844, Admin Desktop 1920x1080)
5. **Manual Verification & Viewport Guide**: Step-by-step procedures for manual testing in Google Chrome across all target form factors.

## Feature Index
- [Structured JSON Logging](file:///d:/projects/VenueAxe/docs/feature-documentation/structured-json-logging/README.md)
- [Authentication & Multi-Tenancy](file:///d:/projects/VenueAxe/docs/feature-documentation/authentication-and-multi-tenancy/README.md)
- [Booking Engine](file:///d:/projects/VenueAxe/docs/feature-documentation/booking-engine/README.md)
- [Digital Waivers](file:///d:/projects/VenueAxe/docs/feature-documentation/digital-waivers/README.md)
- [Email Notifications](file:///d:/projects/VenueAxe/docs/feature-documentation/email-notifications/README.md)
- [Lane Games & Scoring](file:///d:/projects/VenueAxe/docs/feature-documentation/lane-games-and-scoring/README.md)
- [Lane Management](file:///d:/projects/VenueAxe/docs/feature-documentation/lane-management/README.md)
- [Real-Time SignalR Telemetry](file:///d:/projects/VenueAxe/docs/feature-documentation/real-time-signalr-telemetry/README.md)
- [Reporting & Analytics](file:///d:/projects/VenueAxe/docs/feature-documentation/reporting-and-analytics/README.md)
- [Session Lifecycle](file:///d:/projects/VenueAxe/docs/feature-documentation/session-lifecycle/README.md)
- [Venue Management](file:///d:/projects/VenueAxe/docs/feature-documentation/venue-management/README.md)


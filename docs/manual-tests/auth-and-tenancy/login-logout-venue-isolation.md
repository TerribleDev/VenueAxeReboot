# Manual Test: Login / Logout Venue Isolation & Session Purge

**Feature**: Authentication & Multi-Tenancy  
**Test ID**: `AUTH-001`  
**Target URL**: `http://localhost:5173/admin/login`  
**Related Bug**: Cross-user venue cache bleed upon login switch  

---

## 1. Purpose & Objectives
Validate that logging out of an operator account and logging into another account belonging to a different tenant completely purges all local storage venue state, resets cached venue identifiers, and prevents any unauthorized cross-tenant venue bleed.

---

## 2. Prerequisites
1. Backend API running on `http://localhost:5280`.
2. Frontend dev server running on `http://localhost:5173`.
3. Seeded operator credentials:
   - **User A (Tenant 1)**: `owner@venueaxe.com` / `VenueAxeAdmin2026!#$` (Venues: Apex Axe House - Downtown, Apex Axe House - Uptown Club)
   - **User B (Tenant 2)**: `owner@valhallaaxe.com` / `password123` (Venue: Valhalla Axe Lounge)

---

## 3. Step-by-Step Test Procedure
1. Open Google Chrome in an Incognito / Fresh window.
2. Open Chrome DevTools (`F12`), switch to the **Console** tab, and preserve log (`Preserve log` checkbox checked).
3. Navigate to `http://localhost:5173/admin/login`.
4. Fill in:
   - Email: `owner@venueaxe.com`
   - Password: `VenueAxeAdmin2026!#$`
5. Click **Sign In to Arena Console**.
6. Verify redirect to `/admin/lanes`. Inspect the venue selector dropdown in the top header.
7. Click the venue selector dropdown and switch to **Apex Axe House - Uptown Club**.
8. Inspect Chrome DevTools **Application** $\rightarrow$ **Local Storage** (`http://localhost:5173`).
9. In the top navigation header or user avatar menu, click **Sign Out**.
10. Verify redirect back to `http://localhost:5173/admin/login`.
11. In the login form, fill in User B credentials:
    - Email: `owner@valhallaaxe.com`
    - Password: `password123`
12. Click **Sign In to Arena Console**.
13. Observe the active venue displayed in the top header and on the arena lane cards.

---

## 4. What to Look For
- Look at the top venue dropdown immediately upon User B login.
- Look at the browser Local Storage keys: check that `venueaxe_selected_venue_id_<userId>` is scoped per user and that no un-namespaced global keys persist stale venue IDs.
- Look at the DevTools Console to confirm zero errors or warnings.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] User A logs in and successfully switches between "Apex Axe House - Downtown" and "Apex Axe House - Uptown Club".
- [ ] Clicking **Sign Out** clears the auth session and resets in-memory `venueState`.
- [ ] User B lands on the dashboard displaying **Valhalla Axe Lounge** (Venue ID `01a07d11-3f20-719c-af45-53b420ef07af`).
- [ ] **Assertion**: The venue selector dropdown for User B ONLY contains venues belonging to Tenant 2 (Valhalla Axe Lounge).
- [ ] **Assertion**: "Apex Axe House - Uptown Club" or "Apex Axe House - Downtown" is **NEVER** visible or selectable for User B.
- [ ] **Assertion**: Zero console exceptions (`0 errors, 0 unhandled promise rejections`).

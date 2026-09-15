# Manual Test: Customer Booking Wizard & Lane Manager Real-Time Visibility

**Feature**: Guest Booking & Lane Manager Operations  
**Test ID**: `BOOK-001`  
**Target URLs**:  
- `http://localhost:5173/book/downtown`  
- `http://localhost:5173/admin/lanes`  
- `http://localhost:5173/admin/bookings`  

---

## 1. Purpose & Objectives
Validate the customer guest booking flow (date selection, time slot picking, group size, pricing breakdown, and instant confirmation) and verify that completed reservations appear immediately on the assigned lane in `/admin/lanes` with either a green **CURRENT** booking card or blue **NEXT BOOKING TODAY** card, complete with guest name, party size, booking reference, and deposit balance indicator.

---

## 2. Prerequisites
1. Backend and Frontend services active.
2. Operator logged in and viewing `/admin/lanes` under `Apex Axe House - Downtown`.
3. At least 1 lane active and available today.

---

## 3. Step-by-Step Test Procedure
1. Open a new Chrome tab at `http://localhost:5173/book/downtown`.
2. Inspect the booking wizard:
   - Select Experience: `Standard Target Throwing` (60 min).
   - Select Party Size: `4 Throwers`.
   - Select Today's Date.
   - Pick the nearest upcoming time slot (e.g., 7:00 PM).
3. Observe the live pricing summary:
   - Base price ($35/thrower) $\times$ 4 throwers = $140.00.
4. Fill Guest Details:
   - First Name: `Alice`
   - Last Name: `Wonder`
   - Email: `alice.wonder@example.com`
   - Phone: `(555) 012-3456`
   - Marketing Opt-in checkbox: Checked.
5. Click **Complete Reservation**.
6. Note the generated Booking Reference (e.g., `VA-78214`).
7. Switch to the `/admin/lanes` tab:
   - Check the assigned lane card (e.g., Lane 01).
   - If the booking is currently active (within 15 minutes of start time to end time): observe the green **CURRENT** banner with `🚀 Start Match with Alice Wonder`.
   - If the booking is later today: observe the blue **NEXT BOOKING TODAY** card displaying `Alice Wonder • 4 throwers • #VA-78214`.
8. Switch to `/admin/bookings`:
   - Verify `Alice Wonder` appears in the reservations table.

---

## 4. What to Look For
- Visual check on `/admin/lanes`: Lane cards show high-contrast booking badges (Current = emerald green border, Next = blue border).
- Deposit Balance Alert: If balance due $> 0$, an amber `🟡 DEPOSIT PAID — $XX.XX DUE` alert pill is visible.
- Quick Launch Button: Clicking `🚀 Start Match with Alice Wonder` automatically opens the session launcher pre-populated with Alice's name, party size, and duration.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Booking creation generates a unique booking reference and allocates lanes contiguously without conflict.
- [ ] In `/admin/lanes`, the assigned lane displays the booking under either `currentBooking` or `nextBookingToday`.
- [ ] Quick-start session button successfully pre-populates the player roster and match title from the reservation.
- [ ] Zero console errors on `/book/[slug]` and `/admin/lanes`.

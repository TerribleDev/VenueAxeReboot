# Manual Test: Pricing Engine, Tiered Discounts & Balance Collection

**Feature**: Dynamic Pricing, Volume Discounts & Financial Check-In  
**Test ID**: `PRICE-001`  
**Target URLs**:  
- `http://localhost:5173/book/downtown`  
- `http://localhost:5173/admin/lanes`  
- `http://localhost:5173/admin/bookings`  

---

## 1. Purpose & Objectives
Validate dynamic pricing rules, promotional coupon codes, volume group tiered discounts, deposit calculations (50% deposit vs full payment), and on-site remaining balance collection from the Lane Manager console.

---

## 2. Prerequisites
1. Booking configuration active with:
   - Base Price: $35.00/thrower (Standard)
   - Peak Price: $45.00/thrower (Friday night & Saturday)
   - Volume Tier: Groups of $\ge 8$ throwers receive 10% discount.
   - Promo Coupon: `FIRSTRESPONDER` (15% discount).
2. Square sandbox payment integration or verified manual collection.

---

## 3. Step-by-Step Test Procedure

### Part A: Pricing Engine & Volume Tier
1. Navigate to `http://localhost:5173/book/downtown`.
2. Select Party Size: `4 Throwers`:
   - Verify Total Price = $140.00 ($35 $\times$ 4).
3. Increase Party Size to `8 Throwers`:
   - Verify Volume Discount applies: Subtotal $280.00 - 10% ($28.00) = Total $252.00.

### Part B: Promo Code Application
4. In the Promo Code input, enter: `FIRSTRESPONDER`.
5. Click **Apply Code**:
   - Verify 15% discount calculates dynamically and updates the net total due.

### Part C: Deposit Payment & Balance Due Alert
6. Select Deposit Mode: `50% Deposit Due Today`.
   - Verify Deposit Amount = $107.10, Remaining Balance = $107.10.
7. Complete booking reservation.
8. Switch to `/admin/lanes`:
   - Locate the assigned lane card for today.
   - Observe the deposit banner: `🟡 DEPOSIT PAID — $107.10 DUE`.
9. Click **💳 Collect Balance**:
   - In the payment modal, select payment method (`Card Reader / Square Terminal` or `Cash`).
   - Confirm balance payment.
10. Observe lane card and `/admin/bookings`:
    - Banner updates to green `✅ PAID IN FULL`.

---

## 4. What to Look For
- Pricing stepper dynamically recalculates subtotals, tax, and discounts without screen flicker.
- Invalid promo codes display red feedback without resetting selected slot.
- Balance due collection updates the booking financial state in real time.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Tiered volume discounts trigger automatically at configured group size threshold.
- [ ] Promotional coupon codes validate and deduct percentage/fixed amounts accurately.
- [ ] Deposit amounts split evenly and accurately record `PaymentStatus = DepositPaid`.
- [ ] Balance collection clears outstanding amounts, records payment timestamp, and transitions status to `PaidInFull`.
- [ ] Zero console errors throughout pricing and checkout flows.

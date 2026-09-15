# Manual Test: 6-Digit Tablet & TV Terminal Pairing & Space Verification

**Feature**: Lane Management & Hardware Telemetry  
**Test ID**: `LANE-001`  
**Target URLs**:  
- `http://localhost:5173/admin/lanes`  
- `http://localhost:5173/tablet`  
- `http://localhost:5173/screen`  

---

## 1. Purpose & Objectives
Validate that Tablet and TV display pairing codes operate strictly on 6-digit numeric sequences (e.g., `100001`, `200001`), that all frontend pairing input cards have generous width, comfortable padding, and zero character truncation or text wrapping, and that hardware pairing handshakes complete successfully.

---

## 2. Prerequisites
1. Backend and Frontend running on ports 5280 and 5173.
2. Authenticated operator logged into `/admin/lanes` under `Apex Axe House - Downtown`.
3. Lane 01 initialized with pairing codes:
   - Tablet PIN: `100001` (6 digits)
   - TV PIN: `200001` (6 digits)

---

## 3. Step-by-Step Test Procedure
1. Navigate to `http://localhost:5173/admin/lanes`.
2. Inspect the Lane 01 card:
   - Locate the **Tablet PIN** and **TV PIN** pairing badges.
   - Verify the numbers are exactly 6 digits.
   - Verify there is ample horizontal space (`min-width: 105px`) with clean monospace alignment.
3. Open a new Chrome tab at `http://localhost:5173/tablet`:
   - Inspect the pairing input card. Verify `maxlength="6"` and comfortable text input box (`max-width: 320px`).
   - Enter `100001`.
   - Click **Connect to Lane Terminal**.
   - Verify transition to the Tablet Console / Idle screen displaying `Lane 01`.
4. Open a new Chrome tab at `http://localhost:5173/screen`:
   - Inspect the Overhead TV pairing card. Verify `maxlength="6"` and wide input box (`max-width: 340px`).
   - Enter `200001`.
   - Click **Connect Overhead TV Display**.
   - Verify transition to the TV Broadcast / Idle screen displaying `WELCOME TO Lane 01`.
5. Return to `http://localhost:5173/admin/lanes`:
   - Click the gear/settings button or **Regenerate PINs** on Lane 01.
   - Confirm new 6-digit PINs are generated (e.g., `100007`, `200007`).

---

## 4. What to Look For
- Visual check on `/admin/lanes`: 6-digit PIN numbers do not wrap into 2 lines, overlap neighboring badges, or truncate with ellipsis.
- Visual check on `/tablet`: The 6-digit numeric input accommodates all characters with spacing (`letter-spacing: 0.15em`) and clear focus rings.
- Visual check on `/screen`: TV Display PIN input allows 6 large digits without horizontal clipping at 1080p/4K resolution.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Both Tablet and TV PINs are strictly 6 numeric digits.
- [ ] Tablet pairing succeeds with 6-digit PIN and stores device token in browser localStorage.
- [ ] TV display pairing succeeds with 6-digit PIN and joins SignalR broadcast group.
- [ ] Regenerating PINs produces fresh 6-digit codes and updates the database immediately.
- [ ] Zero console errors on `/admin/lanes`, `/tablet`, and `/screen`.

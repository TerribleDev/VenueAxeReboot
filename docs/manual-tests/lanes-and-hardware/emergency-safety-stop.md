# Manual Test: Emergency Safety Freeze & Lockout Synchronization

**Feature**: Venue Safety & Emergency Arena Controls  
**Test ID**: `SAFE-001`  
**Target URLs**:  
- `http://localhost:5173/admin/lanes`  
- `http://localhost:5173/tablet`  
- `http://localhost:5173/screen`  

---

## 1. Purpose & Objectives
Validate the immediate Emergency Safety Stop protocol:
1. When staff triggers **Safety Stop** on a lane card in `/admin/lanes`, the lane status immediately transitions to `Maintenance` / `SafetyStop`.
2. Both the paired In-Lane Tablet and Overhead TV Display immediately transition to a high-contrast red emergency warning screen, freezing the session timer and locking all throw inputs.
3. When the Lane Coach resolves the issue and clicks **Clear Safety Stop**, both screens instantaneously restore active gameplay without losing match state or player scores.

---

## 2. Prerequisites
1. Lane 01 paired on `/tablet` and `/screen` with an active match in progress.
2. Operator viewing `/admin/lanes`.

---

## 3. Step-by-Step Test Procedure
1. In Window 1 (`/admin/lanes`), locate **Lane 01**.
2. Click the red **🛑 Safety Stop** button.
3. Observe Window 2 (`/tablet`):
   - Confirm screen immediately turns bright red with high-contrast warning: `🚨 EMERGENCY SAFETY STOP - CEASE THROWING IMMEDIATELY`.
   - Confirm target board and buttons are completely locked.
4. Observe Window 3 (`/screen`):
   - Confirm overhead TV displays full-screen emergency flash alert visible from 30+ feet away.
5. In Window 1 (`/admin/lanes`):
   - Notice the button changed to: **✅ Clear Safety Stop**.
   - Click **✅ Clear Safety Stop**.
6. Observe Window 2 (`/tablet`) and Window 3 (`/screen`):
   - Confirm the emergency freeze dismisses instantaneously ($< 50\text{ms}$).
   - Confirm the interactive target, current round, throw scores, and countdown timer resume seamlessly.

---

## 4. What to Look For
- Visual check: Flash warning on Overhead TV is unmissable and legible across the venue floor.
- Tablet input locking: Touch interactions on the SVG target or control buttons produce no throws during safety lockout.
- State integrity: Clearing safety stop preserves all player scores, current round number, and remaining session time.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Clicking Safety Stop dispatches `POST /api/lanes/operations/{id}/safety-stop`.
- [ ] SignalR broadcasts `OnSafetyStopTriggered` to paired clients.
- [ ] Both Tablet and TV displays lock and display emergency banners within 50ms.
- [ ] Clearing Safety Stop restores lane status to `Active` and dispatches `OnSafetyStopCleared`.
- [ ] Zero state corruption or lost throw points upon resume.
- [ ] Zero console errors on all three surfaces.

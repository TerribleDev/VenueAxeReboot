# Manual Test: WATL Standard 10-Round Match, Target Math, Clutch & Undo

**Feature**: Target Scoring, WATL Math & Real-Time Gameplay  
**Test ID**: `GAME-001`  
**Target URLs**:  
- `http://localhost:5173/tablet`  
- `http://localhost:5173/screen`  

---

## 1. Purpose & Objectives
Validate the official World Axe Throwing League (WATL) 10-round match engine:
1. Target collision math across all 5 concentric rings (1 through 5 points) and Bullseye (6 points).
2. Clutch calls on Round 5 and Round 10 (called = 7 pts, uncalled = 0 pts, maximum 2 called clutch attempts per match).
3. Live player turn rotation (Thrower 1 $\rightarrow$ Thrower 2).
4. Sub-50ms synchronized throw broadcasting between Tablet and TV display.
5. Throw Undo functionality restoring previous turn and point state.
6. 10th round completion, winner declaration, and podium celebration.

---

## 2. Prerequisites
1. Side-by-side Chrome windows:
   - Window 1: `/tablet` paired to Lane 01
   - Window 2: `/screen` paired to Lane 01
2. Active WATL Standard match launched with 2 throwers (`Marcus`, `Sarah`).

---

## 3. Step-by-Step Test Procedure
1. Observe the interactive SVG Target on Window 1 (`/tablet`):
   - Active thrower indicated: `Marcus` (Throw 1 / Round 1).
2. Tap the Bullseye (center red circle, radius $\le 0.097$):
   - Confirm Marcus is awarded **6 points**.
   - Confirm active turn immediately advances to `Sarah`.
   - In Window 2 (`/screen`), confirm golden hit ripple, audio visualizer, and 6 points on the overhead scoreboard.
3. Tap Ring 4 for Sarah:
   - Confirm Sarah is awarded **4 points**.
   - Match advances to Round 2.
4. Advance through Round 4 using varied ring hits (1, 2, 3, 5 points).
5. On Round 5, before throwing:
   - In `/tablet`, tap **CALL CLUTCH (7 PTS)**.
   - Verify cyan pulsing clutch banner appears on BOTH Tablet and TV displays.
   - Tap the left or right clutch dot.
   - Confirm **7 points** awarded.
6. Test Uncalled Clutch:
   - In Round 6 (without calling clutch), tap the clutch dot.
   - Confirm **0 points** awarded per official WATL regulations.
7. Test Throw Undo:
   - In `/tablet`, tap **↩ Undo Throw**.
   - Confirm the uncalled clutch throw is retracted, scores revert, and active turn returns to the previous thrower.
8. Complete Round 10:
   - Confirm match concludes automatically.
   - Verify podium celebration screen renders on `/screen` displaying winner rankings, total points, bullseye streaks, and throw averages.

---

## 4. What to Look For
- Target math precision: Tapping bullseye, rings, or clutch calculates points accurately with zero coordinate clipping.
- Dual-screen latency: Throws recorded on `/tablet` appear on `/screen` without perceptible lag ($< 50\text{ms}$).
- Undo state restoration: Clean rollback of throw sequence, round count, and player scores.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Bullseye awards 6 points. Ring 5 = 5 pts, Ring 4 = 4 pts, Ring 3 = 3 pts, Ring 2 = 2 pts, Ring 1 = 1 pt.
- [ ] Called Clutch awards 7 points; uncalled clutch awards 0 points.
- [ ] Clutch attempts are capped at 2 per player per match.
- [ ] Undo throw safely reverts match state and persists to database.
- [ ] Podium celebration triggers upon completion of Round 10.
- [ ] Zero console errors throughout the entire 10-round match.

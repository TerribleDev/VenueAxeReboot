# Manual Test: Arcade Game Engines (Countdown 603, Axe Blackjack 21 & Tic-Tac-Toe)

**Feature**: Pluggable Game Engines & Arcade Modes  
**Test ID**: `GAME-002`  
**Target URLs**:  
- `http://localhost:5173/tablet`  
- `http://localhost:5173/screen`  
- `http://localhost:5173/admin/lanes`  

---

## 1. Purpose & Objectives
Validate the non-standard arcade game engines:
1. **Countdown 603**: Starting score deduction (starting at 603 points), exact-zero victory checkout, and bust state handling (when a throw exceeds remaining points or leaves 1 point, score reverts to previous round).
2. **Axe Blackjack 21**: Playing card point accumulation, 21-hand cutoff, bust state transition (hand $> 21$), and high-hand resolution.
3. **Axe Tic-Tac-Toe**: 3x3 territory grid cell claiming, locked cells, and 3-in-a-row victory detection.

---

## 2. Prerequisites
1. Paired Tablet (`/tablet`) and TV Display (`/screen`).
2. Lane operator logged in at `/admin/lanes`.

---

## 3. Step-by-Step Test Procedure

### Part A: Countdown 603 Game
1. In `/admin/lanes`, launch a session with game: `Countdown 603` with 1 player (`Dave`).
2. Verify `/tablet` and `/screen` display starting score: `603`.
3. Throw Bullseye (6 pts): Confirm score drops to `597`.
4. Throw Killshot (8 pts): Confirm score drops to `589`.
5. Advance score until remaining is low (e.g., 4 points remaining).
6. Throw Bullseye (6 pts):
   - Confirm **BUST** condition triggers!
   - Score resets back to `4 points` and turn advances.
7. Throw Ring 4 (4 pts):
   - Confirm **EXACT ZERO CHECKOUT** achieved!
   - Victory celebration triggers immediately.

### Part B: Axe Blackjack 21
8. From `/tablet` or `/admin/lanes`, switch game mode to `Blackjack 21`.
9. Throw rings to accumulate card values.
10. Verify card evaluation:
    - When hand total $> 21$, confirm **BUST** alert triggers and score resets to 0.
    - When hand equals 21, confirm **BLACKJACK!** celebration fires.

### Part C: Axe Tic-Tac-Toe
11. Switch game mode to `Axe Tic-Tac-Toe` with 2 players (`Player X`, `Player O`).
12. Throw into top-left cell: confirm cell is claimed with player's symbol.
13. Attempt to throw into the same claimed cell: confirm cell is locked and cannot be overwritten.
14. Complete a horizontal, vertical, or diagonal 3-in-a-row line:
    - Confirm three-in-a-row victory detection fires immediately.

---

## 4. What to Look For
- UI score counters: Countdown 603 decrementing, Blackjack hand card graphics, Tic-Tac-Toe 3x3 interactive territory board.
- Visual animations: Bust shake animation in red, Blackjack celebration in gold, Tic-Tac-Toe winning line highlight.

---

## 5. Explicit Assertions & Pass Criteria
- [ ] Countdown 603 game decrements score from 603 and enforces exact-zero checkout.
- [ ] Scoring more than remaining points triggers a Bust and preserves prior score.
- [ ] Axe Blackjack awards card values, evaluates 21, and flags hand bust $> 21$.
- [ ] Axe Tic-Tac-Toe correctly locks claimed cells and detects 3-in-a-row victory.
- [ ] Zero console errors during game engine switches and match executions.

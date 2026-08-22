# Module 04: Lane Games & Interactive WATL Target Scoring Specification

## 1. Overview
The Lane Games system transforms static axe throwing into a modern, gamified sports and entertainment experience. Each lane is equipped with a **Tablet UI (In-Lane Thrower/Scorekeeper Console)** and a paired **Overhead TV Display (Above-Lane Monitor)**. 

The standout feature is the **Interactive WATL (World Axe Throwing League) Target Board**, where players and coaches simply tap the exact spot on the board where the axe landed to record scores, track accuracy heatmaps, and drive dynamic broadcast-style graphics on the overhead monitor.

---

## 2. World Axe Throwing League (WATL) Target Board Specification

### 2.1 Target Geometry & Point Values
The interactive target is rendered as an accurate SVG graphic adhering strictly to official WATL dimensions and rules:

```
                          +-----------------------------------+
                          | [C1]                         [C2] |  <-- Clutch Targets (7 pts)
                          |  (o)                         (o)  |      (Active only on call)
                          |                                   |
                          |            +---------+            |
                          |          /     1     \           |  <-- 1 Ring (Black: 1 pt)
                          |        /   +-----+     \          |
                          |       /   /   2   \     \         |  <-- 2 Ring (Blue: 2 pts)
                          |      |   /  +---+  \     |        |
                          |      |  |  /  3  \  |    |        |  <-- 3 Ring (Red: 3 pts)
                          |      |  | | +---+ | |    |        |
                          |      |  | | |(B)| | |    |        |  <-- Bullseye (Black Inner: 6 pts)
                          |      |  | | +---+ | |    |            & 4 Ring (Blue: 4 pts)
                          |      |  |  \  5  /  |    |            & 5 Ring (Red: 5 pts)
                          |      |   \  +---+  /     |        |
                          |       \   \       /     /         |
                          |        \   +-----+     /          |
                          |          \           /            |
                          |            +---------+            |
                          |                                   |
                          |           [Drop / Miss]           |  <-- 0 pts (Fault / Drop)
                          +-----------------------------------+
```

### 2.2 Point Scoring Rules
| Target Zone | Visual Appearance | Base Points | Rules & Validation |
| :--- | :--- | :--- | :--- |
| **Bullseye (Inner Core)** | Solid Black Circle (3.5" diameter) | **6 points** | Default highest regular target zone. |
| **5th Ring** | Red Ring (1.5" width) | **5 points** | Inner red zone. |
| **4th Ring** | Blue Ring (1.5" width) | **4 points** | Inner blue zone. |
| **3rd Ring** | Red Ring (1.5" width) | **3 points** | Outer red zone. |
| **2nd Ring** | Blue Ring (1.5" width) | **2 points** | Outer blue zone. |
| **1st Ring** | Black Outer Ring (1.5" width) | **1 point** | Perimeter black zone. |
| **Clutch (Left / Right)** | High-Contrast Green/Blue Dots (2.625" diameter) | **7 points** | **Crucial Rule**: In standard WATL, Clutch must be intentionally "called" before throwing (traditionally on Throws 5 and 10). If hit without being called, it scores **0 points**. If called but missed, it scores wherever it lands (or 0 if outside target). |
| **Board / Drop / Miss** | Wood boundary outside 1st ring or dropped axe | **0 points** | Axe fails to stick, crosses line fault, or hits wood outside rings. |

### 2.3 Interactive Touch Hit-Detection & Line Touching
- **Coordinate Collision**: The tablet captures normalized touch coordinates `(x, y)` relative to the SVG target center `(0, 0)`.
- **Radial Distance Math**:
  $$r = \sqrt{x^2 + y^2}$$
  - $0 \le r \le R_{bull} \implies 6\text{ pts}$
  - $R_{bull} < r \le R_5 \implies 5\text{ pts}$
  - $R_5 < r \le R_4 \implies 4\text{ pts}$
  - $R_4 < r \le R_3 \implies 3\text{ pts}$
  - $R_3 < r \le R_2 \implies 2\text{ pts}$
  - $R_2 < r \le R_1 \implies 1\text{ pt}$
  - Left Clutch circle: $(x - X_{cL})^2 + (y - Y_{cL})^2 \le R_{clutch}^2$
  - Right Clutch circle: $(x - X_{cR})^2 + (y - Y_{cR})^2 \le R_{clutch}^2$
- **"Line-Breaking" Rule Support**: In axe throwing, if an axe blade touches the border line between two rings, the thrower receives the higher point value.
  - The UI provides a subtle "Line Touch / Higher Value" toggle when a tap is within a calibrated boundary threshold.

---

## 3. Thrower Tablet UI (Scorekeeper Console)

### 3.1 Design Philosophy & Ergonomics
- **Visual Aesthetic**: Dark, high-contrast palette (Carbon Black `#0f1117`, Slate Grey `#1e2230`, Electric Amber `#f59e0b`, Neon Cyan `#06b6d4`, Crimson `#ef4444`).
- **Touch Ergonomics**: All actionable buttons have a minimum touch area of 64x64px.
- **Physical Context**: High-contrast typography readable under varying ambient venue lighting (glow-in-the-dark / UV blacklight throwing modes).

### 3.2 Tablet Screen Modes & Views
1. **Lobby & Setup View**:
   - Roster Management: Add throwers, re-order turn sequence (drag-and-drop), pick avatar colors, or split into Red vs. Blue teams.
   - Game Selector Carousel: Browse available games with duration estimates and difficulty badges.
2. **Active Scoring View**:
   - **Top Bar**: Active Player Banner (Large Avatar + Name + Current Throws: `Throw 4 of 10`), Score breakdown, Match Rank.
   - **Center Screen**: Large interactive WATL Target Board SVG.
   - **Quick Action Dock**:
     - 🎯 **Direct Number Buttons**: Instant manual taps for `[6] [5] [4] [3] [2] [1] [0 (Drop/Miss)] [Fault]`.
     - ⚡ **CALL CLUTCH Button**: Arms left/right clutch with visual pulsing glow.
     - ↩️ **Undo Last Throw**: Reverts previous score and restores turn.
     - ⏭️ **Skip Turn / Next Player**: Handles player absent/sitting out.
3. **Post-Match Summary & Analytics**:
   - Podium celebration (1st, 2nd, 3rd place).
   - Accuracy Radar & Throw Heatmap (shows scatter plot of every axe thrown during the match).
   - "Rematch", "New Game", or "Return to Menu".

---

## 4. Overhead Lane Display UI (Monitor / TV Screen)

### 4.1 Visual Broadcast Layout (16:9 1080p/4K TV)
The TV screen is designed for high readability from 15 to 25 feet away:
```
+-----------------------------------------------------------------------------------+
|  LANE 04        SESSION TIME: 42:15        WATL MATCH - ROUND 4 OF 10             |
+-----------------------------------------------------------------------------------+
|                                 |                                                 |
|        CURRENT THROWER          |               LIVE LEADERBOARD                  |
|                                 |                                                 |
|    [AVATAR]  SARAH "DECKER"     |  RANK  PLAYER       THROWS      TOTAL   STREAK  |
|                                 |  ---------------------------------------------  |
|    SCORE: 18 pts (+6 Bullseye)  |   1    Sarah         4/10        24      🔥 3x  |
|    STREAK: 🔥🔥🔥 3 in a row    |   2    Marcus        4/10        21      🎯     |
|                                 |   3    Alex          3/10        15             |
|    [>> ON DECK: Marcus <<]      |   4    Elena         3/10        12             |
|                                 |                                                 |
+---------------------------------+-------------------------------------------------+
|                                                                                   |
|                      TARGET HIT VISUALIZER & ROUND TRAIL                          |
|         [ Real-time SVG Target rendering exact hit coordinates and icons ]        |
|                                                                                   |
+-----------------------------------------------------------------------------------+
```

### 4.2 Dynamic Screen Events & Broadcast Animations
- **Bullseye Celebration**: Dynamic screen flash with golden particle burst and sound cue (if TV audio enabled).
- **Clutch Call Alert**: Screen border pulses in electric cyan with text: `⚡ CLUTCH CALLED - 7 POINTS ON THE LINE ⚡`.
- **Clutch Nailed!**: Explosive full-screen animation with siren flair and high-score badge.
- **Lead Change Alert**: Mini-banner: `👑 Sarah takes 1st place!`.
- **Idle Attract Mode**:
  - Venue logo in ambient motion.
  - Live venue-wide top scores of the day.
  - QR Code: "Scan to view live digital drink menu" or "Scan to sign safety waiver".

---

## 5. Pluggable Game Engine Architecture

The game subsystem runs on a clean state machine in the backend and frontend:

### 5.1 Game Engine State Model (TypeScript / C#)
```csharp
public interface IGameEngine
{
    string GameTypeId { get; }
    string DisplayName { get; }
    string Description { get; }
    
    GameState Initialize(GameConfig config, List<Player> players);
    ThrowResult ProcessThrow(GameState state, ThrowInput input);
    bool IsMatchOver(GameState state);
    MatchSummary GenerateSummary(GameState state);
}
```

### 5.2 Standard Game Catalog
1. **WATL Standard Match**:
   - Official 10-throw match format.
   - Throws 1-4: Regular target rings.
   - Throw 5: Clutch option allowed.
   - Throws 6-9: Regular target rings.
   - Throw 10: Clutch option allowed.
   - Sudden Death Tiebreakers supported.
2. **Countdown (501 / 301 Rules)**:
   - Players start at 101, 301, or 501 points.
   - Points scored subtract from total.
   - First player to reach exactly zero (with or without bust rule) wins.
3. **Around the World**:
   - Objective: Hit rings sequentially: Ring 1 -> Ring 2 -> Ring 3 -> Ring 4 -> Ring 5 -> Bullseye (6) -> Clutch (7).
   - Only hitting the currently active ring advances the player's quest marker.
4. **Axe Tic-Tac-Toe**:
   - 3x3 overlay projected across the target board.
   - Hitting a quadrant claims it for Team Red or Team Blue.
   - First team to connect 3 in a row wins.
5. **Blackjack (21)**:
   - Goal is to hit exactly 21 points in as few throws as possible.
   - Going over 21 is a "Bust" (resets back to previous turn score).
6. **Clutch Hunter**:
   - High-skill challenge: Only Clutch hits (7 pts) and Bullseyes (6 pts) score. All other rings are 0 points.

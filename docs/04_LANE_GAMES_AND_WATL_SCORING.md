# Module 04: Lane Games & Interactive WATL Target Scoring Specification

## 1. Overview
The Lane Games system transforms static axe throwing into a modern, gamified sports and entertainment operating system. Each lane is equipped with a **Tablet UI (In-Lane Thrower/Scorekeeper Console)** and a paired **Overhead TV Display (Above-Lane Broadcast Monitor)**. 

The centerpiece is the **Interactive WATL (World Axe Throwing League) Target Board**, where players and coaches tap the exact spot on the board where the axe landed to record scores, track accuracy heatmaps, and drive dynamic broadcast-style graphics and celebrations on the overhead monitor.

---

## 2. World Axe Throwing League (WATL) Target Board Specification

### 2.1 Target Geometry & Point Values
The interactive target is rendered as an accurate SVG graphic adhering strictly to official WATL dimensions and rules.

> **League Scoring Mandate**: 
> - **Zero Clutch in League Play**: Clutch scoring is removed from league play.
> - **Zero IATF Support**: IATF rules and 3-ring formats are not supported in league play.
> - **WATL Point Scale**: 6 (Bullseye), 5, 4, 3, 2, 1, 0, Drop, Miss, Fault.
> - **2 Anytime Kills**: Each thrower has a quota of **2 Killshots per 10-throw match** that can be called at **any time** (any round). Calling a Killshot awards **8 points** if successfully hit, and **0 points** if missed or uncalled.

```
                          +-----------------------------------+
                          | [K1]                         [K2] |  <-- Killshots (8 pts)
                          |  (o)                         (o)  |      (2 per player, callable ANYTIME)
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
| **Killshot (Left / Right)** | High-Contrast Cyan/White Ring Dots (1.5" diameter) | **8 points** | **2 Anytime Kills Rule**: Each thrower has a maximum of **2 Killshot calls per 10-throw match**. Can be called at **any time** (not restricted to round 5 or 10). If hit when called $\implies$ **8 points**. If hit without being called $\implies$ **0 points**. If called but missed $\implies$ **0 points**. |
| **Bullseye (Inner Core)** | Solid Black Circle (3.5" diameter) | **6 points** | Inner target core. |
| **5th Ring** | Red Ring (1.5" width) | **5 points** | Inner red zone. |
| **4th Ring** | Blue Ring (1.5" width) | **4 points** | Inner blue zone. |
| **3rd Ring** | Red Ring (1.5" width) | **3 points** | Outer red zone. |
| **2nd Ring** | Blue Ring (1.5" width) | **2 points** | Outer blue zone. |
| **1st Ring** | Black Outer Ring (1.5" width) | **1 point** | Perimeter black zone. |
| **Drop** | Axe fell out of target wood before scoring | **0 points** | Axe hit target but failed to stick. Recorded as Drop in scorebook. |
| **Miss** | Wood outside outer black ring | **0 points** | Stood or missed wood perimeter entirely. Recorded as Miss. |
| **Fault** | Foot crossed the 12-foot fault line | **0 points** | Line violation disqualifying the throw. |

### 2.3 Interactive Touch Hit-Detection & Line Touching
- **Coordinate Collision**: The tablet captures normalized touch coordinates `(x, y)` relative to the SVG target center `(0, 0)`.
- **Radial Distance Math**:
  $$r = \sqrt{x^2 + y^2}$$
  - $0 \le r \le R_{bull} (0.097) \implies 6\text{ pts (Bullseye)}$
  - $R_{bull} < r \le R_5 (0.180) \implies 5\text{ pts}$
  - $R_5 < r \le R_4 (0.264) \implies 4\text{ pts}$
  - $R_4 < r \le R_3 (0.347) \implies 3\text{ pts}$
  - $R_3 < r \le R_2 (0.431) \implies 2\text{ pts}$
  - $R_2 < r \le R_1 (0.514) \implies 1\text{ pt}$
  - Left Killshot circle: $(x - (-0.380))^2 + (y - 0.460)^2 \le (0.045)^2$:
    - If `isKillCalled = true` $\implies 8\text{ pts}$ (decrements remaining kills quota).
    - If `isKillCalled = false` $\implies 0\text{ pts}$.
  - Right Killshot circle: $(x - 0.380)^2 + (y - 0.460)^2 \le (0.045)^2$:
    - If `isKillCalled = true` $\implies 8\text{ pts}$ (decrements remaining kills quota).
    - If `isKillCalled = false` $\implies 0\text{ pts}$.
- **"Line-Breaking" Rule Support**: In official axe throwing, if an axe blade touches the border line between two rings, the thrower receives the higher point value.
  - The UI provides an interactive "Line-Breaking Assist" modal that presents the adjacent point options (e.g. 5 vs 6) whenever a hit occurs within 1.5% radial proximity of a boundary line.

---

## 3. Thrower Tablet UI (Scorekeeper Console)

### 3.1 Design Philosophy & Ergonomics
- **Visual Aesthetic**: Dark arena palette (Carbon Black `#0a0d14`, Slate `#11151f`, Gold `#f59e0b`, Neon Cyan `#06b6d4`, Crimson `#ef4444`).
- **Touch Ergonomics**: Minimum touch area of 60x60px for all quick buttons.
- **Physical Context**: High-contrast typography readable under ambient venue lighting and UV blacklight modes.

### 3.2 Tablet Screen Modes & Views
1. **Lobby & Setup View**:
   - Roster Management: Add throwers, substitute throwers during an active session, re-order turn sequence.
   - Game Selector: WATL Standard, Killshot Hunter, Around The World, Axe Tic-Tac-Toe, Blackjack 21, Countdown 301/501.
2. **Active Scoring View**:
   - **Top Bar**: Active Player Banner (`Throw X of 10`), Score breakdown, Match Rank.
   - **Center Screen**: Large interactive WATL Target Board SVG.
   - **Quick Touch Keypad**:
     - 🎯 **Numeric Buttons**: Direct inputs for `[6 Bull] [5] [4] [3] [2] [1] [0] [Drop] [Miss] [Fault]`.
     - ⚡ **CALL KILLSHOT Button**: Displays remaining kills (`(2 left)`, `(1 left)`, `(0 left)`). Disables when quota is exhausted. Arms target with glowing cyan killshot indicators.
     - ↩️ **Undo Last Throw**: Reverts previous throw score, restores turn sequence, and accurately restores the player's killshot quota if the undone throw was a killshot.
     - 🔄 **Rematch & Substitute**: Instant match restart with identical roster or live substitution of a player.
3. **Post-Match Summary & Analytics**:
   - Dynamic 3D/Gold Podium celebration (1st, 2nd, 3rd place).
   - Accuracy Radar & Throw Heatmap showing all throws.

---

## 4. Overhead Lane Display UI (Monitor / TV Screen)

### 4.1 Visual Broadcast Layout (16:9 1080p/4K TV)
```
+-----------------------------------------------------------------------------------+
|  LANE 01        SESSION TIME: 42:15        WATL MATCH - THROW 4 OF 10             |
+-----------------------------------------------------------------------------------+
|                                 |                                                 |
|        CURRENT THROWER          |               LIVE LEADERBOARD                  |
|                                 |                                                 |
|    [AVATAR]  SARAH "DECKER"     |  RANK  PLAYER       THROWS      TOTAL   KILLS   |
|                                 |  ---------------------------------------------  |
|    SCORE: 20 pts (+8 Killshot!) |   1    Sarah         4/10        26      1/2    |
|    STREAK: 🔥🔥🔥 3 in a row    |   2    Marcus        4/10        21      0/2    |
|                                 |   3    Alex          3/10        15      0/2    |
|    [>> ON DECK: Marcus <<]      |                                                 |
+---------------------------------+-------------------------------------------------+
|                                                                                   |
|                      TARGET HIT VISUALIZER & ROUND TRAIL                          |
|       [ Real-time SVG Target rendering exact hit coordinates and ripples ]        |
|                                                                                   |
+-----------------------------------------------------------------------------------+
```

### 4.2 Dynamic Broadcast Celebrations
- **Bullseye Celebration**: Dynamic gold screen flash with particle burst.
- **Killshot Called Alert**: Cyan screen border pulse: `⚡ KILLSHOT CALLED - 8 POINTS ON THE LINE ⚡`.
- **Killshot Nailed!**: Neon cyan explosion animation with 8-point badge.
- **Heartbeat Protocol**: Automatically transmits keep-alive telemetry every 10 seconds to detect hardware disconnection.

---

## 5. Pluggable Game Engine Architecture

### 5.1 Game Engine State Model (C#)
```csharp
public interface IGameEngine
{
    string GameTypeId { get; }
    string DisplayName { get; }
    string Description { get; }
    
    GameState Initialize(GameConfig config, List<Player> players);
    ThrowResult ProcessThrow(GameState state, ThrowInput input);
    bool UndoLastThrow(GameState state);
    bool IsMatchOver(GameState state);
    MatchSummary GenerateSummary(GameState state);
}
```

### 5.2 Standard Game Catalog
1. **WATL Standard Match (`watl_standard`)**:
   - Official 10-throw match format.
   - Throws 1-10: Bullseye (6 pts) through Ring 1 (1 pt), Drop (0 pts), Miss (0 pts), Fault (0 pts).
   - **2 Anytime Kills**: Each thrower may call Killshot on any 2 throws throughout the 10-round match for 8 points.
   - Full undo support with kill quota restoration.
2. **Killshot Hunter (`kill_hunter`)**:
   - High-skill target practice: 5 throws where only Killshots (8 pts) and Bullseyes (6 pts) count. All other rings score 0.
3. **Countdown (`countdown_301` / `countdown_501`)**:
   - Start with 301 or 501 points; target reaches exactly zero with bust rule.
4. **Around the World (`around_the_world`)**:
   - Progressive target navigation from Ring 1 through Bullseye.
5. **Axe Tic-Tac-Toe (`axe_tictactoe`)**:
   - 3x3 territory grid superimposed on target board for team play.
6. **Blackjack 21 (`blackjack_21`)**:
   - Push your luck to hit exactly 21 points without busting.

---

## 6. Quality Assurance & Real-Time Verification
For detailed mathematical testing specifications (Euclidean collision thresholds, line-breaking override rules), automated test coverage (xUnit and Vitest), and step-by-step Google Chrome dual-screen manual verification procedures, consult **[Module 09: Testing Standards, Quality Assurance & Chrome Verification Protocols](./09_TESTING_AND_QUALITY_ASSURANCE_STANDARDS.md)**.


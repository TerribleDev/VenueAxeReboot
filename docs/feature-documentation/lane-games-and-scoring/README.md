# Lane Games & WATL Scoring

## 1. Overview & Business Value
The Lane Games system transforms axe throwing into a gamified sports and entertainment platform. Each lane has a **Tablet Console** (in-lane) and an **Overhead TV Display** (broadcast). The centerpiece is the **Interactive WATL Target Board** where staff/throwers tap the exact landing spot to record scores. Multiple game modes are supported beyond official WATL scoring.

### Target Roles
- **Guest Thrower**: Views scores on overhead TV
- **Coach/LaneMaster**: Records throws on tablet
- **Admin**: Starts/stops sessions, monitors games

## 2. Technical Architecture & Data Model

### Key Entities
| Entity | Base Class | Purpose |
|:-------|:-----------|:--------|
| `LaneSession` | `VenueScopedEntity` | Time-bounded session containing matches |
| `GameMatch` | `BaseEntity` | Individual game within a session |
| `MatchThrow` | `BaseEntity` | Single throw record |

### Game Engine Architecture
All engines implement `IGameEngine`:
```csharp
public interface IGameEngine {
    string GameTypeId { get; }
    string DisplayName { get; }
    GameStateSnapshot Initialize(Guid matchId, List<GamePlayer> players, GameConfig? config);
    GameStateSnapshot RecordThrow(GameStateSnapshot state, double? x, double? y, TargetZone? manualZone, bool isClutchCalled);
    GameStateSnapshot UndoLastThrow(GameStateSnapshot state);
}
```

### Registered Game Engines
| Engine | GameTypeId | Description |
|:-------|:-----------|:------------|
| `WatlStandardMatchEngine` | `watl_standard` | Official WATL 10-throw match (2 anytime killshots) |
| `CountdownGameEngine` | `countdown_603` | Start at 603, subtract to reach exactly 0 |
| `FirstTo21Engine` | `first_to_21` | Reach exactly 21; over = bust to 13 |
| `AroundTheWorldEngine` | `around_the_world` | Hit rings in sequence: 1→2→3→4→5→Bullseye→Kill |
| `KillHunterEngine` | `kill_hunter` | Only Bullseyes (6 pts) and Killshots (8 pts) score |
| `AxeTicTacToeEngine` | `axe_tictactoe` | 3×3 territory grid, 3-in-a-row wins |
| `Blackjack21Engine` | (deprecated) | Backward-compatibility wrapper for FirstTo21Engine |

### WATL Target Math (`WatlTargetMath`)
Normalized coordinate system (radius 1.0):
- Bullseye: `distance ≤ 0.0972` (6 pts)
- Ring 5: `distance ≤ 0.1667` (5 pts)
- Ring 4: `distance ≤ 0.25` (4 pts)
- Ring 3: `distance ≤ 0.3611` (3 pts)
- Ring 2: `distance ≤ 0.4722` (2 pts)
- Ring 1: `distance ≤ 0.5833` (1 pt)
- Killshots: Fixed positions at `(±0.4167, 0.5833)`, radius `0.0417`, score 8 pts when called

### Core Service: `LaneGameService`
- `StartSessionAsync(laneId, request)` — creates session + first match
- `RecordThrowAsync(laneId, input)` — records throw, replays game state, persists
- `UndoLastThrowAsync(laneId)` — removes last throw, replays state
- `SkipTurnAsync(laneId)` — records zero-point throw for current player
- `StartRematchAsync(laneId)` — new match with same players
- `SwitchGameAsync(laneId, gameTypeId)` — changes game mode mid-session
- `EndSessionAsync(laneId)` — completes session, moves lane to Turnaround

## 3. API & Telemetry Specifications

### Lane Operations Endpoints (No Auth — Terminal Access)
| Method | Route | Purpose |
|:-------|:------|:--------|
| GET | `/api/lanes/operations/games` | List available game engines |
| POST | `/api/lanes/operations/{laneId}/start-session` | Start new session |
| GET | `/api/lanes/operations/{laneId}/active-session` | Get current session state |
| POST | `/api/lanes/operations/{laneId}/throw` | Record a throw |
| POST | `/api/lanes/operations/{laneId}/undo` | Undo last throw |
| POST | `/api/lanes/operations/{laneId}/skip-turn` | Skip current player's turn |
| POST | `/api/lanes/operations/{laneId}/rematch` | Start rematch |
| POST | `/api/lanes/operations/{laneId}/switch-game` | Change game mode |
| POST | `/api/lanes/operations/{laneId}/end-session` | End session |
| POST | `/api/lanes/operations/{laneId}/extend` | Extend session time |
| POST | `/api/lanes/operations/{laneId}/safety-stop` | Emergency freeze |
| POST | `/api/lanes/operations/{laneId}/substitute` | Replace player |
| POST | `/api/lanes/operations/{laneId}/session-title` | Update session name |

### SignalR Events
| Event | Purpose |
|:------|:--------|
| `OnThrowRecorded(GameStateSnapshot)` | Updated game state after throw/undo |
| `OnClutchCalled(playerId, side)` | Clutch/Kill called notification |
| `OnKillCalled(playerId, side)` | Kill called notification |
| `OnSafetyStopActivated(reason)` | Emergency freeze broadcast |
| `OnSessionExtended(extraMinutes)` | Session time extended |

## 4. Testing Strategy

### Backend
- **Unit Tests**: `GameEngineTests.cs`, `ArcadeGameEngineTests.cs`, `WatlTargetMathTests.cs`, `GameEngineRegistryTests.cs`
- **BDD**: `WatlScoring.feature`, `ArcadeGames.feature`, `AroundTheWorld.feature`, `CountdownGame.feature`, `KillHunter.feature`
- **Integration**: `SignalRTelemetryFunctionalTests.cs`

### Frontend
- **Tests**: `watl-league-scoring.test.ts`, `arcade.test.ts`, `podium.test.ts`

## 5. Manual Verification
See root `GEMINI.md` §5.3 "Live Dual-Screen Throwing Verification" for the complete manual test protocol.

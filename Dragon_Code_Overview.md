# Dragon Boss Refactor Plan

Maps Quest Machine integration. Target: Decouple AI from movement.

---

## 1. Target Architecture

Builds four core scripts.

### Pillar A: `QuestMachineDragonBrain`
**Player View:** Dragon proactively controls battlefield. Defends crystals. Corrals player using hazards. Coordinates swarm ambushes. Flees to regenerate when health drops to critical thresholds.
**Code Function:** Translates data. Catches C# events from `BossStatsAndHealth` and environment sensors. Updates Quest Machine Node Graph variables. Processes logic tree. Outputs action. Calls `BossNavigator` methods.
**Why:** Centralizes AI logic in visual node editor. Prevents hardcoded C# logic traps. Enables complex, dynamic combat behaviors.

```mermaid
graph TD
    Data[Sensor: BossStatsAndHealth fires C# Event] --> Adapter[QuestMachineDragonBrain catches Event]
    Adapter --> QM[Updates Quest Machine Variables]
    QM --> Evaluate{Quest Machine Evaluates Variables}

    Evaluate -- Health is Critical --> Evade[Outputs 'Evade' Action]
    Evaluate -- Player is Boxed In --> Swoop[Outputs 'Swoop' Action]
    Evaluate -- Crystal is Attacked --> Defend[Outputs 'Defend' Action]

    Evade --> Execute[Adapter translates Action to C#]
    Swoop --> Execute
    Defend --> Execute

    Execute --> Motor["Calls BossNavigator.RequestSplinePath() or RequestFreestyleTarget()"]
```

```text
+-----------------------------------+
|     QuestMachineDragonBrain       |
|    (Implements IDragonBrain)      |
+-----------------------------------+
| - runtimeQuestInstance: Quest     |
| - motor: BossNavigator            |
| - vitals: BossStatsAndHealth      |
+-----------------------------------+
| + OnDamageTaken(amount): void     |
| + OnStaminaDepleted(): void       |
| + HandleQuestMachineAction(): void|
+-----------------------------------+
```

### Pillar B: `BossStatsAndHealth`
**Player View:** Dragon takes damage from arrows. Loses stamina during prolonged attacks. Visually reacts to status effects like fire or ice.
**Code Function:** Stores float variables for health and stamina. Tracks elemental status states. Fires C# event upon crossing thresholds (e.g., reaching critical health or zero stamina).
**Why:** Creates pure data container. Stops health script from forcing movement. Decouples stats from AI decisions.

```mermaid
graph TD
    Damage[Player Hits Dragon] --> Update[Update Health Float]
    Update --> Check{Is Health < Threshold?}
    Check -- Yes --> FireEvent[Fire OnHealthThresholdReached Event]
    Check -- No --> Wait[Wait for Next Hit]
```

```text
+-----------------------------------+
|        BossStatsAndHealth         |
+-----------------------------------+
| + currentHealth: float            |
| + currentStamina: float           |
| + maxHealth: float                |
+-----------------------------------+
| + TakeDamage(amount, type): void  |
| + DrainStamina(amount): void      |
| + event OnHealthThresholdReached  |
| + event OnStaminaDepleted         |
+-----------------------------------+
```
**Refactor Action:** Delete `BossCreature.cs` `Update()` loop. Delete AI logic. Rename file to `BossStatsAndHealth.cs`. Add elemental state tracking properties.

### Pillar C: `BossNavigator`
**Player View:** Dragon flies through scene geometry. Locks onto looping observation splines. Detaches from splines. Flies freely to specific destinations.
**Code Function:** Controls `transform.position` and rotation. Accepts Spline paths or Vector3 coordinates. Routes Dragon to exact location.
**Why:** Makes movement reusable. Stops flight script from querying BossPhase. Forces Navigator to blindly obey Quest Machine destinations.

```mermaid
graph TD
    Brain[Brain sends Destination] --> Motor{Motor Evaluates Command}
    Motor -- Spline Command --> Lock[Motor locks Dragon to Spline Track]
    Motor -- Freestyle Command --> Fly[Motor calculates Freestyle Flight Path]
    Motor -- Blend Command --> Lerp[Motor Lerps Dragon back to Spline]
```

```text
+-----------------------------------+
|           BossNavigator           |
+-----------------------------------+
| - currentMode: MovementMode       |
| - splineFollower: SplineFollower  |
| - targetDestination: Vector3      |
+-----------------------------------+
| + RequestSplinePath(path): void   |
| + RequestFreestyleTarget(pos):void|
| + RequestBlendToSpline(): void    |
| - TickMovement(): void            |
+-----------------------------------+
```
**Refactor Action:** Rewrite `AirborneBossMovement.cs`. Rename file to `BossNavigator.cs`. Delete `BossPhase` references.

### Pillar D: `DragonSnakeMovementStyle`
**Player View:** Dragon body slithers naturally behind head. Player destroys destructible body piece. Body pieces slide forward seamlessly to close structural gaps.
**Code Function:** Handles snake physics. Records head movement history. Creates breadcrumb trail. Drags child body pieces along exact path based on bounding box sizes.
**Why:** Merges three redundant scripts into one. Eliminates duplicate history lists. Confines body physics to single script.

```mermaid
graph TD
    Head[BossNavigator Moves Head] --> Record[Record Head Position in History List]
    Record --> Loop[Loop Through Body Segments]
    Loop --> Spacing[Calculate Bounding Box Distance for Segment]
    Spacing --> Drag[Drag Segment to History Position]
```

```text
+-----------------------------------+
|     DragonSnakeMovementStyle      |
+-----------------------------------+
| - positionHistory: List<PosData>  |
| - activeSegments: List<Segment>   |
| - segmentSpacing: float           |
| - isClosingGap: bool              |
+-----------------------------------+
| + InitializeAnatomy(): void       |
| - RecordHeadBreadcrumbs(): void   |
| - DragSegmentsAlongHistory(): void|
| - CalculateSegmentSpacings(): void|
| + HandleSegmentDestroyed(): void  |
+-----------------------------------+
```
**Refactor Action:** Delete `DragonMovementManager.cs`. Delete `DragonSpacingManager.cs`. Move spacing and history math from both files into `SegmentedDragonManager.cs`. Rename `SegmentedDragonManager.cs` to `DragonSnakeMovementStyle.cs`.

---

## 2. Current Flaws & Fixes

Addresses redundant legacy logic.

### Flaw 1: `BossCreature.cs`
**Problem:** Holds health data and AI logic. `EvaluateThreat()` method checks `currentHealth`. Method calls `movementManager.ForceImmediateEvasion()`. Logic bypasses Quest Machine completely.
**Fix:** Delete `EvaluateDesires()`. Delete `ForceImmediateEvasion()`. Move evasion logic into Quest Machine Node Graph. Move health data into `BossStatsAndHealth.cs`.
**Why:** Guarantees Quest Machine acts as sole brain. Prevents hidden C# overrides from breaking boss fight sequence.

### Flaw 2: `AirborneBossMovement.cs`
**Problem:** Controls flight and AI logic. Checks `BossCreature.currentPhase`. Checks `isTethered`.
**Fix:** Rename file to `BossNavigator.cs`. Delete phase checks. Move tether math into `DragonSnakeMovementStyle.cs`.
**Why:** Enforces single responsibility. Navigator flies exclusively. Physics script handles structural tethers.

### Flaw 3: Physics Scripts
**Problem:** Three separate scripts drag body parts.
- `SegmentedDragonManager.cs` maintains `positionHistory` list.
- `DragonMovementManager.cs` maintains duplicate `positionHistory` list.
- `DragonSpacingManager.cs` calculates distances.
**Fix:** Delete `DragonMovementManager.cs`. Delete `DragonSpacingManager.cs`. Move math into `DragonSnakeMovementStyle.cs`.
**Why:** Removes memory waste. Stops three scripts from computing identical body segment coordinates simultaneously.

### Flaw 4: Status Scripts
- **`CreatureStatusEffects.cs`:** Manages speed multipliers. Keep script. `BossNavigator.cs` reads script to adjust flight speed.
- **`DesireEvaluator.cs`:** Runs math. Delete script. Move math into Quest Machine Node Conditions. **Why:** Moves math into visual editor. Eases combat balancing.
- **`BossEventBus.cs`:** Fires events. Delete script. Modify `HealthCrystal.cs`. `HealthCrystal.cs` fires event directly to `QuestMachineDragonBrain.cs`. **Why:** Removes middleman manager. Sensors ping Brain directly.

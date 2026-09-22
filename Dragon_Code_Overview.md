# Dragon Boss Refactoring Document

This document maps the Quest Machine refactor. We will rewrite `BossCreature.cs`, `AirborneBossMovement.cs`, `SegmentedDragonManager.cs`, `DragonMovementManager.cs`, and `DragonSpacingManager.cs`. We will move AI logic into Quest Machine. We will move math and movement into C# scripts.

---

## 1. Target Architecture

We will build four scripts.

### Pillar A: `QuestMachineDragonBrain`
**Player View:** The Dragon reacts to the game state. It runs away when hurt. It attacks when the player stands still.
**Code Function:** This script translates data. It receives C# events from `BossStatsAndHealth`. It sends data to the Quest Machine Node Graph. Quest Machine runs the logic tree. Quest Machine outputs an action. This script tells `BossNavigator` where to fly.

```mermaid
graph TD
    Data[Sensor: BossStatsAndHealth fires C# Event] --> Adapter[QuestMachineDragonBrain catches Event]
    Adapter --> QM[Updates Quest Machine Variables]
    QM --> Evaluate{Quest Machine Evaluates Variables}

    Evaluate -- Health is 0 --> Evade[Outputs 'Evade' Action]
    Evaluate -- Player is Exposed --> Swoop[Outputs 'Swoop' Action]
    Evaluate -- Crystal is Attacked --> Defend[Outputs 'Defend' Action]

    Evade --> Execute[Adapter translates Action to C#]
    Swoop --> Execute
    Defend --> Execute

    Execute --> Motor["Calls BossNavigator.RequestPath()"]
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
**Player View:** The Dragon takes damage. The Dragon loses stamina. The Dragon catches on fire.
**Code Function:** This script holds data. It holds float variables for health and stamina. Health hits zero. This script fires a C# event.

```mermaid
graph TD
    Damage[Player Hits Dragon] --> Update[Update Health Float]
    Update --> Check{Is Health < 0?}
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
**Refactor Action:** We will rewrite `BossCreature.cs`. We will delete the `Update()` loop. We will delete AI logic. We will rename the file to `BossStatsAndHealth.cs`.

### Pillar C: `BossNavigator`
**Player View:** The Dragon flies. It locks onto splines. It detaches from splines. It flies freely.
**Code Function:** This script controls `transform.position`. It accepts a Vector3 coordinate. It flies the Dragon to the coordinate.

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
**Refactor Action:** We will rewrite `AirborneBossMovement.cs`. We will rename the file to `BossNavigator.cs`. We will delete references to `BossPhase`.

### Pillar D: `DragonSnakeMovementStyle`
**Player View:** The Dragon body slithers behind the head. The player shoots a body piece. The piece explodes. The body pieces slide forward.
**Code Function:** This script handles snake physics. It records head movement. It creates a history trail. It moves child body pieces along the trail.

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
**Refactor Action:** We will delete `DragonMovementManager.cs`. We will delete `DragonSpacingManager.cs`. We will move math from both files into `SegmentedDragonManager.cs`. We will rename `SegmentedDragonManager.cs` to `DragonSnakeMovementStyle.cs`.

---

## 2. Current State

We will fix flaws in the old code.

### Flaw 1: `BossCreature.cs`
**Problem:** `BossCreature.cs` holds health data and AI logic. The `EvaluateThreat()` method checks if `currentHealth` is low. The method calls `movementManager.ForceImmediateEvasion()`. This logic bypasses Quest Machine.
**Fix:** We will delete `EvaluateDesires()`. We will delete `ForceImmediateEvasion()`. We will move logic into the Quest Machine Node Graph. We will move health data into `BossStatsAndHealth.cs`.

### Flaw 2: `AirborneBossMovement.cs`
**Problem:** `AirborneBossMovement.cs` controls flight and AI logic. It checks `BossCreature.currentPhase`. It checks `isTethered`.
**Fix:** We will rename the file to `BossNavigator.cs`. We will delete phase checks. We will move tether logic into `DragonSnakeMovementStyle.cs`.

### Flaw 3: Physics Scripts
**Problem:** Three scripts drag body parts.
- `SegmentedDragonManager.cs` maintains a `positionHistory` list.
- `DragonMovementManager.cs` maintains a `positionHistory` list.
- `DragonSpacingManager.cs` calculates distances.
**Fix:** We will delete `DragonMovementManager.cs`. We will delete `DragonSpacingManager.cs`. We will move math into `DragonSnakeMovementStyle.cs`.

### Flaw 4: Status Scripts
- **`CreatureStatusEffects.cs`:** This script manages speed multipliers. We will keep this script. `BossNavigator.cs` will read this script.
- **`DesireEvaluator.cs`:** This script runs math. We will delete this script. We will put math into Quest Machine Node Conditions.
- **`BossEventBus.cs`:** This script fires events. We will delete this script. We will modify `HealthCrystal.cs`. `HealthCrystal.cs` will fire an event to `QuestMachineDragonBrain.cs`.

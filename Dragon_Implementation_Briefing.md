# Dragon Boss Implementation Briefing

**To:** Technical Coding AI / Developer
**Subject:** Dragon Boss Architecture - Greenfield Implementation

## Objective
The current Dragon Boss scripts (movement, AI, spacing) are fundamentally flawed and will be discarded. You will build a new, clean architecture from scratch based on the four-pillar design. The final architecture relies entirely on **Quest Machine** acting as the sole brain, while C# scripts act strictly as sensors (Vitals) and actuators (Movement/Physics).

## Required Plugins & Namespaces
This architecture depends on two core third-party assets. You must include their namespaces in the new scripts:
- **PixelCrushers Quest Machine:** Used for all AI node evaluation (`using PixelCrushers.QuestMachine;`).
- **Dreamteck Splines:** Used for spatial movement and pathfinding via `SplineComputer` and `SplineFollower` components (`using Dreamteck.Splines;`).

## Core Directives & Constraints
- **Zero AI Logic in C#:** Do not write `if (health < 0)` combat decisions in the C# movement or vitals scripts. All combat logic must exist as nodes/conditions inside the visual Quest Machine editor.
- **Strict Method Targeting:**
  - When instructing `BossNavigator` to follow a spline, pass the raw `SplineComputer` reference via `RequestSplinePath(SplineComputer path)`.
  - When instructing it to freestyle, pass the `Vector3` via `RequestFreestyleTarget(Vector3 pos)`.
- **Prefab Hierarchy Integrity:** The `Boss_AsianFireDragonNew` prefab uses a `SplineFollower` component on its root. **You must set `SplineFollower.follow = false` by default in the prefab.** If true, the plugin will attempt to hijack the transform before the `BossNavigator` calculates the desired path.
- **Brand New Quest Generator:** The existing `DragonBrainQuestGenerator.cs` is obsolete. You must write a *brand new* C# Editor script to programmatically generate the Quest Machine asset and configure its node tree for this specific architecture.

---

## Execution Sequence (Phases 1-4)

Do not attempt to salvage the old scripts. Build the new ones fresh, then delete the old ones.

### Phase 1: `BossStatsAndHealth` (The Vitals Sensor)

```text
+-----------------------------------+
|        BossStatsAndHealth         |
+-----------------------------------+
| + currentHealth: float            |
| + currentStamina: float           |
| + maxHealth: float                |
| + criticalHealthThreshold: float  |
+-----------------------------------+
| + TakeDamage(amount, type): void  |
| + DrainStamina(amount): void      |
| + event OnHealthThresholdReached  |
| + event OnStaminaDepleted         |
+-----------------------------------+
```

1. **Create** a new script `BossStatsAndHealth.cs`.
2. Give it a serialized float `public float criticalHealthThreshold = 50f;`.
3. Give it `currentHealth` and `currentStamina`.
4. Ensure `TakeDamage()` updates the health float and strictly fires an `OnHealthThresholdReached` C# event when `currentHealth` drops below `criticalHealthThreshold`.
5. Do not include an `Update()` loop.

### Phase 2: `BossNavigator` (The Movement Actuator)

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

1. **Create** a new script `BossNavigator.cs`.
2. It must inherit directly from `MonoBehaviour`.
3. Expose `RequestSplinePath(SplineComputer path)`, `RequestFreestyleTarget(Vector3 pos)`, and `RequestBlendToSpline()` as public methods.
4. Ensure `TickMovement()` multiplies flight speed by `CreatureStatusEffects.CurrentSpeedMultiplier`.
5. Ensure it has zero references to AI phases or decisions.

### Phase 3: `DragonSnakeMovementStyle` (The Physics Actuator)

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

1. **Create** a new script `DragonSnakeMovementStyle.cs`.
2. It must contain a single `positionHistory` array to track the head's breadcrumbs.
3. Write clean math to calculate segment spacing based on bounding box lengths.
4. Write clean math to interpolate child segments along the history array.
5. **Delete** the legacy physics scripts: `SegmentedDragonManager.cs`, `DragonSpacingManager.cs`, and `DragonMovementManager.cs`.

### Phase 4: `QuestMachineDragonBrain` (The Adapter & AI)

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

1. **Create** a new script `QuestMachineDragonBrain.cs` that implements `IDragonBrain`.
2. **Create** the custom Quest Machine adapter nodes: `QuestAction_CommandSplineFlight`, `QuestAction_CommandFreestyleFlight`, and `QuestCondition_CheckHealthDrops` (See `Dragon_Architecture_Blueprint.md` for class signatures).
3. Write a **new** Editor script (`DragonBrainQuestGenerator_V2.cs`) to build the logic tree using these custom nodes.
4. Configure the generated logic tree to handle these specific fallback scenarios using Quest Machine variables (e.g., `ActiveCrystals == 0`):
   - *Condition: High Health* -> Action: Output 'Swoop' (Counter-attack).
   - *Condition: Low Health + ActiveCrystals > 0* -> Action: Output 'Defend' (Flee to Crystal).
   - *Condition: Low Health + ActiveCrystals == 0 + MinionsAlive > 0* -> Action: Output 'Bait & Herd' (Use Minions).
   - *Condition: Low Health + ActiveCrystals == 0 + MinionsAlive == 0* -> Action: Output 'Tactical Weave' (Last Stand using Evasion Splines offensively).
5. **Delete** the legacy AI scripts: `BossCreature.cs`, `AirborneBossMovement.cs`, `BaseBossMovement.cs`, `DesireEvaluator.cs`, and `BossEventBus.cs`.

---
*Note: Refer to `Dragon_Architecture_Blueprint.md` for specific ASCII class diagrams and Mermaid logic flowcharts detailing the final structure.*

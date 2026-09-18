# Decoupled Combat Architecture Guide

> [!info] The Architectural Goal
> In earlier versions, visual effect scripts (like `DissolveEffect`) were acting as a "middleman," intercepting arrow hits and managing health logic. This created rigid, hard-to-maintain code that broke physics interactions.
>
> The new architecture enforces strict **Decoupling**. Visual effects, Combat Logic, and Physics are entirely separated. Entities (like Minions and Bosses) are now "Facilitators." They manage their own health natively and simply send *messages* to Managers when tasks need to be performed.

---

## The Complete Player Experience Flow
This diagram details the exact chain of events from the moment the player shoots an arrow, to the moment the wave advances. It flows from top to bottom.

```mermaid
sequenceDiagram
    direction TB
    actor Player
    participant Arrow as StickingArrow.cs
    participant UnityPhysics as Unity Physics Engine
    participant Creature as Creature Brain (StandardCreature / DragonSegment)
    participant DissolveData as DissolveEffect.cs (Data Container)
    participant DissolveManager as DissolveManager (Global Singleton)
    participant Tracker as Tracking Manager (MinionManager / SegmentedDragonManager)
    participant WaveSpawner as WaveSpawner.cs

    %% 1. The Shot
    rect rgb(200, 220, 240)
    Note over Player, UnityPhysics: Phase 1: The Shot & Hit Detection
    Player->>Arrow: Fires Arrow
    Arrow->>UnityPhysics: Arrow travels through space
    UnityPhysics-->>Arrow: OnCollisionEnter() detected on "Enemy" Layer
    end

    %% 2. Combat Resolution
    rect rgb(240, 220, 200)
    Note over Arrow, Creature: Phase 2: Combat Resolution
    Arrow->>Creature: Calls OnArrowHit() via IArrowTarget
    Creature->>Creature: Processes TakeDamage(amount, elementType)
    Creature->>Creature: Health drops to 0. Sets isDead = true
    Creature->>Creature: Disables physical colliders immediately
    end

    %% 3. Visual Death
    rect rgb(220, 240, 200)
    Note over Creature, DissolveManager: Phase 3: Visual Death Sequence
    Creature->>DissolveData: Calls TriggerDissolve(callback: FinalizeDestruction)
    DissolveData->>DissolveManager: Passes Renderer data & Callback to Manager
    DissolveManager->>DissolveManager: Executes Shader Fade Coroutine Over Time
    DissolveManager->>DissolveData: Coroutine finishes. Fires Callback.
    DissolveData-->>Creature: FinalizeDestruction() triggered!
    end

    %% 4. Scene Cleanup
    rect rgb(230, 210, 240)
    Note over Creature, WaveSpawner: Phase 4: Wave Progression & Cleanup
    Creature->>Tracker: Calls OnDestroyed(this)
    Tracker->>Tracker: Removes entity from active tracking lists
    Tracker->>WaveSpawner: Calls NotifyTargetDestroyed() (Decrements counter)
    WaveSpawner->>WaveSpawner: Checks active enemies. If 0, Advance Wave!
    Tracker-->>Creature: Destroys physical GameObject from scene
    end
```

> [!tip] Universal Compatibility
> This exact flow is symmetric. Whether the `Creature Brain` is a `StandardCreature` (Minion) or a `DragonSegment` (Boss piece), the exact same interface calls and Manager handoffs occur. This guarantees uniform arrow interactions across the entire game.

> [!danger] Do not add logic to `DissolveEffect`
> `DissolveEffect` is now purely a container for public fields (`dissolveDuration`, `targetRenderers`). It should never hold health logic or physics interactions.

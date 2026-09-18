# Dragon AI Desires Flowchart

> [!info] The AI Brain
> The Dragon's intelligence is governed by `BossCreature.cs` (The Brain) and `TacticalBossSplineManager.cs` (The Movement Executor). The Brain constantly evaluates its state (Health, Stamina, Threat Level) and issues commands to the Movement Executor, which smoothly transitions between free flight and structured spline paths to achieve those goals.

---

## AI Decision Flow
This flowchart maps how the Dragon evaluates its priorities and transitions between different behaviors.

```mermaid
flowchart TD
    %% Base States
    Start[Battle Starts] --> Orchestrator

    %% Orchestrator Phase
    subgraph Phase: Orchestrator
        Orchestrator((Orchestrator Mode)) -->|Circles Crystal/Spawns Minions| ObsSpline(Follows Observation Spline)
        ObsSpline -.->|Player shoots Boss| EarlyEngage{Damaged Early?}
        EarlyEngage -- Yes --> EngageState((Engaged Mode))
        ObsSpline -.->|Minions Defeated / Time passes| EngageState
    end

    %% Engaged Phase
    subgraph Phase: Engaged
        EngageState --> EvalEngage{Assess Threat & Stamina}

        EvalEngage -->|Stamina High, Threat Low| Pursue(Freestyle: Pursue Player)
        Pursue -->|Close enough| Attack(Freestyle: Swoop & Shoot Fireballs)

        EvalEngage -->|Threat Level High \n (Taking Rapid Damage)| EvadeCommand(Tactical Evasion Command)
        EvalEngage -->|Stamina Depleted| EvadeCommand

        EvadeCommand -->|Find Nearest Escape Route| FreestyleToEscape[Freestyle: Fly to Escape Spline Start]
        FreestyleToEscape --> ExhaustedState((Exhausted Mode))
    end

    %% Exhausted / Fleeing Phase
    subgraph Phase: Exhausted
        ExhaustedState --> FlyEscapeSpline(Follows Escape Spline)
        FlyEscapeSpline -->|Follow spline to exit area| ReturnToObs[Freestyle: Return to Observation Spline]
        ReturnToObs --> RechargingState((Recharging Mode))
    end

    %% Recharging Phase
    subgraph Phase: Recharging
        RechargingState --> RechargeLogic(Follows Observation Spline\nRegain Stamina)
        RechargeLogic -->|Stamina Full| EngageState
    end
```

---

## Detailed Logic Breakdown

### 1. Orchestrator (The Setup)
- **Desire:** Setup the battlefield, intimidate, and protect its domain.
- **Action:** The Dragon locks onto an **Observation Spline** (circling high above). It won't actively hunt the player yet.
- **Triggers:** If the player ignores the minions and shoots the Dragon while it's watching, it gets angry and instantly switches to **Engaged**.

### 2. Engaged (The Hunt)
- **Desire:** Destroy the player.
- **Action:** The Dragon uses **Freestyle Flight** to aggressively pursue the player. It uses momentum-based steering to swoop and fire projectiles.
- **The "Evasion Threshold":** The Brain (`BossCreature.cs`) accumulates damage over time. If the player deals burst damage rapidly (crossing the `evasionDamageThreshold`), the Dragon decides it is taking too much heat and forces a tactical retreat, even if it has stamina left.
- **Stamina Drain:** Attacking drains stamina. Once depleted, the Dragon must retreat.

### 3. Exhausted (The Tactical Retreat)
- **Desire:** Escape the immediate danger zone to recover.
- **Action:** The Dragon locates the nearest/best **Escape Spline**.
- **Crucial Behavior:** Instead of teleporting, it transitions smoothly into **Freestyle Flight** to physically fly from its current mid-air position to the start of the chosen Escape Spline. Once there, it locks on and rides the spline away from the player.

### 4. Recharging (The Recovery)
- **Desire:** Regain stamina and (if configured) interact with Health Crystals.
- **Action:** After finishing the Escape route, the Dragon freestyle-flies back to an **Observation Spline**. Here it catches its breath. Once stamina reaches 100%, it dives back into the **Engaged** phase for another attack run.

> [!tip] Predictability vs. Chaos
> The AI is designed to mix **Predictability** (Escape routes and Observation splines so players can learn patterns and anticipate where it will go) with **Chaos** (Freestyle pursuit and dodging so players can't just camp one spot).

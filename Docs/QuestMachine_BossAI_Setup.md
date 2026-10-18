# Dragon AI Behavior Tree (Automated Setup)

> [!tip] ZERO MANUAL SETUP REQUIRED
> You do not need to look at the Inspector, add Actions to the Active state, or configure Conditions on the node blocks anymore!
>
> I have created an Editor Tool that will automatically build this entire Behavior Tree for you. It automatically creates every Node, writes all the Action strings, and properly places the Message conditions in the `Conditions` foldout (exactly as you showed me).
>
> **To generate the AI:**
> 1. In Unity, go to the very top menu.
> 2. Click exactly: **Archery Range -> Generate Dragon Brain Quest**
> 3. The script will create a new file named `DragonBrain_Automated` in your `Assets` folder and highlight it for you.
> 4. Simply drag this new `DragonBrain_Automated` asset into the `dragonBrainAsset` slot on your Boss Prefab's `DragonBrainController`.

---

> [!info] Visual Reference
> Below is the map of what the automated script generates behind the scenes. You do not need to build this manually anymore!

```mermaid
flowchart TD
    Start([Battle Starts]) --> Orch((Orchestrator))

    Orch -->|Circle + Spawn Minions| Obs(Observation Spline)
    Obs -.->|Player shoots Boss| Engage
    Obs -.->|Minions beaten / Time| Engage
    Obs -.->|Crystal destroyed| Rage

    Engage((Engaged)) --> Eval{Weigh Desire vs Threat}

    Eval -->|Dominance, low threat| Pursue(Freestyle: Graceful Pursuit)
    Pursue --> Swoop(Freestyle: Committed Swoop + Breath)
    Swoop -->|Overshoot| Bank(Freestyle: Wide Bank + Reacquire)
    Bank --> Pursue

    Eval -->|Territory control| Bait(Freestyle: Drop Spirit Zones)
    Bait --> Herd(Freestyle: Herd Player)
    Herd --> Swoop

    Eval -->|Reserve rich, player pressured| Spawn(Freestyle: Spawn Wave)
    Spawn -->|Tag: Chokepoint| Herd
    Spawn -->|Tag: Cover| Flank(Freestyle: Flank via Cover)

    Eval -->|Elemental advantage| Breath(Freestyle: Breath Attack)
    Breath -->|Tag: ToppleObject| Topple(Freestyle: Topple Object)

    Eval -->|Crystal threatened| Defend((Defender))
    Eval -->|Burst damage / No stamina| Evade(Tactical Withdrawal)
    Eval -->|Stamina empty| Evade

    Defend -->|Crystal safe| Engage
    Defend -->|Crystal destroyed| Rage

    Evade -->|Pick best spline via Tags| FlyEsc[Freestyle: Glide to Escape Spline]
    FlyEsc --> Exhaust((Exhausted))

    Exhaust --> EscSpline(Ride Escape Spline)
    EscSpline --> Recharge((Recharging))
    Recharge --> RechargeLoop(Observation Spline - Regen Stamina)
    RechargeLoop -->|Full, crystals intact| Engage
    RechargeLoop -->|Full, crystals lost| Rage

    Rage((Enraged)) --> RageEval{Segments Intact?}
    RageEval -- Yes --> Relentless(Freestyle: All-in Aggression)
    RageEval -- No --> Regen((Regenerator))
    Relentless -->|Stamina zero| Exhaust

    Regen --> Seek(Freestyle: Fly to Nearest Crystal)
    Seek --> Absorb(Freestyle: Drain Crystal)
    Absorb -->|Segments restored| Engage
    Absorb -->|No crystals left| Desperate((Desperate))
    Desperate --> Relentless
```
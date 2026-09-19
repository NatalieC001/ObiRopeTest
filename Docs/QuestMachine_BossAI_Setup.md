# Dragon AI Behavior Tree (Consolidated Blueprint)

> [!tip] How to read this in Quest Machine
> - **Every box below is a single Task Node.**
> - All the data you need to type into the Unity Inspector is inside the box.
> - **Connections (Arrows)**: Just draw a line from the top node's **Green (Success) pin** to the node below it. No text goes on the connection itself.

```mermaid
flowchart TD
    Start(["Node: Start"]) --> Orch

    Orch(["Node: Orchestrator"])

    Orch --> Obs
    Obs(["Node: Observation Spline<br/>Condition: (Circle + Spawn Minions)"])

    Obs -.-> Engage_Shoots
    Engage_Shoots(["Node: Engaged<br/>Condition: PlayerShootsBoss"])

    Obs -.-> Engage_Time
    Engage_Time(["Node: Engaged<br/>Condition: Minions beaten / Time"])

    Obs -.-> Rage_Crystal
    Rage_Crystal(["Node: Enraged<br/>Condition: Crystal destroyed"])

    Engage(["Node: Engaged<br/>Action: Weigh Desire vs Threat"]) --> Pursue
    Engage --> Bait
    Engage --> Spawn
    Engage --> Breath
    Engage --> Defend
    Engage --> Evade_Dmg
    Engage --> Evade_Stamina

    Pursue(["Node: Pursue<br/>Condition: DesireDominance<br/>Action: Pursuit"]) --> Swoop
    Swoop(["Node: Swoop<br/>Action: Swoop"]) --> Bank
    Bank(["Node: Bank<br/>Condition: SwoopOvershoot<br/>Action: Bank"]) --> Pursue

    Bait(["Node: Bait<br/>Condition: DesireTerritory<br/>Action: Bait"]) --> Herd
    Herd(["Node: Herd<br/>Action: Herd"]) --> Swoop

    Spawn(["Node: Spawn<br/>Condition: DesireSpawn<br/>Action: SpawnWave"]) --> Herd_Choke
    Herd_Choke(["Node: Herd<br/>Condition: TagChokepoint<br/>Action: Herd"])

    Spawn --> Flank
    Flank(["Node: Flank<br/>Condition: TagCover<br/>Action: Flank"])

    Breath(["Node: Breath<br/>Condition: DesireElement<br/>Action: Breath"]) --> Topple
    Topple(["Node: Topple<br/>Condition: TagToppleObject<br/>Action: Topple"])

    Defend(["Node: Defender<br/>Condition: DesireDefend<br/>Action: DefendCrystal"])
    Defend --> Engage_Safe
    Engage_Safe(["Node: Engaged<br/>Condition: Crystal safe"])

    Defend --> Rage_Dest
    Rage_Dest(["Node: Enraged<br/>Condition: Crystal destroyed"])

    Evade_Dmg(["Node: Evade<br/>Condition: BurstDamageTaken<br/>Action: Evade"]) --> FlyEsc
    Evade_Stamina(["Node: Evade<br/>Condition: StaminaEmpty<br/>Action: Evade"]) --> FlyEsc

    FlyEsc(["Node: Glide to Escape Spline<br/>Action: (Pick best spline via Tags)"]) --> Exhaust

    Exhaust(["Node: Exhausted<br/>Action: Ride Escape Spline"]) --> EscSpline
    EscSpline(["Node: EscSpline<br/>Action: Ride Escape Spline"]) --> Recharge

    Recharge(["Node: Recharging<br/>Action: Recharging"]) --> RechargeLoop
    RechargeLoop(["Node: RechargeLoop<br/>Action: Observation Spline - Regen Stamina"])

    RechargeLoop --> Engage_Full
    Engage_Full(["Node: Engaged<br/>Condition: RechargeFull"])

    RechargeLoop --> Rage_Lost
    Rage_Lost(["Node: Enraged<br/>Condition: Crystals lost"])

    Rage(["Node: Enraged<br/>Action: Rage"]) --> Relentless
    Rage --> Regen

    Relentless(["Node: Relentless<br/>Condition: SegmentsIntact<br/>Action: Relentless"]) --> Exhaust_Rage
    Exhaust_Rage(["Node: Exhausted<br/>Condition: StaminaEmpty<br/>Action: Evade"])

    Regen(["Node: Regenerator<br/>Condition: SegmentsMissing<br/>Action: Regenerate"]) --> Seek
    Seek(["Node: Seek<br/>Action: Fly to Nearest Crystal"]) --> Absorb
    Absorb(["Node: Absorb<br/>Action: Drain Crystal"])

    Absorb --> Engage_Restored
    Engage_Restored(["Node: Engaged<br/>Condition: Segments restored"])

    Absorb --> Desperate
    Desperate(["Node: Desperate<br/>Condition: NoCrystalsLeft<br/>Action: Desperate"]) --> Relentless_Desp
    Relentless_Desp(["Node: Relentless<br/>Action: Relentless"])
```
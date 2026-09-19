# 🐉 Dragon Brain: Architecture & Integration Guide

This document explains the fully event-driven, Quest Machine-powered AI system for the Dragon Boss. It is designed for maximum VR performance by using a **single 10Hz heartbeat** instead of heavy, per-frame updates.

---

## 🧠 The Core Philosophy
The Dragon does **not** think every frame.
1. **The Tick (`DragonTick.cs`):** A single script runs 10 times a second. It reads the dragon's physical state (Health, Stamina, Threat) and checks the `DesireEvaluator`. If something important changes, it fires a string message (e.g., `StaminaEmpty`).
2. **The Brain (Quest Machine):** The visual graph receives the message, moves to the next node, and fires a command back out (e.g., `Evade`).
3. **The Listeners (`DragonActionListeners.cs`):** Passive scripts wait for the command and execute the actual physical movement or animation. They cost **zero CPU** until called.

---

## 🏗️ Prefab Hierarchy & Setup
To set up the Dragon in a scene, your prefab hierarchy must look exactly like this.

```text
Dragon (Root)
├── 🟢 SplineFollower                  ← Drives the root on splines
├── 🟠 AirborneBossMovement            ← The movement controller (Spline vs Freestyle)
├── 🟢 BossCreature                    ← State Holder (Health, Stamina tracking only)
├── 🟠 CreatureStatusEffects           ← Reads incoming arrow elemental effects
├── 🟢 SegmentedDragonManager          ← Spawns and manages physical body segments
├── 🔵 RegeneratorController           ← Drains crystals, rebuilds segments
├── 🔵 ElementalBreathController       ← Fires breath attacks from the mouth
│
├── 🔴 DragonTick                      ← THE HEARTBEAT (10Hz Polling)
├── 🔴 DragonBrainController           ← Spawns the Quest Machine Brain at runtime
├── 🔴 DragonActionListeners           ← Receives commands from the Brain
│
├── 📁 SplineAnchor                    ← Empty transform
│   ├── Head
│   │   └── mouthTransform
│   ├── Body (segments)
│   └── Tail
│
└── 📁 Colliders                       ← Hitboxes (Must be on 'Enemy' layer)
```

> [!tip] **Where do I put the Quest Machine Asset?**
> Click on the **DragonBrainController** component on the Dragon Root, and drag your `DragonBrain.asset` into the empty slot. It will clone itself automatically when the game starts!

---

## 🗺️ Scene-Level Dependencies
The Dragon does not exist in a vacuum. It requires these global managers to exist in the scene hierarchy:

* **`WaveSpawner`**: Spawns the Dragon and holds the `bossMinionRoster`.
* **`MinionManager`**: Tracks when standard creatures die and explicitly notifies the wave spawner.
* **`MinionRequestBroker`**: The Dragon asks this script to spend its reserve and spawn minions.
* **`BossPathManager`**: Holds the `Airborne` and `Terrestrial` paths. The Dragon queries this to find its Observation Splines!
* **`EnvironmentTagRegistry`**: Tracks cover, chokepoints, and crystals for the Dragon to interact with.

---

## ✉️ The Message Dictionary

How does the C# code talk to the visual Quest Machine graph? Through the Pixel Crushers `MessageSystem`.

### 📥 Messages SENT TO the Brain (From `DragonTick.cs`)
These messages trigger conditions in your Quest Machine nodes to move the flowchart forward.

| Message | When is it sent? |
| :--- | :--- |
| `BossDamaged` | Health drops more than 10% in a single tick. |
| `StaminaEmpty` | Stamina drops to 0 while engaged. |
| `CrystalThreatened` | Player hits a crystal. |
| `CrystalSafe` | 5 seconds pass without the crystal taking damage. |
| `CrystalDestroyed` | The active crystal shatters. |
| `MinionsBeaten` | The Dragon realizes one of its spawned minions died. |
| `RechargeFull` | Stamina hits 100% while resting on an Observation Spline. |
| `SegmentsIntact` / `SegmentsMissing` | Evaluates if the dragon has taken structural damage. |
| `DesireEvade`, `DesireDominance`, etc. | The `DesireEvaluator` math changes its primary intent. |

### 📤 Messages RECEIVED FROM the Brain (To `DragonActionListeners.cs`)
When a Quest Machine node becomes active, it fires these messages out to make the Dragon physically move.

| Action Command | What it physically does |
| :--- | :--- |
| `Pursuit` | `FreestyleIntent.Pursue` toward player. |
| `Swoop` | Fast Pursuit + Fires Elemental Breath. |
| `Bank` | `FreestyleIntent.Withdraw` upwards and away. |
| `SpawnWave` | Pings `MinionRequestBroker` to spend reserves. |
| `DefendCrystal` | Snaps immediately back to the nearest Observation Spline coil. |
| `Evade` | Jumps to the nearest Escape Spline. |
| `Regenerate` | Flies to nearest `HealthCrystal` and begins healing/regrowing segments. |

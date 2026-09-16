# 🏹 Level Design & Encounter Configuration Guide

This document explains exactly how to set up exciting, varied encounters using the `LevelDesign_Template.csv`. By mixing different targets, movement types, elements, and modifiers, you can build everything from simple target practice to epic boss battles.

## 1. The Three Core Target Types
When building a wave, you are choosing between three fundamental types of spawns. This is controlled by the `MovementBehavior` column.

*   **Vanilla Targets:** Use standard movement types like `PingPongMovement`, `Rotator`, `CorkscrewMovement`, `FigureEightMovement`, `FishSwimMovement`, `SwoopAndRetreatMovement`, or `None` (Static). These spawn individual models (like the gong/target).
*   **Spline Swarms:** Use `SplinePathAsset`. This spawns an entire pre-baked swarm of enemies on a custom track.
*   **Boss Encounters:** Use `BossDragonAsset`. This spawns a massive, multi-phase boss.

> **CRITICAL RULE FOR BOSSES:** To spawn exactly **one** boss, you must have exactly **one** row in your CSV using `BossDragonAsset` for that wave.
> Do not copy/paste multiple `BossDragonAsset` rows unless you explicitly want a chaotic 2v1 or 3v1 boss fight!

---

## 2. Setting Up the CSV Exactly Right

Here is the exact column breakdown for your `LevelDesign_Template.csv`:

*   **LevelName:** Groups waves together. (e.g., `Level1_Intro`)
*   **LevelIntroText & LevelOutroText:** Text shown at the start/end of the level.
*   **WaveName:** Groups targets into a single wave. (e.g., `Wave1`)
*   **ProgressionType:**
    *   `ClearAllTargets`: The wave ends only when every target/boss is dead.
    *   `TimeBased`: The wave automatically ends after a set time, regardless of what is alive.
*   **WaveDuration:** Used only if `TimeBased`. How many seconds the wave lasts.
*   **SpawnDelay:** How long (in seconds) to wait before spawning this specific row. Useful for staggering enemy arrivals!
*   **PosX & PosZ:** The relative spawn position from the center. (Y is always kept above ground).
*   **ScaleModifier:** Makes targets smaller (e.g., `0.5` = 50% size) or larger (e.g., `2.0` = 200% size). Smaller targets are harder to hit!
*   **SpeedModifier:** Makes vanilla movement scripts faster (e.g., `1.5` = 50% faster).
*   **MovementBehavior:** Defines what gets spawned (see Core Target Types above).
*   **RequiredElement:** Forces the player to use specific arrows (`Normal`, `Fire`, `Electric`, `Ice`).

---

## 3. Exciting Encounter Recipes (CSV Examples)

Here are distinct examples of how to combine these settings for dynamic gameplay.

### Recipe 1: The Warmup (Static & Moving Mix)
A simple "Clear all targets" wave. Some sit still, some move, and one is tiny and fast.

```csv
LevelName,LevelIntroText,LevelOutroText,WaveName,ProgressionType,WaveDuration,SpawnDelay,PosX,PosZ,ScaleModifier,SpeedModifier,MovementBehavior,RequiredElement
Level1_Warmup,Get your eye in!,Nice shooting.,Wave1,ClearAllTargets,0,0,-5,10,1,1,None,Normal
Level1_Warmup,Get your eye in!,Nice shooting.,Wave1,ClearAllTargets,0,0,5,10,1,1,None,Normal
Level1_Warmup,Get your eye in!,Nice shooting.,Wave1,ClearAllTargets,0,0,0,15,1,1,PingPongMovement,Normal
Level1_Warmup,Get your eye in!,Nice shooting.,Wave1,ClearAllTargets,0,2,0,25,0.5,2,CorkscrewMovement,Normal
```

### Recipe 2: Elemental Ambush (Time Survival)
The player must survive for 30 seconds against a horde.

```csv
LevelName,LevelIntroText,LevelOutroText,WaveName,ProgressionType,WaveDuration,SpawnDelay,PosX,PosZ,ScaleModifier,SpeedModifier,MovementBehavior,RequiredElement
Level2_Ambush,Survive for 30s!,Well done!,Wave1,TimeBased,30,0,0,10,1,0,SplinePathAsset,Normal
Level2_Ambush,Survive for 30s!,Well done!,Wave1,TimeBased,30,5,-10,15,1.5,1,SwoopAndRetreatMovement,Fire
Level2_Ambush,Survive for 30s!,Well done!,Wave1,TimeBased,30,5,10,15,1.5,1,SwoopAndRetreatMovement,Ice
```

### Recipe 3: The True Boss Fight
An epic battle where the boss watches the player fight minions, then engages.

```csv
LevelName,LevelIntroText,LevelOutroText,WaveName,ProgressionType,WaveDuration,SpawnDelay,PosX,PosZ,ScaleModifier,SpeedModifier,MovementBehavior,RequiredElement
Level3_Boss,The Dragon Watches!,You survived!,Wave1,ClearAllTargets,0,0,0,10,1,1,SplinePathAsset,Normal
Level3_Boss,The Dragon Watches!,You survived!,Wave1,ClearAllTargets,0,0,0,10,1,1,BossDragonAsset,Normal
```

---

## 4. How to Fix Multiple Bosses Spawning

If you press Play and see 2 or 3 dragons stacked on top of each other, use the **Level Diagnostic Tool** to see exactly what is going wrong:
1. Click `Archery Range -> Run Level Diagnostics` at the top of the Unity window.
2. Read the `LevelDiagnosticReport.txt` that is generated.

**The Fix:**
You have a missing prefab in your level configuration, causing the wrong objects to spawn. You must fix the prefab assignments.
1. Open the CSV Level Importer (`Archery Range -> CSV Level Importer`).
2. Click the new `Auto-Find Prefabs in Project` button to assign the Vanilla, Spline, and Boss prefabs automatically.
3. Click `Generate Levels & Waves`.
4. Click `Archery Range -> Run Level Diagnostics` again to verify there are no WARNINGS in the text file.

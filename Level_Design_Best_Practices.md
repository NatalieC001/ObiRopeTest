# 🏹 Level Design & Encounter Configuration Guide

This document explains exactly how to set up exciting, varied encounters using the `LevelDesign_Template.csv`. By mixing different targets, movement types, elements, and modifiers, you can build everything from simple target practice to epic boss battles.

## 1. The Three Core Target Types
When building a wave, you are choosing between three fundamental types of spawns. This is controlled by the `MovementBehavior` column in your CSV.

*   **Vanilla Targets:** Use standard movement types like `PingPongMovement`, `Rotator`, `CorkscrewMovement`, `FigureEightMovement`, `FishSwimMovement`, `SwoopAndRetreatMovement`, or `None` (Static). These spawn individual standard models.
*   **Spline Swarms:** Use `SplinePathAsset`. This spawns an entire pre-baked swarm of enemies on a custom track.
*   **Boss Encounters:** Use `BossDragonAsset`. This spawns a massive, multi-phase boss.

> [!danger] CRITICAL RULE FOR BOSSES
> To spawn exactly **one** boss, you must have exactly **one** row in your CSV using `BossDragonAsset` for that wave.
> Do not copy/paste multiple `BossDragonAsset` rows unless you explicitly want a chaotic 2v1 or 3v1 boss fight!

---

## 2. How The System Knows Which Prefab To Use (The Internal Logic)

> [!info] The Problem with CSVs
> A CSV is just a text file. It cannot hold 3D models or Unity Prefabs. The game needs a way to connect the text "BossDragonAsset" to the actual 3D Boss Dragon file.

Here is exactly how the system connects your text file to the game files:

```mermaid
graph TD
    A[LevelDesign_Template.csv\n(Text File)] -->|Imported By| B(CSV Level Importer Window)
    C[Project Folder\n(Unity Prefab Files)] -->|Auto-Find Button| B
    B -->|Generates| D[(LevelConfigSO\nScriptable Object)]
    D -->|Holds Data & Prefabs| E{TrainingLevelManager}
    E -->|Spawns| F[Game Scene]
```

*   **The Middleman (LevelConfigSO):** When you run the `CSV Level Importer`, it creates a Scriptable Object (`LevelConfigSO`) for each level. This object acts as the bridge. It holds the text data from the CSV *and* the actual physical references to the Unity Prefabs.
*   **How the Importer Connects Them:**
    1. Open the CSV Importer (`Archery Range -> CSV Level Importer`).
    2. Click the **Auto-Find Prefabs in Project** button.
    3. **What happens internally:** The script searches your entire Unity project folder for prefabs containing specific names. It looks for a file containing "Target" for the Vanilla slot, "Spline" for the Swarm slot, and "Boss" for the Dragon slot.
    4. It then locks those files into the Importer window.
    5. When you click **Generate Levels & Waves**, the importer saves those exact prefab files into the newly created `LevelConfigSO` files.
*   **The Final Step (Runtime):** When you play the game, the `TrainingLevelManager` reads the `LevelConfigSO`. If the CSV says "Spawn a BossDragonAsset", the Manager looks at the `bossDragonPrefab` slot in the `LevelConfigSO` and spawns whatever object is sitting there.

---

## 3. Setting Up the CSV Exactly Right

> [!tip] Scroll horizontally to view the full CSV data examples below!

Here is the exact column breakdown for your `LevelDesign_Template.csv`:

| Column Name | What It Does | Example |
| :--- | :--- | :--- |
| **LevelName** | Groups waves together into a single level. | `Level1_Intro` |
| **LevelIntroText** | Text shown at the start of the level. | `Get your bow ready!` |
| **LevelOutroText** | Text shown at the end of the level. | `Great job!` |
| **WaveName** | Groups multiple targets into a single wave. | `Wave1` |
| **ProgressionType** | `ClearAllTargets` (Wait until everything is dead) or `TimeBased` (Wait until the timer runs out). | `ClearAllTargets` |
| **WaveDuration** | How many seconds the wave lasts (Only used if `TimeBased`). | `30` |
| **SpawnDelay** | How long (in seconds) to wait before spawning this specific row. | `5` (Wait 5 seconds) |
| **PosX & PosZ** | The relative spawn position from the center. (Y is always kept above ground). | `5, 10` |
| **ScaleModifier** | Makes targets smaller (harder to hit) or larger. | `0.5` (50% size) |
| **SpeedModifier** | Makes vanilla movement scripts faster. | `1.5` (50% faster) |
| **MovementBehavior**| Defines what gets spawned (See Core Target Types). | `BossDragonAsset` |
| **RequiredElement** | Forces the player to use specific arrows. | `Fire` |

---

## 4. Exciting Encounter Recipes (CSV Examples)

Here are distinct examples of how to combine these settings for dynamic gameplay.

### Recipe 1: The Warmup (Static & Moving Mix)
A simple "Clear all targets" wave. Some sit still, some move, and one is tiny and fast.

> [!example] Warmup CSV Data
> | LevelName | LevelIntroText | LevelOutroText | WaveName | ProgressionType | WaveDuration | SpawnDelay | PosX | PosZ | ScaleModifier | SpeedModifier | MovementBehavior | RequiredElement |
> | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
> | Level1_Warmup | Get your eye in! | Nice shooting. | Wave1 | ClearAllTargets | 0 | 0 | -5 | 10 | 1 | 1 | None | Normal |
> | Level1_Warmup | Get your eye in! | Nice shooting. | Wave1 | ClearAllTargets | 0 | 0 | 5 | 10 | 1 | 1 | None | Normal |
> | Level1_Warmup | Get your eye in! | Nice shooting. | Wave1 | ClearAllTargets | 0 | 0 | 0 | 15 | 1 | 1 | PingPongMovement | Normal |
> | Level1_Warmup | Get your eye in! | Nice shooting. | Wave1 | ClearAllTargets | 0 | 2 | 0 | 25 | 0.5 | 2 | CorkscrewMovement | Normal |

### Recipe 2: Elemental Ambush (Time Survival)
The player must survive for 30 seconds against a horde.

> [!example] Elemental Ambush CSV Data
> | LevelName | LevelIntroText | LevelOutroText | WaveName | ProgressionType | WaveDuration | SpawnDelay | PosX | PosZ | ScaleModifier | SpeedModifier | MovementBehavior | RequiredElement |
> | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
> | Level2_Ambush | Survive for 30s! | Well done! | Wave1 | TimeBased | 30 | 0 | 0 | 10 | 1 | 0 | SplinePathAsset | Normal |
> | Level2_Ambush | Survive for 30s! | Well done! | Wave1 | TimeBased | 30 | 5 | -10 | 15 | 1.5 | 1 | SwoopAndRetreatMovement | Fire |
> | Level2_Ambush | Survive for 30s! | Well done! | Wave1 | TimeBased | 30 | 5 | 10 | 15 | 1.5 | 1 | SwoopAndRetreatMovement | Ice |

### Recipe 3: The True Boss Fight
An epic battle where the boss watches the player fight minions, then engages.

> [!example] Boss Fight CSV Data
> | LevelName | LevelIntroText | LevelOutroText | WaveName | ProgressionType | WaveDuration | SpawnDelay | PosX | PosZ | ScaleModifier | SpeedModifier | MovementBehavior | RequiredElement |
> | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- | :--- |
> | Level3_Boss | The Dragon Watches! | You survived! | Wave1 | ClearAllTargets | 0 | 0 | 0 | 10 | 1 | 1 | SplinePathAsset | Normal |
> | Level3_Boss | The Dragon Watches! | You survived! | Wave1 | ClearAllTargets | 0 | 0 | 0 | 10 | 1 | 1 | BossDragonAsset | Normal |

---

## 5. How to Fix Multiple Bosses Spawning

> [!bug] Seeing 2 or 3 dragons stacked on top of each other?
> This means you have a missing or incorrect prefab in your level configuration, causing the system to spawn the wrong objects.

**The Fix:**
You must fix the prefab assignments by letting the computer find them automatically.

1. Click `Archery Range -> Run Level Diagnostics` at the top of the Unity window to read the `LevelDiagnosticReport.txt` and see exactly which level is broken.
2. Open the CSV Level Importer (`Archery Range -> CSV Level Importer`).
3. Click the `Auto-Find Prefabs in Project` button to assign the Vanilla, Spline, and Boss prefabs automatically.
4. Click `Generate Levels & Waves`.
5. Click `Archery Range -> Run Level Diagnostics` again.
6. Verify there are no WARNINGS in the text file.

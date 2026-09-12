# Archery Range System Setup & Level Guide 🎯

This document outlines how to set up the data-driven level progression system within your Unity scene and provides 8 different level concepts to demonstrate how flexible the system is.

## 1. System Setup

### A. Creating the Data Files (Spreadsheet Workflow)
To ensure accessibility and an easy "at-a-glance" workflow, the entire level progression is driven by a single spreadsheet. **You do not need to manually create ScriptableObjects in the Inspector.**

1. **Prepare your CSV Spreadsheet:**
   - Use Google Sheets, Excel, or open `LevelDesign_Template.csv`.
   - Ensure the columns perfectly match the template (LevelName, WaveName, WaveIntroText, WaveOutroText, SpawnDelay, PosX, PosZ, ScaleModifier, SpeedModifier, MovementBehavior, RequiredElement).
   - Every row defines exactly one target.

2. **Generate the Game Data:**
   - In the top Unity menu, click **Archery Range -> CSV Level Importer**.
   - Drag your `.csv` file into the prompt.
   - Drag your **Vanilla Target Prefab** (which has the `MovingTarget` component) into the prompt.
   - Click **Generate Levels & Waves**.
   - The importer will automatically create or update all `LevelConfigSO` and `WaveDataSO` assets inside `Assets/Data/`.

### B. Setting Up the Scene (The Game Loop)
1. Create an Empty GameObject in the hierarchy named `TrainingLevelManager` and attach the **Training Level Manager** component.
2. Open the **Level Playlist** array on the component.
3. Drag your generated Level Config ScriptableObjects into this array in the exact order you want them played (e.g., Element 0 = Level1_Intro, Element 1 = Level2_Gallery, Element 2 = Level3_Boss). The manager will seamlessly transition between them.
4. Create an Empty GameObject to serve as the physical center of the spawn area (e.g., `SpawnCenterPoint`).
   * **VR Note:** Targets can spawn in 360 degrees around this point depending on the X/Z offsets in the Wave Data. The system automatically ensures they never spawn below the Y-level of this center point and always rotates them upright to face this center point.
5. Drag `SpawnCenterPoint` into the **Spawn Center** slot on the Manager.
6. **VR UI Setup (The "Jumbo Tron"):** Standard Screen Space UI causes motion sickness in VR. You must use World Space UI.
   - Right-click in the Hierarchy and select **UI -> Text - TextMeshPro**.
   - Select the Canvas that is automatically generated and change its **Render Mode** to **World Space**.
   - Scale the Canvas down significantly (e.g., `X: 0.01, Y: 0.01, Z: 0.01`) and position it high above the `SpawnCenterPoint` like a floating billboard.
   - Duplicate the Text element so you have two.
   - Name the top one "Announcer Text" and drag it into the Manager's **Level Feedback Text** slot.
   - Name the bottom one "HUD Text" and drag it into the Manager's **HUD Text** slot.
7. Add a physical object (like a 17th-century Gong model) to your scene. Attach the `LevelAdvanceGong` script to it and ensure it has a valid Collider. Link your `TrainingLevelManager` to its script slot.

---

## 2. The Game Loop Flow
1. **Level Start:** The Announcer text shows the Level's Intro Text. It waits infinitely.
2. **Ready Up:** The player shoots the Gong. The text disappears and the fast-paced waves begin.
3. **The Waves:** Waves flow seamlessly into one another instantly with no text interruptions. The HUD text updates in real-time.
4. **Level Complete:** When all waves finish, the Announcer shows the Level Outro Text and the player's completion time. It waits infinitely.
5. **Next Level:** The player shoots the Gong, and the cycle repeats for Level 2.

---

## 3. Walkthrough: 8 Level Concepts

Use these 8 concepts to build a progressively challenging archery sandbox!

### Level 1: The Basics (Intro)
* **Goal:** Teach the player how to shoot and introduce the color-matching mechanic.
* **Waves:** 3
* **Setup:**
  - **Wave 1:** 1 static target (Normal color) at `Z = 10`.
  - **Wave 2:** 2 static targets (Normal color) at `X = -5, Z = 10` and `X = 5, Z = 10`.
  - **Wave 3:** 1 static target (Fire color) at `Z = 10` to introduce switching arrows.
* **Global Modifiers:** Scale = 1.2 (Extra large targets), Speed = 1.0.

### Level 2: Target Practice (Static Distance)
* **Goal:** Test accuracy at different depths.
* **Waves:** 2
* **Setup:**
  - **Wave 1:** 3 targets spawned at `Z = 5`, `Z = 15`, and `Z = 25`. Delay them by `1.0s` each so they appear one by one.
  - **Wave 2:** Same as Wave 1, but set the `scaleModifier` on the furthest target to `0.5` (very small).

### Level 3: The Shooting Gallery (Basic Movement)
* **Goal:** Introduce moving targets.
* **Waves:** 2
* **Setup:**
  - **Wave 1:** 2 targets at `Z = 15`. Add the string `PingPongMovement` to the Movement Script Name. Set speed modifier to `1.0`.
  - **Wave 2:** 3 targets at `Z = 20`. `PingPongMovement`, but set the required elements to Fire, Ice, and Electric to force the player to think while aiming.

### Level 4: The 180-Degree Arc
* **Goal:** Force the player to look around their standard forward FOV.
* **Waves:** 3 (Time-Based: 30 seconds each)
* **Setup:**
  - Populate the waves with 10 targets each, setting `spawnDelay` in increments (0s, 2s, 4s, etc.).
  - Set the `spawnPosition` of these targets in a wide arc: `X = -15, Z = 5` (Far Left), `X = 15, Z = 5` (Far Right), etc.
  - Since the manager forces targets to face the center, the player can just turn their head and shoot.

### Level 5: The Swarm (Time-Based Survival)
* **Goal:** High pressure, rapid firing.
* **Waves:** 1 (Time-Based: 60 seconds)
* **Setup:**
  - Configure 20 targets. Give them a tiny `scaleModifier` (0.6) and a high `speedModifier` (2.0) with `CorkscrewMovement` or `FishSwimMovement`.
  - Stagger their `spawnDelay` tightly (every 1.5 seconds).
  - The player must hit as many as possible before the 60 seconds run out.

### Level 6: The 360-Degree Ambush
* **Goal:** For active/standing VR players. Targets appear from behind.
* **Waves:** 2
* **Setup:**
  - **Wave 1:** 4 targets. Positions: `Z = 10` (Front), `Z = -10` (Behind), `X = 10` (Right), `X = -10` (Left).
  - Give the front target a `0s` delay, and the one behind a `3s` delay, forcing the player to turn around when they hear the spawn sound.

### Level 7: The Elemental Puzzle
* **Goal:** Master the color/material mechanic under pressure.
* **Waves:** 4
* **Setup:**
  - Each wave spawns exactly 1 target, but it moves erratically (`FigureEightMovement`).
  - **Wave 1:** Needs Fire. **Wave 2:** Needs Sticky. **Wave 3:** Needs Stasis.
  - Set the `globalScaleMultiplier` to `0.7` so they are small and hard to hit while swapping arrows.

### Level 8: The Grand Finale (Boss Wave)
* **Goal:** Everything at once.
* **Waves:** 1 (Clear All Targets)
* **Setup:**
  - Spawn 4 "Minion" targets at `Z = 10` with `spawnDelay = 0`. Give them normal colors and `PingPongMovement`.
  - Spawn 1 "Boss" target at `Z = 20` with `spawnDelay = 5.0`.
  - Make the boss massive (`scaleModifier = 3.0`), extremely fast (`speedModifier = 3.0`), require an `Electric` arrow, and use `Rotator` or `SwoopAndRetreatMovement`.
  - The player has to clear the minions before the boss overwhelms them!
# Archery Range System Setup Instructions 🎯

This document outlines how to set up the data-driven level progression system within your Unity scene using the new `TrainingLevelManager` and its corresponding ScriptableObjects.

## 1. Creating the Data Files (Spreadsheet Alternative)

We use ScriptableObjects natively in Unity to avoid parsing CSV files at runtime, which allows you to drag-and-drop prefabs and tweak settings directly in the Inspector.

### A. Create Wave Data Profiles
1. In the Unity Project window, right-click in any folder (e.g., `Assets/Data/Waves`).
2. Select **Create -> Archery Range -> Wave Data**.
3. Name it (e.g., `Wave1_Easy`).
4. In the Inspector:
   - Choose the **Progression Type** (`ClearAllTargets` or `TimeBased`).
   - Add elements to the **Targets** list.
   - For each target, specify the relative spawn delay, spawn position (relative to the Manager's center), required arrow element, scale modifier, and speed modifier.
   - Set **Movement Script Name** to the EXACT class name of the movement script you want to inject (e.g., `PingPongMovement`). Leave it blank for a static target.

### B. Create Level Configuration
1. Right-click in the Project window (e.g., `Assets/Data/Levels`).
2. Select **Create -> Archery Range -> Level Config**.
3. Name it (e.g., `Level1_Intro`).
4. In the Inspector:
   - Assign your **Vanilla Target Prefab** (must have the `MovingTarget` script attached).
   - Drag and drop the `Wave Data` profiles you created in step A into the **Waves** list, in the order you want them to appear.
   - Adjust the **Global Scale Multiplier** and **Global Speed Multiplier** if you want to quickly make the whole level harder or easier without editing individual waves.

## 2. Setting Up the Scene

1. Open your Archery Range Scene.
2. Create an Empty GameObject in the hierarchy and name it `TrainingLevelManager`.
3. Add the **Training Level Manager** component to it.
4. Drag your `Level1_Intro` ScriptableObject into the **Current Level Config** slot on the component.
5. Create another Empty GameObject to serve as the physical center point where targets should spawn around (e.g., `SpawnCenterPoint`).
6. Drag `SpawnCenterPoint` into the **Spawn Center** slot on the `TrainingLevelManager` component.

## 3. How It Works at Runtime

- On `Start()`, the Manager reads the Level Config and starts the first Wave in the list.
- It iterates through the Targets in the Wave Data.
- For each target, it waits for the specified `SpawnDelay`, then instantiates the Vanilla Target Prefab.
- It calculates the final scale (Global * Local) and final speed (Global * Local).
- It applies the scale, notes the required arrow element, and uses C# Reflection to search all loaded assemblies for a script matching the `Movement Script Name`.
- It dynamically adds that movement script to the target and sets its speed property.
- When the progression conditions are met (all targets destroyed or time runs out), it clears any remaining objects, stops active spawning routines, and advances to the next wave after a brief delay.
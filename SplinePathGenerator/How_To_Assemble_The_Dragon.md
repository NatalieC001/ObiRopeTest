# 🐉 How to Assemble the Asian Dragon Boss

This tutorial will guide you step-by-step through building the Segmented Asian Dragon from complete scratch using Unity primitives (like Cubes) or your final 3D models.

By the end of this, you will have a slithering dragon that dynamically shrinks and closes gaps when the player shoots its body segments with arrows!

---

## Phase 1: Building the Body Parts (The Prefabs)

Before we can build the whole dragon, we need to make the individual pieces: the **Head**, the **Body**, and the **Tail**.

**CRITICAL ARCHITECTURE RULE:** We *never* put scripts directly on 3D meshes. If you put scripts on a Cube, it is incredibly difficult to swap that Cube out for a real Dragon 3D model later. We always use the **"Empty Root"** pattern.

### Step 1: Create the Root Object (The Identity)
1. Right-click in your Hierarchy and choose `Create Empty`. Name it **"Dragon_Body"**.
2. **The Physics Anchor:** Click `Add Component` and add a **`Rigidbody`**.
   * *Crucial:* Check the box for **`Is Kinematic`**. This ensures the segment can be hit by your game's physical arrows, but gravity won't pull it off the spline track!
3. **The Brain:** Click `Add Component` and search for **`DragonSegment`**.
   * Set the Health (e.g., `100`).
   * Set the Power Contribution (e.g., `10`).
4. **The Visual Effects:** Click `Add Component` and search for **`DissolveEffect`**.
5. **The Spline Follower Warning:** When you added the `DragonSegment` script, Unity automatically added a `SplineFollower` component. **Do not worry about the blank "Spline" field.** The Manager fills this in at runtime!

### Step 2: Add the Visuals and Colliders (The Children)
Now we add the replaceable parts as children to the Root.
1. Right-click your **"Dragon_Body"** root object and choose `3D Object -> Cube`. Name this child **"Mesh_Visual"**.
2. Remove the `BoxCollider` from this child (we want the physics on a separate node, or managed explicitly).
3. Right-click the Root object again and choose `Create Empty`. Name it **"Collider_Node"**. Add a `BoxCollider` to this and size it to fit the cube.
4. **Wire it up:** Go back to your Root object. Drag the **"Mesh_Visual"** into the `Target Renderer` slot on your `DissolveEffect` script. Drag the **"Collider_Node"** into any relevant slots on your scripts.

*Why do we do this?* Tomorrow, when your artist gives you a 3D Dragon model, you just delete the "Mesh_Visual" cube, drop the 3D model in as a child, and resize the "Collider_Node". You don't have to touch a single script!

### Step 3: Configure Destructibility
It would be very gory (and mechanically broken) if the player could destroy the Dragon's Head or Legs while the body was still alive! We want players to shoot the body segments to shrink the dragon's power, but we want the Head, Legs, and Tail to be permanent until the final boss dies.
1. On your **"Dragon_Body"** prefab, ensure the `Is Destructible Part` checkbox on the `DragonSegment` script is **Checked (True)**.
2. For your Head, Legs, and Tail prefabs, you must **Uncheck (False)** this box. When the player shoots the head, the damage will still hurt the overall Boss Health Bar, but the Head itself won't be destroyed mid-fight!

### Step 4: Save the Prefabs
1. Drag the **"Dragon_Body"** root object from your Hierarchy down into `Assets/SplinePathGenerator/Scripts/Entities/Dragon/Prefabs` to save it as a **Prefab**.
2. Delete it from the scene.
3. Repeat this process to create your **"Dragon_Head"**, **"Dragon_FrontLegs"**, **"Dragon_BackLegs"**, and **"Dragon_Tail"** prefabs (Remembering to uncheck `Is Destructible Part` for these!).

---

## Phase 2: Building the Boss Root (The Manager)

Now we will create the "Brain" and "Skeleton" that controls the pieces we just made.

1. Right-click in the Hierarchy and choose **Create Empty**. Name it **"Boss_AsianDragon"**.
2. **The Brain:** Click `Add Component` and add **`BossCreature`**.
   * *Note: This will automatically attach the `TacticalBossSplineManager` for you!*
3. **The Skeleton:** Click `Add Component` and add **`SegmentedDragonManager`**.
4. Look at the `SegmentedDragonManager` in the Inspector. You will see slots for the anatomy:
   * Drag your **Dragon_Head** prefab into the `Head Prefab` slot.
   * Drag your **Dragon_FrontLegs** prefab into the `Front Legs Prefab` slot.
   * Drag your **Dragon_Body** prefab into the `Body Prefab` slot.
   * Drag your **Dragon_BackLegs** prefab into the `Back Legs Prefab` slot.
   * Drag your **Dragon_Tail** prefab into the `Tail Prefab` slot.
5. Set `Number Of Body Segments` to something fun (like `8`). This dictates how many standard body pieces exist *between* the front and back legs.
6. Set `Segment Spacing` to control how far apart the cubes float (e.g., `1.5`).
7. **Save the Boss:** Drag the "Boss_AsianDragon" object down into your `Entities/Dragon/Prefabs` folder to save the complete boss asset!
8. Delete it from the scene (it should only exist as a prefab waiting to be spawned).

---

## Phase 3: Setting Up The Boss Arena (The Scene Environment)

Because the Boss is saved as a Prefab in your project folder, it **cannot** save direct references to objects that exist in your specific level (like a pillar to wrap around). We use a Scene Manager to hold these environment pieces.

1. Open your Boss Level scene.
2. Right-click the Hierarchy and choose **Create Empty**. Name it **"BossArenaManager"**.
3. Add the **`BossArenaManager`** script to this object.
4. **Draw the Observation Spline:**
   * Create an Empty GameObject and name it **"Observation_Spline"**.
   * Add a `SplineComputer` component to it and draw a circular path high in the sky. (This is where the boss waits/charges during Phase 1).
5. **Draw the Escape Routes:**
   * *Important Rule:* In Dreamteck, **One SplineComputer = One Track**. To have multiple escape routes, you need multiple objects!
   * Create an Empty GameObject, name it **"Escape_Route_1"**, add a `SplineComputer`, and draw a path wrapping around a pillar.
   * Create another Empty GameObject, name it **"Escape_Route_2"**, add a `SplineComputer`, and draw a path diving through a cloud.
6. **Wire it to the Arena Manager:**
   * Click on your **"BossArenaManager"**.
   * Drag the "Observation_Spline" object into the `Observation Spline` slot.
   * Drag your "Escape_Route_1" and "Escape_Route_2" objects into the `Tactical Escape Routes` array.

---

## Phase 4: Spawning the Encounter (The Lifecycle)

Here is how you actually bring the boss to life during gameplay without writing any custom code! Everything is handled by the `TrainingLevelManager` and your CSV sheet.

1. **Assign your Prefabs:** Open `LevelConfigSO` in your Inspector.
   * Drag your Minion Swarm prefab (created via the Spline Generator) into the `Spline Path Asset Prefab` slot.
   * Drag your **"Boss_AsianDragon"** prefab into the `Boss Dragon Prefab` slot.
2. **Setup the CSV:** Open your `LevelDesign_Template.csv`.
   * Create a wave row. In the `MovementBehavior` column, type `SplinePathAsset` to spawn your minion swarm.
   * Create a second wave row (or use the same one!). Type `BossDragonAsset` to spawn your dragon.
3. **The Magic:** When you start the level, the `TrainingLevelManager` reads those strings.
   * It instantiates the Boss prefab.
   * It dynamically locates your `BossArenaManager` in the scene.
   * It gathers up all the spawned minions.
   * It automatically wires them all together using `RegisterBattleParticipants()`!

As soon as that happens:
1. The Boss retrieves the environmental splines from the Arena.
2. The Boss tells the `SegmentedDragonManager` to spawn the visual Head, Body, Legs, and Tail.
3. The Boss instantly snaps to the `Observation_Spline` and begins Phase 1 (Orchestrating)!

---

## Phase 3: Giving it a Track (The Spline)

The dragon needs a track to ride on so it can slither.

1. Right-click on your **"Boss_AsianDragon"** root object and choose **Create Empty**. Name this child object **"Dragon_Track"**.
2. Click `Add Component` on the child and add a **`SplineComputer`**.
3. Use the Dreamteck tools to draw a quick wavy line or circle in the sky.

### Wiring it Together
Now we just need to tell the Dragon where its track is.
Normally, your wave manager (`BossArenaManager`) will do this automatically via code when the boss spawns!

But if you are testing manually in the editor:
1. Create an empty script or a simple Unity Event that calls `GetComponent<SegmentedDragonManager>().InitializeDragon( track )`.
2. As soon as `InitializeDragon` runs, the manager will spawn the Head, spawn 8 Bodies, and spawn 1 Tail.
3. It will perfectly space them out along the wavy track you drew!

---

## 🎯 How the Combat Works (Under the Hood)

Here is exactly what happens when a player successfully hits the dragon:

1. **The Hit:** An arrow hits the BoxCollider on segment #4.
2. **The Damage:** The arrow script calls `TakeDamage()` on segment #4's `DragonSegment` script.
3. **The Death:** Segment #4's health hits 0. It triggers the `DissolveEffect` to visually burn away the cube.
4. **The Notification:** Right before it dies, segment #4 shouts to the `SegmentedDragonManager`: *"I am destroyed!"*
5. **The Gap Close:** The Manager recalculates the math. It realizes there is a hole in the dragon. It uses DOTween to smoothly slide segments #5, #6, #7, #8, and the Tail forward to close the gap.
6. **The Weakening:** The Boss's overall power is reduced, bringing you one step closer to victory!
# 🐉 How to Assemble the Asian Dragon Boss

This tutorial will guide you step-by-step through building the Segmented Asian Dragon from complete scratch using Unity primitives (like Cubes) or your final 3D models.

By the end of this, you will have a slithering dragon that dynamically shrinks and closes gaps when the player shoots its body segments with arrows!

---

## Phase 1: Building the Body Parts (The Prefabs)

Before we can build the whole dragon, we need to make the individual pieces: the **Head**, the **Body**, and the **Tail**.

### Step 1: Create a Body Segment
1. Right-click in your Hierarchy and choose `3D Object -> Cube`. Name it **"Dragon_Body"**.
2. **The Collider:** Ensure it has a `BoxCollider` (or `MeshCollider`) so the player's arrows can physically hit it.
3. **The Health Script:** Click `Add Component` and search for **`DragonSegment`**.
   * Set the Health (e.g., `100`).
   * Set the Power Contribution (e.g., `10`).
4. **The Visual Effects:** Click `Add Component` and search for **`DissolveEffect`**. (This existing project script will make the segment burn away smoothly when destroyed).
5. Drag the **"Dragon_Body"** object from your Hierarchy down into `Assets/SplinePathGenerator/Scripts/Entities/Dragon/Prefabs` to save it as a **Prefab**.
6. Delete the cube from the scene.

### Step 2: Create the Head and Tail
Repeat the exact same process above to create a **"Dragon_Head"** prefab and a **"Dragon_Tail"** prefab.
*(Tip: Make the Head cube slightly larger, and the Tail cube slightly smaller so you can tell them apart while testing!)*

---

## Phase 2: Building the Boss Root (The Manager)

Now we will create the "Brain" and "Skeleton" that controls the pieces we just made.

1. Right-click in the Hierarchy and choose **Create Empty**. Name it **"Boss_AsianDragon"**.
2. **The Brain:** Click `Add Component` and add **`BossCreature`**.
   * *Note: This will automatically attach the `TacticalBossSplineManager` for you!*
3. **The Skeleton:** Click `Add Component` and add **`SegmentedDragonManager`**.
4. Look at the `SegmentedDragonManager` in the Inspector. You will see three empty slots for the anatomy:
   * Drag your **Dragon_Head** prefab into the `Head Prefab` slot.
   * Drag your **Dragon_Body** prefab into the `Body Prefab` slot.
   * Drag your **Dragon_Tail** prefab into the `Tail Prefab` slot.
5. Set `Number Of Body Segments` to something fun (like `8`).
6. Set `Segment Spacing` to control how far apart the cubes float (e.g., `1.5`).
7. **Save the Boss:** Drag the "Boss_AsianDragon" object down into your `Entities/Dragon/Prefabs` folder to save the complete boss asset!

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
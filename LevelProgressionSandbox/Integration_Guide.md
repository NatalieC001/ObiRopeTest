# Level Progression Sandbox Integration Guide

> [!info] The Goal
> This guide explains exactly how to safely test the new **Event-Driven Progression System** in your main game scene without permanently deleting the old spaghetti code. Once you test this and confirm it works, you can safely delete the old scripts.

---

### Step 1: Disable the Old System
We need to turn off the old God Class so it doesn't fight with the new system.
1. In your Unity Hierarchy, locate the GameObject that holds `TrainingLevelManager.cs`.
2. Click the **Checkbox** next to the `TrainingLevelManager` script in the Inspector to **disable it** (do NOT delete it yet, just uncheck it).

---

### Step 2: Set Up the New "Progression Manager" Object
We will create a brand new, clean GameObject to hold our new system.

1. Right-click in the Hierarchy and select **Create Empty**.
2. Name this new object: `[MANAGER] _LevelProgression_NEW`.
3. Select this new object and click **Add Component** in the Inspector. Add the following scripts from the `LevelProgressionSandbox` folder:
   - `LevelProgressionManager`
   - `WaveSpawner`
   - `LevelUIManager`

> [!tip] Keeping it together
> Having all three scripts on one GameObject makes it very easy to find and manage.

---

### Step 3: Wire Up the Scripts in the Inspector

Now, select `[MANAGER] _LevelProgression_NEW` and fill in the missing fields in the Inspector:

**Level Progression Manager:**
*   **Level Playlist:** Copy exactly what was in your old `TrainingLevelManager` list. Drag in your `LevelConfigSO` files (like Level 1, Level 2) here.

**Wave Spawner:**
*   **Spawn Center:** Drag the exact same Transform (usually the middle of the arena) that you used for the old manager.

**Level UI Manager:**
*   **Level Feedback Text:** Drag your giant Announcer TMP_Text object from the Canvas here.
*   **HUD Text:** Drag your smaller Target Counter TMP_Text object here.

---

### Step 4: Fix the Gong
The physical Gong in the scene needs to talk to the new system, not the old one.

1. Locate your Gong object in the Hierarchy.
2. It should have the old `LevelAdvanceGong` script attached to it.
3. Because the new script has the exact same name, Unity might have automatically updated it. Look at the script in the Inspector.
4. If it has a slot asking for **Level Manager** (the old version), you need to replace it. Remove the script and add the new `LevelAdvanceGong.cs` from the `LevelProgressionSandbox` folder.
5. You should now see a slot asking for **Progression Manager**. Drag your new `[MANAGER] _LevelProgression_NEW` object into this slot.

---

### Step 5: What About Minions?
If you rely on a `MinionManager` or a `BossArenaManager` to track kills, those systems operate entirely independently of this new progression loop!
* Your Minions will still spawn (because `WaveSpawner` handles the physical instantiation).
* Your Minions will still die and trigger their dissolves.
* `WaveSpawner` simply counts how many targets are physically left in the scene to know when the wave is done.

> [!info] Boss Fights
> Bosses are now tracked seamlessly just like standard minions! When the Boss's destructible colliders on the `Enemy` layer are completely destroyed, the spawner will automatically trigger the Boss Defeated progression state.

---

### Step 6: Test and Verify
1. Press Play.
2. The Announcer Text should immediately say "Level 1... Shoot the Gong to begin!"
3. Shoot the Gong *ONCE*.
4. You should see a clean 3, 2, 1 Countdown.
5. The text will vanish and the wave will begin instantly.
6. When you clear all targets, the next wave starts instantly.

If this works smoothly, congratulations! You can now safely delete `TrainingLevelManager.cs` from your project forever.

---

### Step 7: Minion & Swarm Prefab Blueprints
To ensure your standard grunts and nested swarms can be shot by arrows, correctly drain health, and cleanly notify the progression manager when they die without leaving ghost colliders, your prefabs MUST look like this:

#### Blueprint A: Single Minion
This is for a standard enemy that rides a spline on its own.

```text
👹 [Prefab Root] Minion_Basic
 ├── 📜 StandardCreature.cs       (The brain: tracks health and registers to MinionManager)
 ├── 📜 SplineFollower.cs         (Handles movement along the path)
 ├── 📜 CreatureStatusEffects.cs  (Handles Ice/Slow elemental reactions)
 │
 ├── 🧩 [Child] Mesh_Visual
 │    ├── 🎨 SkinnedMeshRenderer
 │    └── 📜 DissolveEffect.cs    (Visual death. Ensure 'Dissolve Immediately On Hit' is OFF!)
 │
 └── 🧩 [Child] Collider_Node     (Layer MUST be 'Enemy')
      ├── 📜 BoxCollider
      └── 📜 Rigidbody            (Kinematic)
```

> [!warning] Arrow Detection
> For `StandardCreature` to receive arrow damage, it is best practice to either place the `Collider` directly on the Root object, or ensure your Arrow scripts use `GetComponentInParent<IArrowTarget>()` when hitting a child collider.

#### Blueprint B: Nested Swarm
This is for a single prefab that spawns multiple enemies riding the same spline in a formation.

```text
🐝 [Prefab Root] Swarm_Of_Bees
 ├── 📜 SplineSwarmManager.cs     (Manages the local swarm spacing/timing)
 ├── 📜 SplineFollower.cs         (The master follower moving the entire swarm group)
 │
 ├── 🧩 [Child] Bee_1             (This is the actual killable entity!)
 │    ├── 📜 StandardCreature.cs
 │    ├── 📜 CreatureStatusEffects.cs
 │    ├── 🧩 Mesh_Visual
 │    │    └── 📜 DissolveEffect.cs
 │    └── 🧩 Collider_Node (Layer: Enemy)
 │
 ├── 🧩 [Child] Bee_2
 │    ├── 📜 StandardCreature.cs
 │    ├── 📜 CreatureStatusEffects.cs
 │    ├── 🧩 Mesh_Visual
 │    │    └── 📜 DissolveEffect.cs
 │    └── 🧩 Collider_Node (Layer: Enemy)
```

In a nested swarm, the `WaveSpawner` will dynamically find `Bee_1` and `Bee_2`, registering both of them with the `MinionManager`. When they both die, the `WaveSpawner` will cleanly obliterate the `[Prefab Root] Swarm_Of_Bees` from the scene automatically!
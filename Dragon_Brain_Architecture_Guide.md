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
├── 🟢 DragonMovementManager           ← Tracks breadcrumb history for the body segments
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

## 🧩 Anatomy & Spline Following
The Dragon is not a single mesh; it is a snake-like chain of independent rigidbodies.
* **`DragonMovementManager.cs`** sits on the root object. It constantly records the root's `positionHistory` (a breadcrumb trail of where the head has flown).
* **`SegmentedDragonManager.cs`** owns the actual list of prefabs (Head, Body, Tail). It spawns them, tracks their power contribution, and in `LateUpdate()`, physically drags each segment to its required distance along the `DragonMovementManager`'s breadcrumb trail.

By splitting these two apart, the AI brain (`DragonTick`) and the movement executor (`AirborneBossMovement`) only ever have to steer the single root GameObject. The 20+ body segments passively slither behind it with zero AI overhead.

---

## 🗺️ Scene-Level Dependencies
The Dragon does not exist in a vacuum. It requires these global managers to exist in the scene hierarchy:

* **`WaveSpawner`**: Spawns the Dragon and holds the `bossMinionRoster`.
* **`MinionManager`**: Tracks when standard creatures die and explicitly notifies the wave spawner.
* **`MinionRequestBroker`**: The Dragon asks this script to spend its reserve and spawn minions.
* **`BossPathManager`**: Holds the `Airborne` and `Terrestrial` paths. The Dragon queries this to find its Observation Splines!
* **`EnvironmentTagRegistry`**: Tracks cover, chokepoints, and crystals for the Dragon to interact with.

---

## 🛠️ Step-by-Step: Authoring the DragonBrain Quest Asset
Follow these exact tables to build your Quest Machine nodes. Do not guess the strings—copy them exactly.

### Step 1: Create the Base Asset
1. Right-click in your Project window -> `Create > Pixel Crushers > Quest Machine > Quest`.
2. Name the file exactly **`DragonBrain`**.
3. Select it. In the Inspector, uncheck **Is Trackable** and **Show In Track HUD**.

### Step 2: The Wiring Table (Message Conditions)
When drawing an arrow connecting one node to another, add a **Message Requirement** condition. Set the **Message** field to `Brain`, and set the **Parameter** field to the exact string below.

| From Node | To Node | Parameter String |
| :--- | :--- | :--- |
| Start | Orchestrator | *(No condition, connects automatically)* |
| Orchestrator | ObservationSpline | *(No condition, connects automatically)* |
| ObservationSpline | Engage | `PlayerShootsBoss` |
| ObservationSpline | Engage | `MinionsBeaten` |
| ObservationSpline | Rage | `CrystalDestroyed` |
| Engage | Evaluate | *(No condition, connects automatically)* |
| Evaluate | Pursue | `DesireDominance` |
| Evaluate | Bait | `DesireTerritory` |
| Evaluate | Spawn | `DesireSpawn` |
| Evaluate | Breath | `DesireElement` |
| Evaluate | Defend | `DesireDefend` |
| Evaluate | Evade | `BurstDamageTaken` |
| Evaluate | Evade | `StaminaEmpty` |
| Evaluate | Rage | `CrystalDestroyed` |
| Swoop | Bank | `SwoopOvershoot` |
| Spawn | Herd | `TagChokepoint` |
| Spawn | Flank | `TagCover` |
| Breath | Topple | `TagTopple` |
| Defend | Engage | `CrystalSafe` |
| Defend | Rage | `CrystalDestroyed` |
| RechargeLoop | Engage | `RechargeFull` |
| RechargeLoop | Rage | `RechargeFullLostCrystal` |
| RageEval | Relentless | `SegmentsIntact` |
| RageEval | Regen | `SegmentsMissing` |
| Relentless | Exhaust | `StaminaEmpty` |
| Absorb | Engage | `SegmentsRestored` |
| Absorb | Desperate | `NoCrystalsLeft` |

### Step 3: The Action Table (Message Actions)
When the Brain enters a specific node, it must tell the Dragon to move.
Select the Node. In the **Active State** actions list, add a **Message Action**. Set the **Message** to `DragonActions`, and set the **Parameter** to the exact string below.

| Target Node | Parameter String |
| :--- | :--- |
| Pursue | `Pursuit` |
| Swoop | `Swoop` |
| Bank | `Bank` |
| Bait | `Bait` |
| Herd | `Herd` |
| Spawn | `SpawnWave` |
| Flank | `Flank` |
| Breath | `Breath` |
| Topple | `Topple` |
| Defend | `DefendCrystal` |
| Evade | `Evade` |
| Exhausted | `Exhausted` |
| Recharge | `Recharging` |
| Rage | `Rage` |
| Relentless | `Relentless` |
| Regen | `Regenerate` |
| Desperate | `Desperate` |

### Step 4: Resetting Loops
When a sequence finishes (e.g. `Pursue -> Swoop -> Bank`), the Dragon must loop back.
On the final node of a loop (like `Bank`), add a **Set Quest Node State Action**.

| From Node | Target Node | State to Set |
| :--- | :--- | :--- |
| Bank | `Pursue` | Active |
| Flank | `Evaluate` | Active |
| Topple | `Evaluate` | Active |

---

## 🐛 Debugging: Why is the Quest Journal Empty?

When you click the Dragon Boss in the Unity Inspector and look at the `QuestJournal` component, the Quests list will say `List is Empty`. This is expected.

**Here is what is happening:**
The `QuestJournal` component is designed to show quests that were dragged and dropped in the Editor at *design time*. However, if you attach the `DragonBrain` asset to multiple enemies at design time, Quest Machine will share the same logical state across all of them (killing one dragon fails the quest for the other dragon!).

To prevent this, our original code instantiated a **clone** of the asset dynamically during `Start()` and injected it into the Journal. The Unity Inspector does not auto-refresh to show quests that were injected via code after play mode begins.

**The Bigger Problem: We shouldn't use a Quest Journal for a Brain**
Quest Journals are built for RPGs. They remember completed quests, they compress save data, and they permanently flag nodes as "True". A Boss Brain is a **State Machine**, which means it needs to loop endlessly. If it remembers it did a swoop attack once, it might refuse to ever swoop again!

### The Fix: Update your `DragonBrainController.cs`
We are ripping out the `QuestJournal` entirely. Replace the contents of your `DragonBrainController.cs` with the script below. It holds the logic engine in raw memory, skips the RPG UI baggage, and exposes the `runtimeQuestInstance` directly to the Unity Inspector.

**How to watch the Brain think live:**
1. Hit **Play**.
2. Click the live Dragon Boss (the `(Clone)`) in the scene hierarchy.
3. Look at the `DragonBrainController` script in the Inspector.
4. Double-click the **Runtime Quest Instance** field. The Quest Editor window will open, and you will see the nodes lighting up live as the Dragon makes decisions!

```csharp
using UnityEngine;
using PixelCrushers.QuestMachine;

/// <summary>
/// Attached to the Boss Prefab.
/// Automatically instantiates and activates the DragonBrain Quest Machine instance upon spawn.
/// Cleans it up upon death.
/// </summary>
public class DragonBrainController : MonoBehaviour
{
    [Header("Quest Configuration")]
    [Tooltip("The DragonBrain Quest asset generated by the editor script.")]
    public Quest dragonBrainAsset;

    [Header("Runtime (Visible in Play Mode)")]
    [Tooltip("Double-click this field during Play Mode to watch the live flowchart!")]
    public Quest runtimeQuestInstance;

    private void Start()
    {
        if (dragonBrainAsset == null)
        {
            Debug.LogError("[DragonBrainController] Missing DragonBrain Quest asset! The boss has no brain.");
            return;
        }

        // Clone the quest asset so multiple dragons don't share state
        // and so we don't accidentally modify the project asset file during play!
        runtimeQuestInstance = dragonBrainAsset.Clone();

        // We DO NOT use a QuestJournal.
        // QuestJournal is for RPG tracking, UI, and save states ("Remember Completed Quests").
        // This is a State Machine Brain. It needs to be raw and forgetful so it can loop.

        // Initialize the quest directly
        runtimeQuestInstance.SetState(QuestState.Active);
        Debug.Log($"[DragonBrainController] Activated Quest Machine brain for {gameObject.name}");
    }

    private void Update()
    {
        // Without a Journal, we must manually tick the quest if it relies on time-based conditions or internal updates.
        // Normally QuestJournal calls this globally, but since we are running headless, we tick it ourselves.
        if (runtimeQuestInstance != null && runtimeQuestInstance.GetState() == QuestState.Active)
        {
            // runtimeQuestInstance.Update(); // Uncomment if your nodes use time-based wait conditions!
        }
    }

    private void OnDestroy()
    {
        if (runtimeQuestInstance != null)
        {
            // Cleanly shut down the quest graph when the dragon despawns/dies
            runtimeQuestInstance.SetState(QuestState.Successful);
            Debug.Log($"[DragonBrainController] Shut down Quest Machine brain for {gameObject.name}");
        }
    }
}
```

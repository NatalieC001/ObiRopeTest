# Boss AI: Gameplay Flow & System Architecture

This guide explains the life cycle of the Boss AI, focusing on how its interconnected systems (Brain, Senses, Muscles, Paths, and Anatomy) collaborate to create a dynamic, non-linear encounter. The AI does not follow a strict script; instead, it uses a Hub-and-Spoke behavior tree to constantly evaluate its desires and react to the battle.

---

## 1. Scene Load & Wave Initialization

When the player reaches the Boss wave, the level orchestrator begins the sequence.

### **`WaveSpawner.cs`**
- **Action:** Receives the command to start the Boss Wave.
- **Process:**
  1. Identifies the `BossConfig` in the wave data.
  2. Instantiates the configured Observation Paths and Escape Paths (Prefabs).
  3. Registers these paths globally with the `BossPathManager`.
  4. Spawns the `Boss_AsianFireDragonNew` prefab.

### **`BossPathManager.cs`**
- **Role:** The "Air Traffic Controller". It holds a categorized list of all active routes (Airborne Observation, Terrestrial Escape, etc.).

---

## 2. Dragon Anatomy & Positioning

The moment the Dragon spawns, it must assemble itself and locate its starting position.

### **`BossCreature.cs` (Start)**
- **Process:**
  1. Queries the `BossPathManager` for an Airborne Observation path.
  2. Evaluates the physical start point (`startSpline.Evaluate(0.0)`).
  3. Instantly warps the Dragon's root transform to this exact position and rotation.
  4. Commands the `AirborneBossMovement` to begin coiling along this spline.
  5. Instructs `SegmentedDragonManager` to generate the physical body.

### **`SegmentedDragonManager.cs`**
- **Role:** The skeleton builder. Spawns the Head, Body, Legs, and Tail, hooking them up so they slither behind the root object along a shared breadcrumb trail. Fires `OnHeadSpawned`.

### **`DragonActionListeners.cs` (Awake/OnEnable)**
- **Role:** The Muscles. Hears `OnHeadSpawned` and searches the new Head child for the `ElementalBreathController.cs` to arm its breath attacks.

---

## 3. The Brain Awakes

With the body built and placed on the track, the AI takes over.

### **`DragonBrainController.cs`**
- **Process:**
  1. Clones the `DragonBrain_Automated.asset` (The Quest Machine Behavior Tree).
  2. Attaches and registers a `QuestJournal` to the Dragon for debugging.
  3. Forces the state machine to `Active`, dropping the Dragon into the **"Orchestrator"** state.

---

## 4. Non-Linear Decision Making (The Senses)

The AI relies on a 10Hz loop to constantly re-evaluate its environment.

### **`DragonTick.cs` & `DesireEvaluator.cs`**
- **Role:** The Senses.
- **Process:** Constantly monitors the environment (Missing segments? Minions dying? Player dealing burst damage? Crystals threatened?).
- **Desire Evaluation:** `DesireEvaluator` weighs these factors to output the strongest desire. For example:
  - High minion deaths = `DesireDominance` or `DesireSpawn`
  - Crystal under attack = `DesireDefend`
  - Low stamina or high burst damage = `DesireEvade`
- **Messaging:** `DragonTick` uses `MessageSystem.SendMessage("Brain", "<Condition>")` to broadcast these desires to the Quest Machine Brain.

---

## 5. Dynamic Combat Hub (The Brain in Action)

The Quest Machine Behavior Tree is not linear; it is designed as a **Hub and Spoke** system. The `Engaged` node acts as the central hub. Whenever the Dragon finishes an action, it returns to the `Engaged` hub to evaluate its *next* move based on the ever-changing Desires.

### **Scenario: The Hub Evaluation**
The Dragon is in the `Engaged` Hub. `DragonTick` feeds it current desires, opening different branches:

- **Branch A: Attrition (Calling Minions)**
  - If the player is overwhelmed, the Brain receives `DesireSpawn`.
  - The flow transitions to the `Spawn` node, which tells the Muscles: `"SpawnWave"`.
  - The Muscles trigger the `MinionRequestBroker` to call down reinforcements.
  - *Dynamic Follow-up:* Depending on the environment, the Brain evaluates tags (e.g., `TagChokepoint`). It might chain the spawn action into a `Herd` maneuver to push the player into the new minions!

- **Branch B: Pure Aggression**
  - If the player is weak and isolated, the Brain receives `DesireDominance`.
  - The flow transitions to the `Pursue` node, commanding a graceful swoop. If the swoop overshoots, it seamlessly chains into a `Bank` maneuver to re-acquire the target.

- **Branch C: Elemental Tactics**
  - If the player is grouped up, the Brain receives `DesireElement`.
  - The flow transitions to the `Breath` node, commanding the Muscles to fire an elemental attack.

*Once any of these branches complete (e.g., the breath is fired), the node broadcasts `ActionComplete`, immediately dumping the Brain back into the `Engaged` Hub to pick the next best move.*

---

## 6. Exhaustion, Fleeing, and Regrowth

The Dragon fights dynamically until forced to retreat.

### **Evasion Trigger**
- **Process:** If `DragonTick` detects 0 stamina or heavy burst damage, it broadcasts `"StaminaEmpty"` or `"BurstDamageTaken"`.
- **Action:** The Brain instantly overrides the `Engaged` hub and forces flow into the `Evade` -> `Exhausted` states.

### **Tactical Retreat (`AirborneBossMovement.cs`)**
- **Process:** The Muscles receive `"Ride Escape Spline"`. The movement script queries `BossPathManager` for the nearest Escape route, detaches from freestyle flight, and glides away.

### **Healing (`RegeneratorController.cs`)**
- **Process:** Once safely away, the Brain reaches the `Recharging` hub and rides the Observation Spline.
- **Action:** The `RegeneratorController` takes over. Every 1.5 seconds, it commands `SegmentedDragonManager.RegrowOneSegment()`.
- **Return to Combat:** Once fully healed, the controller signals `"RechargeFull"`. The Brain leaps from the `Recharging` loop back into the `Engaged` hub, starting the dynamic combat cycle anew!

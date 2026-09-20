# Boss AI: Gameplay Flow & System Architecture

This guide walks through the exact linear sequence of events from the moment the Boss Level loads to the Dragon actively fighting the player. It explains how the interconnected systems (Brain, Senses, Muscles, Paths, and Anatomy) collaborate to create a living, breathing encounter.

---

## 1. Scene Load & Wave Initialization

When the player reaches the Boss wave, the level orchestrator begins the sequence.

### **`WaveSpawner.cs`**
- **Action:** Receives the command to start the Boss Wave.
- **Process:**
  1. Identifies the `BossConfig` in the wave data.
  2. Instantiates the configured Observation Paths and Escape Paths (Prefabs).
  3. Registers these paths globally with the `BossPathManager`.
  4. Spawns the `Boss_AsianFireDragonNew` prefab into the center of the arena.

### **`BossPathManager.cs`**
- **Role:** The "Air Traffic Controller". It holds a categorized list of all active routes (Airborne Observation, Terrestrial Escape, etc.).

---

## 2. Dragon Anatomy & Positioning

The moment the Dragon prefab spawns, it must assemble itself and locate its starting position.

### **`BossCreature.cs` (Start)**
- **Action:** Queries the `BossPathManager` for an Airborne Observation path.
- **Process:**
  1. Locates the `SplineComputer` on the returned path.
  2. Evaluates the physical start point (`startSpline.Evaluate(0.0)`).
  3. Instantly warps the Dragon's root transform to this exact position and rotation (preventing the `(0,0,0)` spawn bug).
  4. Commands the `AirborneBossMovement` to begin coiling along this spline.
  5. Instructs `SegmentedDragonManager` to generate the physical body.

### **`SegmentedDragonManager.cs`**
- **Role:** The skeleton builder.
- **Process:** Spawns the Head, Body, Legs, and Tail prefabs, hooking them up so they slither behind the root object along a shared breadcrumb trail.
- **Event:** Fires `OnHeadSpawned`.

### **`DragonActionListeners.cs` (Awake/OnEnable)**
- **Role:** The Muscles.
- **Action:** Hears `OnHeadSpawned` and immediately searches the new Head child for the `ElementalBreathController.cs` (since the mouth is located on the head). It is now fully armed.

---

## 3. The Brain Awakes

With the body built and placed on the track, the AI takes over.

### **`DragonBrainController.cs`**
- **Role:** The spark of life.
- **Process:**
  1. Clones the `DragonBrain_Automated.asset` (The Quest Machine Behavior Tree).
  2. Attaches a `QuestJournal` to the Dragon.
  3. Explicitly registers the Journal with `QuestMachine.RegisterQuestJournal` (allowing developers to view the AI state in real-time).
  4. Forces the state machine to `Active`, dropping the Dragon into the **"Orchestrator"** state.

---

## 4. Observing the Battle (Senses & Evaluation)

The Dragon is now flying in a circle high above, watching the player fight minions.

### **`DragonTick.cs`**
- **Role:** The Eyes and Ears (Senses).
- **Process:** Runs a 10Hz loop that constantly monitors the environment:
  - Are segments missing?
  - Are minions dying?
  - Did the player shoot the Dragon (10% health drop)?
  - Is the `HealthCrystal` taking damage?
- **Desire Evaluator:** Calls the decoupled `DesireEvaluator.cs`, which weighs these factors (e.g., Minion deaths increase 'Desire for Dominance', Crystal damage spikes 'Desire to Defend').
- **Messaging:** `DragonTick` uses `MessageSystem.SendMessage("Brain", "CrystalDamaged")` to broadcast these observations.

---

## 5. Decision Making (The Brain in Action)

The Quest Machine Behavior Tree processes the inputs from the Senses.

### **`DragonBrain_Automated.asset` (Quest Machine)**
- **Role:** The Decision Maker.
- **Scenario:** The player ignores the minions and shoots the Health Crystal.
- **Process:**
  1. The Brain is in the `Observation Spline` node.
  2. It receives the `"Brain"` -> `"CrystalDamaged"` message from `DragonTick`.
  3. A Quest Condition link (Condition: `Message: Brain -> CrystalDamaged`) evaluates to True.
  4. The flow transitions to the `Engaged` hub node.
  5. The `Engaged` node evaluates the highest Desire (provided by `DragonTick`). It determines `DesireDefend` is the strongest priority.
  6. The flow transitions to the `Defender` node.
  7. The `Defender` node has a Quest Action that broadcasts: `Target: DragonActions`, `Message: DefendCrystal`.

---

## 6. Execution (Muscles & Movement)

The Brain has decided what to do; the Muscles now execute it.

### **`DragonActionListeners.cs`**
- **Role:** The Executor.
- **Process:**
  1. Receives the `"DragonActions"` -> `"DefendCrystal"` message.
  2. Looks up the command in its `switch` statement.
  3. Commands the `AirborneBossMovement` to alter course toward the crystal.

### **`AirborneBossMovement.cs`**
- **Role:** The Pilot.
- **Process:**
  - Switches from `MovementMode.Spline` to `MovementMode.Freestyle`.
  - Detaches from the observation spline and physically steers the Dragon's root transform down toward the Health Crystal.
  - *(Meanwhile, `SegmentedDragonManager` continues blindly dragging the tail segments along the root's new freestyle path).*

---

## 7. Exhaustion and Regrowth

The Dragon fights fiercely, but takes heavy damage and loses a body segment.

### **`DragonTick.cs` (Stamina Drain)**
- **Process:** Detects that the Dragon's stamina has reached 0 (or burst damage threshold exceeded).
- **Action:** Broadcasts `"Brain"` -> `"StaminaEmpty"`.

### **`DragonBrain_Automated.asset`**
- **Process:** Transitions from `Engaged` to `Evade`, then to `Exhausted`.
- **Action:** Broadcasts `"DragonActions"` -> `"Ride Escape Spline"`.

### **`DragonActionListeners.cs`**
- **Process:** Commands `AirborneBossMovement.ForceImmediateEvasion()`.

### **`AirborneBossMovement.cs`**
- **Process:** Queries `BossPathManager` for the nearest Terrestrial/Airborne Escape route, lerps to it, and glides away to safety. Once complete, it signals the Brain, transitioning it to the `Recharging` node.

### **`RegeneratorController.cs`**
- **Role:** The Healer.
- **Process:**
  - When the Dragon enters the `Recharging` state and rests on the Observation Spline near an active `HealthCrystal`, this script activates.
  - It runs a strict internal timer. Every `1.5` seconds, it commands `SegmentedDragonManager.RegrowOneSegment()`.
  - Once fully healed, it tells the Brain it is ready to re-engage, starting the cycle anew.

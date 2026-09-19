# Setting Up Dragon AI in Quest Machine

> [!info] Goal
> Create a visual Behavior Tree where the Boss flows from `Orchestrator` -> `Engaged` -> `Exhausted` -> `Recharging` based on data from `DragonTick`.

---

## Step 1: Open the Editor

1. Click the top Unity menu: **Tools -> Pixel Crushers -> Quest Machine -> Quest Editor**.
2. Open your `DragonBrain` Quest.

---

## Step 2: Create the Task Nodes

> [!warning] Node Types
> You must specifically create **Task** nodes for behavior states.

1. Right-click empty space -> Select **Add Node -> Task**.
2. Click the new node. Change the **Name** field to exactly: `Orchestrator`
3. Right-click empty space -> Select **Add Node -> Task**.
4. Click the new node. Change the **Name** field to exactly: `Engaged`
5. Right-click empty space -> Select **Add Node -> Task**.
6. Click the new node. Change the **Name** field to exactly: `Exhausted`
7. Right-click empty space -> Select **Add Node -> Task**.
8. Click the new node. Change the **Name** field to exactly: `Recharging`

---

## Step 3: Connect Start to Orchestrator

> [!tip] Connection Pins
> Nodes pass to the next node when they Succeed. You must draw connections from the **Green Circle (Success)** at the bottom of a node.

1. Click the **Green Circle** at the bottom of the `Start` node.
2. Drag the line and drop it onto the `Orchestrator` node.

*(Now, when the boss spawns, it immediately starts in the Orchestrator state.)*

---

## Step 4: Configure Orchestrator -> Engaged

**A. Create the Connection:**
1. Click the **Green Circle (Success)** at the bottom of the `Orchestrator` node.
2. Drag the line and drop it onto the `Engaged` node.

**B. Set the Pass-Through Condition (How it Succeeds):**
1. Click the `Orchestrator` node.
2. Go to the Inspector panel. Find **Active State**.
3. Under **Conditions**, click the `+` button.
4. Select **Message**.
5. Fill exactly:
   - Target: `Brain`
   - Message: `DesireEvade`
   *(When DragonTick sends this message, the Orchestrator node succeeds and passes through the Green pin to Engaged).*

---

## Step 5: Configure Engaged Action (What it does)

1. Click the `Engaged` node.
2. Go to the Inspector panel. Find **Active State**.
3. Under **Actions**, click the `+` button.
4. Select **Message**.
5. Fill exactly:
   - Target: `DragonActions`
   - Message: `Pursuit`

---

## Step 6: Configure Engaged -> Exhausted

**A. Create the Connection:**
1. Click the **Green Circle (Success)** at the bottom of the `Engaged` node.
2. Drag the line and drop it onto the `Exhausted` node.

**B. Set the Pass-Through Condition:**
1. Click the `Engaged` node.
2. Go to the Inspector panel. Find **Active State**.
3. Under **Conditions**, click the `+` button.
4. Select **Message**.
5. Fill exactly:
   - Target: `Brain`
   - Message: `StaminaEmpty`
   *(When stamina is 0, this node succeeds and passes through the Green pin to Exhausted).*

---

## Step 7: Configure Exhausted Action (What it does)

1. Click the `Exhausted` node.
2. Go to the Inspector panel. Find **Active State**.
3. Under **Actions**, click the `+` button.
4. Select **Message**.
5. Fill exactly:
   - Target: `DragonActions`
   - Message: `Evade`

---

## Step 8: Configure Exhausted -> Recharging

**A. Create the Connection:**
1. Click the **Green Circle (Success)** at the bottom of the `Exhausted` node.
2. Drag the line and drop it onto the `Recharging` node.

**B. Set the Pass-Through Condition:**
1. Click the `Exhausted` node.
2. Go to the Inspector panel. Find **Active State**.
3. Under **Conditions**, click the `+` button.
4. Select **Timer**.
5. Fill exactly:
   - Duration: `5` (or whatever delay you want before returning to heal).
   *(When the timer ends, this node succeeds and passes through the Green pin).*

---

## Step 9: Configure Recharging Action (What it does)

1. Click the `Recharging` node.
2. Go to the Inspector panel. Find **Active State**.
3. Under **Actions**, click the `+` button.
4. Select **Message**.
5. Fill exactly:
   - Target: `DragonActions`
   - Message: `Regenerate`

---

## Step 10: Configure Recharging -> Orchestrator

**A. Create the Connection:**
1. Click the **Green Circle (Success)** at the bottom of the `Recharging` node.
2. Drag the line and drop it onto the `Orchestrator` node.

**B. Set the Pass-Through Condition:**
1. Click the `Recharging` node.
2. Go to the Inspector panel. Find **Active State**.
3. Under **Conditions**, click the `+` button.
4. Select **Message**.
5. Fill exactly:
   - Target: `Brain`
   - Message: `RechargeFull`
   *(When stamina is maxed, this node succeeds and passes through the Green pin back to the start).*
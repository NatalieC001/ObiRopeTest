# 👾 How to Assemble the Minions

Just like the Asian Dragon Boss, your generic Minions (the ones that spawn in geometric swarms) must follow the **Empty Root Architecture** to ensure that when your artist delivers the final 3D models, you can safely swap them out without breaking your physics or scripts!

Here is how you build a standard Minion prefab that is ready to be used by the Spline Path Generator.

---

## Step 1: Create the Root Object (The Identity)
1. Right-click in your Hierarchy and choose **Create Empty**. Name it **"Minion_Basic"**.
2. **The Physics Anchor:** Click `Add Component` and add a **`Rigidbody`**.
   * *Crucial:* Check the box for **`Is Kinematic`**. This ensures the minion can be hit by your physical arrows, but gravity won't pull it off its spline track!
3. **The Brain:** Click `Add Component` and search for **`StandardCreature`**.
   * This gives the minion basic health and an Elemental Type.
4. **The Visual Effects:** Click `Add Component` and search for **`DissolveEffect`**. (This existing project script will make the minion burn away smoothly when destroyed).

## Step 2: Add the Visuals and Colliders (The Children)
Now we add the replaceable parts as children to the Root.
1. Right-click your **"Minion_Basic"** root object and choose `3D Object -> Cube`. Name this child **"Mesh_Visual"**.
   * *Important Material Note:* For the dissolve effect to actually work, the material on your Cube's MeshRenderer must be set up to use a dissolve shader/texture!
2. Remove the auto-generated `BoxCollider` from this child (we want the physics on a separate node).
3. Right-click the Root object again and choose **Create Empty**. Name it **"Collider_Node"**. Add a `BoxCollider` (or `CapsuleCollider`) to this and size it to fit your cube.

## Step 3: Wire It Up
1. Go back to your Root object ("Minion_Basic").
2. Drag the **"Mesh_Visual"** into the `Target Renderer` slot on your `DissolveEffect` script.

*Why do we do this?* Tomorrow, when your artist gives you a 3D Gargoyle model, you just delete the "Mesh_Visual" cube, drop the 3D model in as a child, and resize the "Collider_Node". You don't have to touch a single script!

## Step 4: Save the Prefab
1. Drag the **"Minion_Basic"** root object from your Hierarchy down into `Assets/SplinePathGenerator/Scripts/Entities/Minions/` to save it as a **Prefab**.
2. Delete it from your active scene.

---

## Step 5: Generating the Swarm Path
Now that you have a perfectly constructed Minion Prefab, you can bake a swarm!
1. Open `Tools -> Spline Path Generator`.
2. Choose your shape (e.g., a Star).
3. In the Metadata Settings, drag your newly saved **"Minion_Basic"** prefab into the `Enemy Prefab` slot.
4. Set the Spawn Count to 20.
5. Click **Generate and Save Prefab**.

You now have a completed, ready-to-use Swarm Asset that you can plug into your `LevelConfigSO` and spawn via the CSV file!
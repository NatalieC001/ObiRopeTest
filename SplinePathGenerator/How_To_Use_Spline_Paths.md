# 🚂 The Master Guide: How to Use Spline Paths

Welcome! If you are reading this, you are probably trying to make enemies or bosses move in interesting, beautiful patterns—like swooping in a spiral, or marching in a star formation.

This document is your complete, beginner-friendly guide to understanding **how paths work in this game**, why we built a special tool for them, and how you can use them to create amazing gameplay.

---

## 1. The Big Picture: Tracks and Trains

The easiest way to understand this system is to think of **Rollercoaster Tracks** and **Rollercoaster Carts**.

*   **The Track:** We use a tool called *Dreamteck Splines* to draw invisible lines in the sky. These are our tracks.
*   **The Cart:** We attach our enemies (or bosses) to these tracks. They are the carts that ride the line.

In the past, drawing a perfect mathematical shape (like a 5-pointed star or a perfect spiral) by hand was nearly impossible. So, we built a tool to do it for you!

---

## 2. The Two Halves of the System

To make enemies move in cool patterns, we separated the work into two distinct halves. It is very important to remember that these two things are separate:

### A. The Path Generator (The "Track Maker")
This is a tool you only use in the Unity Editor before the game is playing. You find it at the top of your screen under `Tools -> Spline Path Generator`.
Its only job is to do the complex math to draw a shape (like a Celtic Knot) and save that shape as a **Prefab** (a saved file you can use over and over).
*   **Important:** This tool *does not know how to move enemies*. It only draws the tracks!

### B. The Movement Scripts (The "Engine")
These are scripts that already exist in the game (like `SplineTargetMovement.cs` or your Boss Controller). Their only job is to look at a track, spawn an enemy, and say, *"Hey enemy, drive along this line!"*

By keeping the Tracks and the Engine separate, you can draw a million different shapes without ever having to rewrite how enemies walk!

---

## 3. Step-by-Step: Making Your First Path

Imagine you want a wave of 5 enemies to fly in a perfect Star shape. Here is exactly how you do it:

**Step 1: Open the Tool**
Click `Tools -> Spline Path Generator` at the top of Unity. A window will pop up.

**Step 2: Design the Shape**
Change the "Shape Type" to **Star**.
In your Scene view, you will instantly see an invisible track appear! Use the sliders in the window to change the size, the number of points on the star, and how sharp the corners are.

**Step 3: Set the Rules**
At the bottom of the window, you will see a section for "Path Metadata Settings". This is where you write down the rules for whoever uses this track later:
*   **Enemy Prefab:** Drag and drop the actual enemy you want to use here.
*   **Spawn Count:** Type `20` (because we want 20 enemies!).
*   **Movement Speed:** Type how fast they should go.
    *   *Secret Trick:* If you want the enemies to hang in the air as a perfectly still, non-moving mathematical shape (like a constellation of stars), **set the Speed to 0!**

**Step 4: Save It!**
Click the big "Generate and Save Prefab" button.
Congratulations! You just created a permanent saved file in your `Assets/SplinePathGenerator/GeneratedPaths` folder.

---

## 4. What Exactly Did I Just Save?

If you click on the Prefab you just saved, you will see it is a fully complete, ready-to-use package!
It contains:
1.  **Spline_Curve:** A child object holding the `SplineComputer` (the track).
2.  **The Enemies:** The tool actually instantiates the enemies and bakes them into the prefab as children! They already have their `SplineFollower` components attached and configured with the speed you requested.
3.  **PathSpawnInfo:** A sticky note with the rules (just in case the game needs to know how many enemies are inside without counting them).

---

## 5. Using the Path in the Game

Now that you have your track saved, how do you use it?

Because the enemies are already baked inside the prefab, using it is incredibly easy.
When the `TrainingLevelManager` (or Wave Manager) says "Start Wave 1!", it simply instantiates the Star Prefab. That's it!

The track spawns, and the enemies inside it instantly start moving along the path.

*(If you are a programmer looking for how to modify these paths for boss attack runs, see the `Enemy_Spline_Integration_Guide.md` file!)*

---

## 6. The Advanced Trick: Boss Attack Runs

Sometimes you don't want a permanent, looping shape. You want a boss to suddenly swoop at the player in a crazy wavy line!

We call this an **Open Path** (a line with a clear START and END, unlike a closed circle).

1.  Use the Path Generator tool to make a **Spiral** or a wavy shape.
2.  The tool will automatically label one end **START** and the other **END**. Save it!
3.  During the game, when the boss gets angry, the game code grabs that Open Path.
4.  The code takes the **START** of the line and glues it to the Boss.
5.  The code takes the **END** of the line and glues it to where the Player is standing.
6.  The boss then rides that wavy track straight at the player!

This trick is what makes boss fights feel dynamic and hand-crafted, rather than just having the boss walk in a straight, boring line.

---

## Summary

*   **Spline Path Generator:** Draws the tracks and writes down the rules (How many enemies? How fast?).
*   **PathSpawnInfo:** The sticky note that holds those rules.
*   **Game Scripts:** Read the sticky note and push the enemies down the tracks.

Have fun creating wild shapes!
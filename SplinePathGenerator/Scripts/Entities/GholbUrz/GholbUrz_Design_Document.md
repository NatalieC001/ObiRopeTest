# 🌋 Entity Design Blueprint: Gholb-urz (Lava Slaters)

**Classification:** Hybrid AI (NavMesh + Physics + Scent Trail Splines)
**Environment:** Region 4: The Volcanic Caldera (Ash, lava flows, multi-level sunken trenches, volcanic geysers).

## 1. Overview
The **Gholb-urz** (Lava Slaters) are highly advanced, pack-mentality enemies. Visually resembling massive, armored pillbugs, they navigate the Volcanic Caldera using a dynamic hybrid movement system.

Unlike geometric swarms that strictly follow tracks, Lava Slaters roam freely. However, they use **"Scent Trails"**—invisible ground splines placed by level designers—as tactical hints to execute high-speed ramp jumps or coordinated flanking maneuvers.

---

## 2. Core Mechanics

### The Rolling State (Invulnerable)
*   **Behavior:** The Slater curls into an armored ball. It uses high-speed physics to roll down sloped hills and trenches.
*   **Defense:** While curled, the Slater's thick armor makes it **100% invulnerable** to standard damage.

### The Uncurled State (Vulnerable)
*   **Behavior:** When it reaches the player's platform or hits an obstacle, it uncurls to attack with mandibles/claws.
*   **Defense:** Its soft underbelly is exposed, making it highly vulnerable to standard arrows.

### Pack AI & Scent Trails
*   **Cover & Swarm:** Slaters are intelligent. They will use NavMesh to pathfind behind rocks or debris, clustering together. Once a pack reaches a critical mass, they initiate a **Mass Rush**, rolling simultaneously at the player.
*   **Scent Trails:** Designers can draw Dreamteck splines across ramps or trick-jumps. When a rolling Slater detects a "Scent Trail", it is magnetically attracted to the spline, using it to launch itself over gaps or smash into the player from above.
*   **Dynamic Blocking:** If the player physically blocks a Scent Trail, the Slaters are smart enough to abandon the spline and recalculate a raw NavMesh path to reach the player.

---

## 3. Combat Puzzles & Player Expression

The Gholb-urz are designed to challenge both the player's twitch reflexes and their creative problem-solving skills in VR.

### Approach A: Pure Skill & Aggression
*   **The Setup:** A pack of Lava Slaters curl up and execute a high-speed roll down a sloped trench toward the watchtower.
*   **The Solution:**
    1.  The player fires a **Stasis Arrow** to freeze a rolling Slater mid-air/mid-roll.
    2.  While frozen, the armor gaps are temporarily exposed. The player fires an **Ice Arrow** into the gap to apply the *Brittle* status effect.
    3.  Finally, the player fires an **Electric Arrow** which triggers the Brittle multiplier, shattering the armor and destroying the creature instantly!

### Approach B: The Creative Tinkerer ("Skeet Shooter")
*   **The Setup:** The battlefield is littered with dormant volcanic geysers sitting along the Slaters' preferred Scent Trails.
*   **The Solution:**
    1.  The player crafts a "Tar Baby" (combining a **Sticky Arrow** with a Decoy Tool).
    2.  The player shoots the Tar Baby directly onto a dormant geyser.
    3.  **The Payoff:** The rolling pack of Slaters is attracted to the decoy. When they roll over it, the sticky goo permanently glues them to the geyser hole.
    4.  Pressure builds, the geyser erupts, and the trapped Slaters are violently launched 100 feet into the sky. The player can then casually aim upward and shoot their exposed underbellies out of the sky!
Dragon AI & Boss Behavior Refinements

Task 1: Segment Death State Synchronization
Target Script: SegmentedDragonController.cs
Objective: Fix the gap between the head and body segments, and ensure synchronized entity despawning upon death.
Implementation Details:
- Refactor segment destruction callbacks so that when the final body segment reaches 0 HP, a synchronized OnDragonDeath trigger executes.
- Eliminate the delay coroutines that cause the head to disappear before legs and tail segments.
- Ensure head, body, legs, and tail despawn simultaneously on the exact same frame.

Task 2: Tagged Environment & Dynamic AI Decision-Making
Target Script: DragonDecisionEngine.cs / DragonBehaviorTree.cs
Objective: Implement dynamic tactical awareness for attack runs, evasion, minion management, and crystal recharging.
Implementation Details:
- Movement & Evasion: Evaluate observation paths, tagged environment escape routes, and freestyling navigation to dynamically initiate attack runs or evade player attacks.
- Elemental Affix Reaction: Incorporate status reactions for time-stop (5s freeze), movement slow/brittleness, and elemental damage over time.
- Tethering Mechanics: Hitting any part of the dragon with a tether rope attached to an anchor (floor/wall) creates a strict "rubber band" radius constraint. The dragon is restricted to this new radius until the rope breaks. Do not use complex flailing or struggling physics.
- Area Denial Mechanics: The dragon uses a Fire attack to deny floor space (extinguishable by player Ice arrows), and a "Wraith" dark mist attack that obscures vision and deals heavy damage if walked through.
- Crystal & Minion Interaction: The health crystal acts strictly as a health source. The dragon evaluates its priorities (healing itself vs. protecting minions or the crystal) to decide when to visit the crystal.
- Regrowth Mechanic: The dragon stays at the crystal to recharge health. Once it absorbs enough health to match the exact total health of a missing segment, that segment regrows.
- Minion Recharging: Minions visit the crystal to recharge. If shot while recharging, they become frightened and flee, retaining only the health they managed to gather.

Task 3: Dreamteck Environmental Splines Flight Choreography
Target Script: TacticalBossSplineManager.cs
Objective: Integrate environmental splines choreography for fluid aerial movement and tactical occlusion pathing.
Implementation Details:
- Utilize Dreamteck Environmental Splines container anchors to drive procedural flight paths via TacticalBossSplineManager.cs.
- Enable AI pathing to dip behind environmental occlusion structures and re-emerge seamlessly during aerial maneuvering loops.
- Ensure smooth camera tracking and seamless spline-to-freestyle movement transitions.

Player Experience & Combat Strategy
When players start the encounter, clear visual and sound signals help them target specific parts of the dragon's body. Shooting and breaking these parts weakens the dragon.

The main challenge is managing the health economy of the encounter. The dragon's AI assesses its needs and strategies, deciding when to recharge at crystals, summon minions, or protect allies. As the dragon absorbs health from the crystals, players see it regrow lost body parts based on the exact health values restored.

Destroying the recharge crystals is crucial to stop the boss and its minions from healing. If players do not destroy the crystals, the dragon will continue to heal and regrow parts. When the dragon or minions are at the crystal, the player has a clear opportunity to attack the crystal or the enemies.

As the boss moves through complex aerial paths and uses structures for cover, players must watch for environmental cues to predict attacks. The dragon restricts the player's movement area by using extinguishable fire and damaging dark mist.

Players should use different weapon types to find weaknesses, such as using ice arrows to put out fires, or rope arrows to rubber-band the dragon to a strict radius. Applying the correct strategy breaks the boss's defenses and wins the battle.

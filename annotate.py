import os

def insert_after(lines, search_term, insert_lines):
    for i, line in enumerate(lines):
        if search_term in line:
            return lines[:i] + insert_lines + lines[i:]
    return lines

def replace_line(lines, search_term, new_line):
    for i, line in enumerate(lines):
        if search_term in line:
            lines[i] = new_line
            return lines
    return lines

def annotate_dissolve_manager():
    with open("DissolveManager.cs", "r") as f:
        content = f.read()

    header = """// NEW: Changes 1 of 1:
// 1. Entirely new class created to act as a decoupled singleton manager for dissolve effects.
"""

    lines = content.split('\n')
    for i, line in enumerate(lines):
        if "public class DissolveManager" in line:
            lines.insert(i, header)
            break

    for i, line in enumerate(lines):
        if "public class DissolveManager" in line:
            lines.insert(i, "    // NEW: 1. This entire class handles coroutines independently of dying objects.")
            break

    with open("DissolveManager.cs", "w") as f:
        f.write('\n'.join(lines))

def annotate_dissolve_effect():
    with open("DissolveEffect.cs", "r") as f:
        lines = f.readlines()

    header = """// NEW: Changes 3 of 3:
// 1. Removed IArrowTarget interface from class declaration.
// 2. Changed dissolve fields to public so other scripts can access them without reflection.
// 3. Updated TriggerDissolve to delegate execution to DissolveManager.
"""

    for i, line in enumerate(lines):
        if "public class DissolveEffect" in line:
            lines.insert(i, header)
            lines.insert(i+1, "    // NEW: 1. No longer implements IArrowTarget.\n")
            break

    for i, line in enumerate(lines):
        if "public string dissolvePropertyName" in line:
            lines.insert(i-1, "    // NEW: 2. Changed to public fields.\n")
            break

    for i, line in enumerate(lines):
        if "public void TriggerDissolve(Action onComplete)" in line:
            lines.insert(i-4, "    // NEW: 3. Delegates execution to DissolveManager.\n")
            break

    with open("DissolveEffect.cs", "w") as f:
        f.writelines(lines)


def annotate_moving_target():
    with open("MovingTarget.cs", "r") as f:
        lines = f.readlines()

    header = """// NEW: Changes 1 of 1:
// 1. Updated TakeDamage() to call effect.TriggerDissolve with a null callback.
"""
    for i, line in enumerate(lines):
        if "public class MovingTarget" in line:
            lines.insert(i, header)
            break

    for i, line in enumerate(lines):
        if "effect.TriggerDissolve(null);" in line:
            lines.insert(i, "                // NEW: 1. Passing null callback to decoupled DissolveEffect\n")
            break

    with open("MovingTarget.cs", "w") as f:
        f.writelines(lines)

def annotate_dragon_movement():
    with open("SplinePathGenerator/Scripts/Entities/Dragon/DragonMovementManager.cs", "r") as f:
        lines = f.readlines()

    header = """// NEW: Changes 1 of 1:
// 1. Modified PlaceSegment() to use Rigidbody.MovePosition to fix arrow tunneling.
"""
    for i, line in enumerate(lines):
        if "public class DragonMovementManager" in line:
            lines.insert(i, header)
            break

    for i, line in enumerate(lines):
        if "Rigidbody rb = segment.GetComponent<Rigidbody>();" in line:
            lines.insert(i, "                // NEW: 1. Using MovePosition to solve fast-moving physics tunneling.\n")
            break

    with open("SplinePathGenerator/Scripts/Entities/Dragon/DragonMovementManager.cs", "w") as f:
        f.writelines(lines)

def annotate_dragon_segment():
    with open("SplinePathGenerator/Scripts/Entities/Dragon/DragonSegment.cs", "r") as f:
        lines = f.readlines()

    header = """// NEW: Changes 4 of 4:
// 1. Added private bool isDead to prevent double-triggering.
// 2. Updated Awake() to force all nested colliders onto the Enemy layer automatically.
// 3. Updated TakeDamage() to check isDead and Die() to set isDead.
// 4. Updated Die() to pass FinalizeDestruction as a callback to DissolveEffect.
"""
    for i, line in enumerate(lines):
        if "public class DragonSegment" in line:
            lines.insert(i, header)
            break

    for i, line in enumerate(lines):
        if "private bool isDead =" in line:
            lines.insert(i, "    // NEW: 1. Prevent double death triggering\n")
            break

    for i, line in enumerate(lines):
        if "Collider[] allColliders = GetComponentsInChildren<Collider>(true);" in line:
            lines.insert(i, "            // NEW: 2. Automatically apply Enemy layer to all child colliders to fix hit detection.\n")
            break

    for i, line in enumerate(lines):
        if "if (isDead) return;" in line and "public virtual void TakeDamage" in lines[i-2]:
            lines.insert(i, "        // NEW: 3. Health logic checking isDead flag\n")
            break

    for i, line in enumerate(lines):
        if "dissolve.TriggerDissolve(FinalizeDestruction);" in line:
            lines.insert(i, "            // NEW: 4. Passing callback to decoupled visual effect.\n")
            break

    with open("SplinePathGenerator/Scripts/Entities/Dragon/DragonSegment.cs", "w") as f:
        f.writelines(lines)

def annotate_perm_dragon_segment():
    with open("SplinePathGenerator/Scripts/Entities/Dragon/PermanentDragonSegment.cs", "r") as f:
        lines = f.readlines()

    header = """// NEW: Changes 2 of 2:
// 1. Removed reflection hack from Start().
// 2. Updated TriggerTotalDeath() to pass Destroy callback to DissolveEffect.
"""
    for i, line in enumerate(lines):
        if "public class PermanentDragonSegment" in line:
            lines.insert(i, header)
            break

    for i, line in enumerate(lines):
        if "isDestructiblePart = false;" in line:
            lines.insert(i+1, "        // NEW: 1. Removed complex reflection code that used to disable old dissolve logic here.\n")
            break

    for i, line in enumerate(lines):
        if "dissolve.TriggerDissolve(() =>" in line:
            lines.insert(i, "            // NEW: 2. Passing lambda callback to destroy object after visuals finish.\n")
            break

    with open("SplinePathGenerator/Scripts/Entities/Dragon/PermanentDragonSegment.cs", "w") as f:
        f.writelines(lines)

def annotate_seg_dragon_manager():
    with open("SplinePathGenerator/Scripts/Entities/Dragon/SegmentedDragonManager.cs", "r") as f:
        lines = f.readlines()

    header = """// NEW: Changes 2 of 2:
// 1. Updated UpdateSegmentSpacing() to use Rigidbody.MovePosition.
// 2. Updated TriggerTotalDeath() to read public dissolveDuration directly instead of using Reflection.
"""
    for i, line in enumerate(lines):
        if "public class SegmentedDragonManager" in line:
            lines.insert(i, header)
            break

    for i, line in enumerate(lines):
        if "Rigidbody rb = segment.GetComponent<Rigidbody>();" in line:
            lines.insert(i, "                    // NEW: 1. Moving using physics to prevent arrow tunneling.\n")
            break

    for i, line in enumerate(lines):
        if "float duration = dissolve.dissolveDuration;" in line:
            lines.insert(i, "                    // NEW: 2. Directly accessing public property without Reflection.\n")
            break

    with open("SplinePathGenerator/Scripts/Entities/Dragon/SegmentedDragonManager.cs", "w") as f:
        f.writelines(lines)

def annotate_standard_creature():
    with open("SplinePathGenerator/Scripts/Entities/Minions/StandardCreature.cs", "r") as f:
        lines = f.readlines()

    header = """// NEW: Changes 4 of 4:
// 1. Added IArrowTarget interface to class declaration.
// 2. Added isDead boolean to prevent multiple deaths.
// 3. Updated Awake() to force all child colliders to the Enemy layer.
// 4. Added OnArrowHit implementation and updated Die() to pass callback to DissolveEffect.
"""
    for i, line in enumerate(lines):
        if "public class StandardCreature" in line:
            lines.insert(i, header)
            lines.insert(i+1, "    // NEW: 1. Natively implements IArrowTarget directly on the brain.\n")
            break

    for i, line in enumerate(lines):
        if "private bool isDead =" in line:
            lines.insert(i, "    // NEW: 2. Tracking death state.\n")
            break

    for i, line in enumerate(lines):
        if "Collider[] colliders = GetComponentsInChildren<Collider>(true);" in line:
            lines.insert(i, "            // NEW: 3. Automatic nested collider layer assignment to fix hit detection.\n")
            break

    for i, line in enumerate(lines):
        if "public void OnArrowHit" in line:
            lines.insert(i, "    // NEW: 4. Routing interface hits natively to TakeDamage, and Die() passes a callback.\n")
            break

    with open("SplinePathGenerator/Scripts/Entities/Minions/StandardCreature.cs", "w") as f:
        f.writelines(lines)

def annotate_sticking_arrow():
    with open("StickingArrow.cs", "r") as f:
        lines = f.readlines()

    header = """// NEW: Changes 1 of 1:
// 1. Updated CleanupWallArrowRoutine() to pass CleanupArrow as a callback to DissolveEffect.
"""
    for i, line in enumerate(lines):
        if "public class StickingArrow" in line:
            lines.insert(i, header)
            break

    for i, line in enumerate(lines):
        if "effect.TriggerDissolve(CleanupArrow);" in line:
            lines.insert(i, "            // NEW: 1. Trigger visuals and wait for callback instead of hardcoded wait.\n")
            break

    with open("StickingArrow.cs", "w") as f:
        f.writelines(lines)

annotate_dissolve_manager()
annotate_dissolve_effect()
annotate_moving_target()
annotate_dragon_movement()
annotate_dragon_segment()
annotate_perm_dragon_segment()
annotate_seg_dragon_manager()
annotate_standard_creature()
annotate_sticking_arrow()

using UnityEngine;

/// <summary>
/// Defines the physical weight and mobility of an object for the Rope System.
/// The Rope System looks at the mobility of BOTH targets to determine the final Rope State:
/// 
/// MATRIX:
/// - Lightweight + Lightweight = BASH (Both objects can move freely, yank them into each other)
/// - Lightweight + Heavyweight/Immovable = TETHER (One object is movable, the other is an anchor. Yank the lightweight object to the anchor)
/// - Heavyweight/Immovable + Heavyweight/Immovable = TRIPWIRE (Neither object moves significantly. Freeze the rope in place as a trap)
/// </summary>
public enum RopeTargetMobility
{
    /// <summary>
    /// Goblins, crates, explosive barrels. Can be yanked freely through the air.
    /// Used for BASH and TETHER interactions.
    /// </summary>
    Lightweight,

    /// <summary>
    /// Big bosses, heavy statues. Acts as an anchor, pulling Lightweight objects to it without moving itself.
    /// Pairs with another Heavy/Immovable to create a TRIPWIRE.
    /// </summary>
    Heavyweight,

    /// <summary>
    /// Walls, floors, the ground. Cannot be moved at all.
    /// Pairs with another Heavy/Immovable to create a TRIPWIRE.
    /// </summary>
    Immovable
}

/// <summary>
/// Attach this script to any GameObject in the scene that can be hit by a Rope Arrow.
/// This defines how the object will react when connected to another object by a rope.
/// If an object is hit but does not have this component, it defaults to Immovable.
/// </summary>
public class ObjectWeight : MonoBehaviour
{
    public RopeTargetMobility mobility = RopeTargetMobility.Lightweight;
}

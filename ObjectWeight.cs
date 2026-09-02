using UnityEngine;

public enum RopeTargetMobility
{
    Lightweight, // Can be yanked through the air
    Heavyweight, // Acts as an anchor
    Immovable    // Cannot be moved at all
}

public class ObjectWeight : MonoBehaviour
{
    public RopeTargetMobility mobility = RopeTargetMobility.Lightweight;
}

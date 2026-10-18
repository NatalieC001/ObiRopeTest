using UnityEngine;

/// <summary>
/// ====================================================================================================
/// BASH PHYSICS MANAGER - CENTRAL PHYSICS ORCHESTRATOR
/// ====================================================================================================
///
/// WHAT THIS CLASS DOES:
/// This manager is strictly responsible for handling the physics state changes of objects caught in a rope
/// mechanic (Bash, Tether, Tripwire). It enforces separation of concerns: data classes (like RopeArrowPair)
/// determine *who* is hit, while this class determines *how* the physics engine reacts.
///
/// WHY IT EXISTS:
/// When a lightweight object (like a DestructibleProp) is suddenly winched by Obi Rope, the instantaneous
/// change in rest length imparts a massive physical impulse. Without intervention, targets would slingshot
/// at terminal velocity, causing motion sickness in VR and breaking immersion.
///
/// HOW IT WORKS:
/// When the rope logic triggers a Bash or Tether:
/// 1. It turns `isKinematic = false` so the object can be pulled.
/// 2. It applies a temporary "Physics Hijack", spiking linear drag and angular drag to smooth out the
///    impulse and act like air resistance.
/// When the rope breaks or slumps, it reverts the object to its resting state.
/// </summary>
public class BashPhysicsManager : MonoBehaviour
{
    public static BashPhysicsManager Instance { get; private set; }

    [Header("VR Smoothing Settings")]
    [Tooltip("Temporary drag applied to lightweight objects while they are being winched to prevent whiplash.")]
    public float bashDrag = 3f;
    [Tooltip("Temporary angular drag applied to lightweight objects to stop them from spinning wildly.")]
    public float bashAngularDrag = 2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Temporarily overrides the physics of a target to prepare it for a violent rope pull.
    /// </summary>
    public void ApplyBashPhysics(GameObject target)
    {
        if (target == null) return;

        Rigidbody rb = target.GetComponentInParent<Rigidbody>();
        if (rb == null) rb = target.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            // Crucial for Obi Rope pulling in VR
            rb.isKinematic = false;

            // Temporary drag to prevent massive whipping/slingshotting during winch
            rb.drag = bashDrag;
            rb.angularDrag = bashAngularDrag;
        }
    }

    /// <summary>
    /// Restores the default resting physics of a target once the rope breaks or the mechanic ends.
    /// </summary>
    public void RevertBashPhysics(GameObject target)
    {
        if (target == null) return;

        Rigidbody rb = target.GetComponentInParent<Rigidbody>();
        if (rb == null) rb = target.GetComponentInChildren<Rigidbody>();

        if (rb != null)
        {
            ObjectWeight weight = target.GetComponentInParent<ObjectWeight>();
            if (weight == null) weight = target.GetComponent<ObjectWeight>();

            // Restore default values - assume new lightweight objects want to be kinematic when free
            if (weight != null && weight.mobility == RopeTargetMobility.Lightweight)
            {
                rb.isKinematic = true;
            }

            rb.drag = 0f;
            rb.angularDrag = 0.05f;
        }
    }
}

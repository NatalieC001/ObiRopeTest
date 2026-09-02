

//-----------------------------------------------------------------
using UnityEngine;
using System.Collections;

public enum ArrowElementType
{
    Normal,
    Fire,
    Electric,
    Sticky,
    Stasis,
    Ice
}


public enum ArrowCategory { Normal, Rope }

public class StickingArrow : MonoBehaviour
{
    public ArrowCategory arrowCategory = ArrowCategory.Normal;
    [Tooltip("What element type this arrow carries.")]
    public ArrowElementType elementType = ArrowElementType.Normal;
    [Tooltip("Drag the child object (e.g., HunterTail) here to define where the rope spool begins.")]
    public Transform tailPoint;

    private Rigidbody rb;
    private SphereCollider myCollider;
    private BoxCollider shaftCollider;
    private ArrowFlightDynamics flightDynamics;
    private LineRenderer trailRenderer;

    private bool hasHit = false;
    private bool isDestroying = false;
    private GameObject anchor;
    private Coroutine cleanupRoutine;

    private bool hasBeenRegistered = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        myCollider = GetComponentInChildren<SphereCollider>();
        shaftCollider = GetComponentInChildren<BoxCollider>();
        flightDynamics = GetComponentInChildren<ArrowFlightDynamics>();
        trailRenderer = GetComponentInChildren<LineRenderer>();
    }

    private void Start()
    {
        // Start the 3-second self-destruct timer
        StartCoroutine(SelfDestructRoutine());
    }

    private IEnumerator SelfDestructRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (!hasHit) CleanupArrow();
    }

    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[StickingArrow] Collision detected with: {collision.gameObject.name} (Layer: {collision.gameObject.layer})");

        if (hasHit || isDestroying) return;

        // Ensure arrows never collide with themselves or each other upon spawning
        if (collision.gameObject.layer == LayerMask.NameToLayer("Arrow") || collision.gameObject.tag == "Arrow")
        {
            return;
        }

        // VERBOSE DEBUG LOGGING
        string hitName = collision.gameObject.name;
        string hitTag = collision.gameObject.tag;
        string hitLayer = LayerMask.LayerToName(collision.gameObject.layer);

        // ── Validation Guard ──────────────────────────────────────────────────
        // Only valid targets (MovingTarget/IArrowTarget) or explicit environment surfaces (Wall/Floor/Ground/Target/FloorTarget)
        // should cause the arrow to stick and stop moving. We use ToLower() to catch "ground" or "Ground".
        IArrowTarget target = collision.collider.GetComponentInParent<IArrowTarget>();
        FloorTarget floorTarget = collision.collider.GetComponentInParent<FloorTarget>();

        string hTag = hitTag.ToLower();
        string hLayer = hitLayer.ToLower();

        bool isEnvironment = hTag == "wall" || hTag == "floor" || hTag == "ground" || hTag == "target" ||
                             hLayer == "wall" || hLayer == "floor" || hLayer == "ground" || hLayer == "target" ||
                             floorTarget != null;

        if (target == null && !isEnvironment)
        {
            Debug.Log($"<color=grey>[StickingArrow] '{gameObject.name}' ignored collision with '{hitName}'. Not a valid target or environment surface.</color>");
            return; // Ignore this collision completely (e.g. Hands, Bows, random spheres)
        }

        hasHit = true;
        Debug.Log($"<color=yellow>[StickingArrow] '{gameObject.name}' VALID HIT -> Object: '{hitName}' | Tag: '{hitTag}' | Layer: '{hitLayer}'</color>");

        // Stop the self-destruct timer since we hit something
        StopAllCoroutines();

        // 1. STOPPING THE ARROW
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (myCollider != null) myCollider.isTrigger = true;
        if (shaftCollider != null) shaftCollider.isTrigger = true;

        if (flightDynamics != null) flightDynamics.enabled = false;
        if (trailRenderer != null) trailRenderer.enabled = false;

        // 2. SURFACE ROTATION ALIGNMENT
        Vector3 impactPoint = transform.position;
        if (collision.contacts.Length > 0)
        {
            impactPoint = collision.contacts[0].point;

            // If we hit an environment surface (Wall/Floor), align perfectly perpendicular like a peg!
            if (target == null && isEnvironment)
            {
                Vector3 surfaceNormal = collision.contacts[0].normal;
                transform.rotation = Quaternion.LookRotation(-surfaceNormal);
            }
        }

        // 3. ATTACHING TO THE TARGET
        anchor = new GameObject("ArrowAnchor");
        anchor.transform.position = transform.position;
        anchor.transform.rotation = transform.rotation;

        if (collision.collider.attachedRigidbody != null)
        {
            anchor.transform.SetParent(collision.collider.attachedRigidbody.transform, true);
        }
        else
        {
            anchor.transform.SetParent(collision.collider.transform, true);
        }

        transform.SetParent(anchor.transform, true);

        // 4. EXPLICIT LOGIC RULES FOR ROPE ARROW VS ELEMENTAL ARROW
        if (arrowCategory == ArrowCategory.Rope)
        {
            // --- ROPE ARROW RULES ---
            // 1. Deals ZERO damage to targets.
            // 2. Waits in the scene FOREVER (No Cleanup Routines ever!).
            // 3. Rope generation rules are handled remotely by MovingTarget.cs and the Pair Manager.

            if (target != null)
            {
                Debug.Log($"<color=lime>[StickingArrow] Rope Arrow '{gameObject.name}' hit target '{target.GetType().Name}'. Sending 0-damage signal.</color>");
                target.OnArrowHit(0f, impactPoint, ElementTypeOB7.Normal);
            }
            else
            {
                Debug.Log($"<color=orange>[StickingArrow] Rope Arrow '{gameObject.name}' hit environment surface '{hitName}'. Sticking permanently.</color>");
            }

            // Register the rope arrow with the manager (only once)
            if (arrowCategory == ArrowCategory.Rope && !hasBeenRegistered)
            {
                hasBeenRegistered = true;
                if (RopeArrowManagerObi7.Instance != null)
                {
                    RopeArrowManagerObi7.Instance.RegisterRopeArrow(this, this.gameObject, impactPoint);
                }
            }
        }
        else if (arrowCategory == ArrowCategory.Normal)
        {
            // --- ELEMENTAL ARROW RULES ---
            // 1. Deals 1.0 damage to targets.
            // 2. ALWAYS runs a self-destruct cleanup routine so it deletes itself from the scene.

            if (target != null)
            {
                // Convert ArrowElementType to ElementTypeOB7
                ElementTypeOB7 ob7Element = ElementTypeOB7.Normal;
                switch (elementType)
                {
                    case ArrowElementType.Fire: ob7Element = ElementTypeOB7.Fire; break;
                    case ArrowElementType.Electric: ob7Element = ElementTypeOB7.Electric; break;
                    case ArrowElementType.Sticky: ob7Element = ElementTypeOB7.Sticky; break;
                    case ArrowElementType.Stasis: ob7Element = ElementTypeOB7.Stasis; break;
                    case ArrowElementType.Ice: ob7Element = ElementTypeOB7.Ice; break;
                    default: ob7Element = ElementTypeOB7.Normal; break;
                }

                Debug.Log($"<color=lime>[StickingArrow] Elemental Arrow '{gameObject.name}' hit target '{target.GetType().Name}'. Sending element: {ob7Element}.</color>");
                target.OnArrowHit(1f, impactPoint, ob7Element);

                // Elemental arrow cleans up quickly if it hit an enemy
                cleanupRoutine = StartCoroutine(CleanupArrowRoutine(3f));
            }
            else
            {
                Debug.Log($"<color=orange>[StickingArrow] Elemental Arrow '{gameObject.name}' hit environment surface '{hitName}'. Beginning slow dissolve...</color>");

                // Elemental arrow dissolves slowly if it hit a wall
                DissolveEffect myDissolve = GetComponentInChildren<DissolveEffect>();
                if (myDissolve != null)
                {
                    cleanupRoutine = StartCoroutine(CleanupWallArrowRoutine(myDissolve));
                }
                else
                {
                    cleanupRoutine = StartCoroutine(CleanupArrowRoutine(10f));
                }
            }
        }
    }

    private IEnumerator CleanupArrowRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        DissolveEffect myDissolve = GetComponentInChildren<DissolveEffect>();
        if (myDissolve != null && !isDestroying)
        {
            myDissolve.TriggerDissolve();
            yield return new WaitForSeconds(1.5f);
        }

        CleanupArrow();
    }

    private IEnumerator CleanupWallArrowRoutine(DissolveEffect effect)
    {
        // Wait 10 seconds, then dissolve
        yield return new WaitForSeconds(10f);

        if (effect != null && !isDestroying)
        {
            effect.TriggerDissolve();
            yield return new WaitForSeconds(1.5f);
        }

        CleanupArrow();
    }

    private void CleanupArrow()
    {
        if (isDestroying) return;
        isDestroying = true;

        // Stop all coroutines
        StopAllCoroutines();
        if (cleanupRoutine != null)
        {
            StopCoroutine(cleanupRoutine);
            cleanupRoutine = null;
        }

        // Destroy the anchor first (this will unparent the arrow)
        if (anchor != null)
        {
            Destroy(anchor);
            anchor = null;
        }

        if (arrowCategory == ArrowCategory.Rope && RopeArrowManagerObi7.Instance != null)
        {
            RopeArrowManagerObi7.Instance.HandleArrowDestroyed(this);
        }

        // Destroy the arrow
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Safety cleanup - ensure anchor is destroyed if arrow is destroyed unexpectedly
        if (anchor != null)
        {
            Destroy(anchor);
            anchor = null;
        }
    }
}

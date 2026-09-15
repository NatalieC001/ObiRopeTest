using UnityEngine;
using System.Collections;

//public enum ElementType 
//{ 
//    Normal, 
//    Fire, 
//    Electric, 
//    Sticky, 
//    Stasis 
//} 

using Obi;

/// <summary> 
/// Dedicated logic for the Rope Arrow. 
/// Sticks to surfaces/targets, deals 0 damage on impact, and registers with the RopeArrowManager. 
/// </summary> 
public class RopeArrow : StickingArrow, IArrowTarget
{
    private Rigidbody rb;
    private SphereCollider headCollider;
    private BoxCollider shaftCollider;
    private ArrowFlightDynamics flightDynamics;
    private TrailRenderer trailRenderer;

    [Header("Elemental Infusion")]
    [Tooltip("The time window in seconds the player has to hit the arrow with an elemental arrow.")]
    public float infusionWindow = 10f;

    [Tooltip("The collider that accepts elemental hits.")]
    public Collider elementalCollider;

    private bool hasHit = false;
    private bool isDestroying = false;

    // The entire tail assembly (which contains the large physical tailCollider causing the VR snagging)
    private GameObject ropeTailAssembly;
    private GameObject anchor;
    private Coroutine cleanupRoutine;
    private Coroutine infusionWindowRoutine;

    // Tracks the rope this arrow is attached to, if any
    private ObiRope connectedRope;

    // The element currently infused into this arrow (starts normal) 
    public ElementTypeOB7 CurrentElement { get; private set; } = ElementTypeOB7.Normal;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        headCollider = GetComponentInChildren<SphereCollider>();
        shaftCollider = GetComponentInChildren<BoxCollider>();
        flightDynamics = GetComponentInChildren<ArrowFlightDynamics>();
        trailRenderer = GetComponentInChildren<TrailRenderer>();

        if (elementalCollider != null)
        {
            // Disable the ENTIRE tail assembly (including the massive physical tailCollider)
            // to ensure absolutely 0 rope mechanics are active when the arrow is held/fired.
            ropeTailAssembly = elementalCollider.transform.parent.gameObject;
            ropeTailAssembly.SetActive(false);
        }
    }

    private void Start()
    {
        // Start the 3-second self-destruct timer (matches StickingArrow)
        StartCoroutine(SelfDestructRoutine());
    }

    private IEnumerator SelfDestructRoutine()
    {
        yield return new WaitForSeconds(3f);
        CleanupArrow();
    }

    public void SetConnectedRope(ObiRope rope)
    {
        connectedRope = rope;
    }

    public ObiRope GetConnectedRope()
    {
        return connectedRope;
    }

    public Transform GetTailTransform()
    {
        return elementalCollider != null ? elementalCollider.transform : transform;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit || isDestroying) return;
        hasHit = true;

        // Stop the self-destruct timer since we hit something (matches StickingArrow)
        StopAllCoroutines();

        // 1. STOPPING THE ARROW (Matches StickingArrow exactly)
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (headCollider != null) headCollider.isTrigger = true;
        if (shaftCollider != null) shaftCollider.isTrigger = true;

        if (flightDynamics != null) flightDynamics.enabled = false;
        if (trailRenderer != null) trailRenderer.emitting = false;

        // 2 & 3. ATTACHING TO THE TARGET (Matches StickingArrow exactly)
        anchor = new GameObject("RopeArrowAnchor");
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

        // Handle damage (Matches StickingArrow exactly)
        IArrowTarget target = collision.collider.GetComponentInParent<IArrowTarget>();
        if (target == null) target = collision.collider.GetComponentInChildren<IArrowTarget>();

        Vector3 impactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position;

        // 4. WAKE UP CHECK: Apply 0 damage if we hit a gameplay target.
        if (target != null) target.OnArrowHit(0f, impactPoint, ElementTypeOB7.Normal);

        // ACTIVATE ENTIRE ROPE TAIL ASSEMBLY FOR INFUSION 
        // We do this unconditionally so players can set up tripwires on static walls/floors!
        if (ropeTailAssembly != null)
        {
            ropeTailAssembly.SetActive(true);

            // CRITICAL FIX: The validator requires the tailCollider to be `isTrigger = false` initially. 
            // But once the arrow is stuck to a target, leaving a solid collider active inside the target
            // will cause a massive physics explosion! We must convert it to a trigger immediately.
            Collider tailCol = ropeTailAssembly.GetComponentInChildren<Collider>();
            if (tailCol != null) tailCol.isTrigger = true;

            infusionWindowRoutine = StartCoroutine(InfusionWindowTimer());
        }

        // CONTACT MANAGER
        Rigidbody targetRb = collision.collider.attachedRigidbody;
        GameObject hitObj = targetRb != null ? targetRb.gameObject : collision.collider.gameObject;

        // Notify the rope manager (Obi7 implementation)
        RopeArrowManagerObi7.Instance?.RegisterRopeArrow(this, hitObj, impactPoint);

        // If we hit a dragon segment, notify it so the dragon can track tether state
        DragonSegment ds = collision.collider.GetComponentInParent<DragonSegment>();
        if (ds != null)
        {
            ds.OnRopeAttached(this);
        }
    }

    // (Matches StickingArrow exactly)
    private IEnumerator CleanupArrowRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        CleanupArrow();
    }

    // (Matches StickingArrow exactly)
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

    // (Matches StickingArrow exactly)
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
        if (infusionWindowRoutine != null)
        {
            StopCoroutine(infusionWindowRoutine);
            infusionWindowRoutine = null;
        }

        // If this arrow is attached to a dragon segment, notify it that the rope detached
        if (anchor != null)
        {
            DragonSegment ds = anchor.GetComponentInParent<DragonSegment>();
            if (ds != null)
            {
                ds.OnRopeDetached(this);
            }
        }

        // Destroy the anchor first (this will unparent the arrow)
        if (anchor != null)
        {
            Destroy(anchor);
            anchor = null;
        }

        // Notify manager before destruction so it resets if needed
        RopeArrowManagerObi7.Instance?.HandleArrowMiss(this);

        // If the arrow is taking a connected rope down with it (e.g. target died and despawned),
        // notify the manager (so subscribers can react) and destroy the entire rope to avoid leaving giant floating tethers.
        if (connectedRope != null && connectedRope.gameObject != null)
        {
            RopeArrowManagerObi7.Instance?.NotifyRopeBroken(this);
            Destroy(connectedRope.gameObject);
        }

        // Destroy the arrow
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Safety cleanup - ensure anchor is destroyed if arrow is destroyed unexpectedly (Matches StickingArrow exactly)
        if (anchor != null)
        {
            // Notify segment if present
            DragonSegment ds = anchor.GetComponentInParent<DragonSegment>();
            if (ds != null)
            {
                ds.OnRopeDetached(this);
            }

            Destroy(anchor);
            anchor = null;
        }

        // Safety cleanup - destroy the rope if the arrow was killed unexpectedly by the target
        if (connectedRope != null && connectedRope.gameObject != null)
        {
            RopeArrowManagerObi7.Instance?.NotifyRopeBroken(this);
            Destroy(connectedRope.gameObject);
        }
    }

    private IEnumerator InfusionWindowTimer()
    {
        yield return new WaitForSeconds(infusionWindow);

        // Window closed! Player missed the time limit. 
        if (ropeTailAssembly != null)
        {
            ropeTailAssembly.SetActive(false);
        }

        if (connectedRope != null)
        {
            // We have a rope, so freeze it to become a static tripwire
            RopeArrowManagerObi7.Instance?.FreezeRope(connectedRope);
        }
        else
        {
            // We never got a rope (we were a single arrow that was forgotten)
            RopeArrowManagerObi7.Instance?.HandleArrowTimeout(this);
        }
    }

    /// <summary> 
    /// When another arrow (an elemental arrow) hits the elemental collider of this stuck rope arrow. 
    /// Implementation of IArrowTarget. 
    /// </summary> 
    /// <summary>
    /// When another arrow (an elemental arrow) hits the elemental collider of this stuck rope arrow.
    /// Implementation of IArrowTarget.
    /// </summary>
    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        if (ropeTailAssembly == null || !ropeTailAssembly.activeInHierarchy) return;
        if (elementType == ElementTypeOB7.Normal) return;

        Infuse(elementType);
    }

    private void Infuse(ElementTypeOB7 newElement)
    {
        CurrentElement = newElement;

        // Close the window so it can't be changed again 
        if (infusionWindowRoutine != null) StopCoroutine(infusionWindowRoutine);
        if (ropeTailAssembly != null) ropeTailAssembly.SetActive(false);

        // Tell the manager to update the physical Obi Rope effects 
        RopeArrowManagerObi7.Instance?.ApplyElementalEffect(this);
    }
}
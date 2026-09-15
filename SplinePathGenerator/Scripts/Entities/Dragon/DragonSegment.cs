using System;
using UnityEngine;
using Dreamteck.Splines;

/// <summary>
/// Attached to individual segments (Head, Body, Tail) of the Asian Dragon Boss.
/// Handles segment health and reports destruction to the main SegmentedDragonManager.
/// </summary>
public class DragonSegment : MonoBehaviour, IArrowTarget
{
    [Header("Segment Stats")]
    [Tooltip("Can this individual piece be destroyed mid-fight? (Check True for body segments, False for Head/Legs/Tail).")]
    public bool isDestructiblePart = true;

    public float health = 100f;

    [Tooltip("The amount of power/energy this specific segment contributes to the boss's total power.")]
    public float powerContribution = 10f;

    [Header("Visuals & Physics")]
    [Tooltip("Reference to the child mesh renderer (useful for triggering visual effects).")]
    [SerializeField] private Renderer segmentRenderer;
    [Tooltip("Reference to the child collider (useful for disabling physics upon death).")]
    [SerializeField] private Collider segmentCollider;

    [Tooltip("The physics layer this segment will be forced onto so arrows can detect it. Displayed here as a reminder!")]
    [SerializeField] private string targetLayer = "Enemy";

    private SegmentedDragonManager dragonManager;
    private BossCreature bossBrain;

    // Follower is now explicitly set by SegmentedDragonManager (no automatic GetComponent())
    private SplineFollower follower;
    public SplineFollower Follower => follower;

    // The index of this segment in the manager's list (Head = 0)
    public int SegmentIndex { get; set; }

    // Prevent re-entrancy during death/dissolve
    private bool isDying = false;

    private void Awake()
    {
        // Do not create/modify follower here — SegmentedDragonManager owns follower creation/enable logic.
        // Keep Awake light-weight (only layer enforcement).
        // Force the physics layer so arrows detect this segment, even if the dev forgot to set it!
        int layerIndex = LayerMask.NameToLayer(targetLayer);
        if (layerIndex != -1)
        {
            gameObject.layer = layerIndex;

            // Also explicitly ensure the collider child is on the layer, as that's what physics actually hits
            if (segmentCollider != null)
            {
                segmentCollider.gameObject.layer = layerIndex;
            }
        }
        else
        {
            Debug.LogWarning($"[DragonSegment] Layer '{targetLayer}' does not exist in your project settings!");
        }
    }

    /// <summary>
    /// Called by SegmentedDragonManager after instantiation so the manager remains the single authority over movement/followers.
    /// </summary>
    public void SetFollower(SplineFollower follower)
    {
        this.follower = follower;
    }

    public void Initialize(SegmentedDragonManager manager, BossCreature brain, int index)
    {
        dragonManager = manager;
        bossBrain = brain;
        SegmentIndex = index;
    }

    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        // If this is a permanent piece (head/legs/tail) it should be invulnerable / not shootable.
        if (!isDestructiblePart)
        {
            Debug.Log($"<color=yellow>[DragonSegment] Non-destructible piece {SegmentIndex} was hit and ignored.</color>");
            return;
        }

        TakeDamage(damage, impactPoint, elementType);
    }

    /// <summary>
    /// Called when the player shoots this specific segment.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint, ElementTypeOB7 arrowType = ElementTypeOB7.Normal)
    {
        // Pass damage up to the brain so the overall boss loses health and can trigger evasions!
        if (bossBrain != null)
        {
            // The boss brain calculates actual damage using its own elemental modifiers
            bossBrain.TakeDamage(amount, hitPoint, arrowType);

            // Immediate tactical response: force an evasive routine right away when the dragon is hit.
            // This addresses cases where the dragon kept flying "like normal" while being shot.
            try
            {
                bossBrain.ForceImmediateEvasion();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DragonSegment] ForceImmediateEvasion failed: {ex}");
            }
        }

        // Only track local destruction if this is a breakable middle piece
        if (isDestructiblePart)
        {
            // Note: Currently, body segments just take raw base damage to pop off.
            // Elemental logic is managed centrally by the BossBrain above to control the overall health bar.
            health -= amount;
            Debug.Log($"<color=orange>[DragonSegment] Body Segment {SegmentIndex} took {amount} base damage. Local Health: {health}</color>");

            if (health <= 0)
            {
                Die();
            }
        }
        else
        {
            Debug.Log($"<color=yellow>[DragonSegment] Permanent piece {SegmentIndex} hit! Ignored (invulnerable).</color>");
        }
    }

    private void DestroyStuckArrows()
    {
        // Robust cleanup: find any ArrowFlightDynamics that are either children of this segment
        // OR appear physically inside this segment's renderer/collider bounds.
        try
        {
            // 1) Disable and destroy arrows parented under this segment (fast path)
            var localArrows = GetComponentsInChildren<ArrowFlightDynamics>(true);
            foreach (var s in localArrows)
            {
                if (s == null || s.gameObject == null) continue;
                var parentRb = s.GetComponentInParent<Rigidbody>();
                if (parentRb != null)
                    Destroy(parentRb.gameObject);
                else
                    Destroy(s.gameObject);
            }

            // 2) Also scan the whole scene for any ArrowFlightDynamics that may be overlapping
            // (covers cases where an arrow wasn't properly parented but stuck inside the mesh bounds)
            var allArrows = UnityEngine.Object.FindObjectsOfType<ArrowFlightDynamics>(true);
            Bounds? targetBounds = null;
            if (segmentRenderer != null)
            {
                targetBounds = segmentRenderer.bounds;
            }
            else if (segmentCollider != null)
            {
                targetBounds = segmentCollider.bounds;
            }

            foreach (var s in allArrows)
            {
                if (s == null || s.gameObject == null) continue;

                // Already handled above if it's a child
                if (s.transform.IsChildOf(transform)) continue;

                bool inside = false;
                if (targetBounds.HasValue)
                {
                    if (targetBounds.Value.Contains(s.transform.position)) inside = true;
                }
                else
                {
                    // If we don't have a renderer/collider bounds, fall back to a small overlap check
                    Collider[] hits = Physics.OverlapSphere(s.transform.position, 0.05f);
                    foreach (var h in hits)
                    {
                        if (h != null && h.transform.IsChildOf(transform))
                        {
                            inside = true;
                            break;
                        }
                    }
                }

                if (inside)
                {
                    var parentRb = s.GetComponentInParent<Rigidbody>();
                    if (parentRb != null)
                        Destroy(parentRb.gameObject);
                    else
                        Destroy(s.gameObject);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DragonSegment] Error while destroying stuck arrows: {ex}");
        }
    }

    private void Die()
    {
        if (isDying) return;

        if (!isDestructiblePart)
        {
            // Defensive: log if something attempted to destroy an invulnerable piece so you can trace root cause
            Debug.LogError($"[DragonSegment] Die() called on non-destructible piece index {SegmentIndex}. This indicates another system bypassed OnArrowHit checks.");
            return;
        }

        isDying = true;
        Debug.Log($"<color=red>[DragonSegment] Segment {SegmentIndex} Destroyed!</color>");

        // Disable physics immediately so arrows don't keep hitting the dying segment
        // Disable all colliders in the segment hierarchy to avoid invisible colliders remaining.
        try
        {
            var allColliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in allColliders)
            {
                if (col != null) col.enabled = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DragonSegment] Error disabling child colliders: {ex}");
        }

        // Trigger dissolve effect if present (use callback from DissolveEffect). If present, notify manager only after dissolve completes.
        DissolveEffect dissolve = null;
        if (segmentRenderer != null)
        {
            dissolve = segmentRenderer.GetComponentInParent<DissolveEffect>();
            if (dissolve == null)
            {
                // fallback: check on this GameObject
                dissolve = GetComponentInChildren<DissolveEffect>();
            }
        }
        else
        {
            dissolve = GetComponentInChildren<DissolveEffect>();
        }

        if (dissolve != null)
        {
            // Let the DissolveEffect manage timing and call us back when it's finished.
            // We destroy stuck arrows and notify the manager inside the callback so gap-closing waits for the visual dissolve.
            dissolve.TriggerDissolve(() =>
            {
                try
                {
                    // Ensure any stuck arrows are cleaned up after the visuals are gone
                    try
                    {
                        DestroyStuckArrows();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DragonSegment] Error destroying stuck arrows after dissolve: {ex}");
                    }

                    // Notify the body manager that this piece has visually dissolved
                    if (dragonManager != null)
                    {
                        try
                        {
                            dragonManager.NotifySegmentDissolved(this);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[DragonSegment] Error notifying manager of dissolved segment: {ex}");
                        }
                    }
                }
                finally
                {
                    try
                    {
                        Destroy(gameObject);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[DragonSegment] Error destroying segment after dissolve: {ex}");
                    }
                }
            });
        }
        else
        {
            // No dissolve component present -> clean arrows, notify manager and destroy immediately (no hidden timers)
            try
            {
                DestroyStuckArrows();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DragonSegment] Error destroying stuck arrows: {ex}");
            }

            if (dragonManager != null)
            {
                try
                {
                    dragonManager.NotifySegmentDissolved(this);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[DragonSegment] Error notifying manager of dissolved segment: {ex}");
                }
            }

            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by the SegmentedDragonManager when the entire boss is defeated.
    /// Forces permanent pieces (Head, Legs, Tail) to finally dissolve.
    /// </summary>
    public void TriggerTotalDeath()
    {
        if (isDying) return;
        isDying = true;

        // Disable all colliders so the object is non-interactable immediately
        try
        {
            var allColliders = GetComponentsInChildren<Collider>(true);
            foreach (var col in allColliders)
            {
                if (col != null) col.enabled = false;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[DragonSegment] Error disabling child colliders during total death: {ex}");
        }

        // Remove stuck arrows before final dissolve (they won't be useful after)
        DestroyStuckArrows();

        // Prefer dissolve callback if available
        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>() ?? segmentRenderer?.GetComponentInParent<DissolveEffect>();
        if (dissolve != null)
        {
            dissolve.TriggerDissolve(() => Destroy(gameObject));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called by RopeArrow when it becomes attached to this segment
    public void OnRopeAttached(RopeArrow rope)
    {
        if (dragonManager != null)
        {
            dragonManager.HandleRopeAttached(this, rope);
        }
    }

    // Called by RopeArrow when it is cleaned up / detached from this segment
    public void OnRopeDetached(RopeArrow rope)
    {
        if (dragonManager != null)
        {
            dragonManager.ReleaseTetherFromSegment(this, rope);
        }
    }
}
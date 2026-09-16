using UnityEngine;
using Dreamteck.Splines;

/// <summary>
/// Attached to individual segments (Head, Body, Tail) of the Asian Dragon Boss.
/// Handles segment health and reports destruction to the main SegmentedDragonManager.
/// </summary>
[RequireComponent(typeof(SplineFollower))]
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
    [SerializeField] public Collider segmentCollider;

    [Tooltip("The physics layer this segment will be forced onto so arrows can detect it. Displayed here as a reminder!")]
    [SerializeField] private string targetLayer = "Enemy";

    private SegmentedDragonManager dragonManager;
    private BossCreature bossBrain;
    private SplineFollower follower;

    // The index of this segment in the manager's list (Head = 0)
    public int SegmentIndex { get; set; }
    public SplineFollower Follower => follower;

    private void Awake()
    {
        follower = GetComponent<SplineFollower>();

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

    public void Initialize(SegmentedDragonManager manager, BossCreature brain, int index)
    {
        dragonManager = manager;
        bossBrain = brain;
        SegmentIndex = index;
    }

    public void OnRopeAttached(MonoBehaviour rope)
    {
        if (dragonManager != null)
        {
            // Will pass up if manager has it locally
        }
    }

    public void OnRopeDetached(MonoBehaviour rope)
    {
        if (dragonManager != null)
        {
            // Will pass up if manager has it locally
        }
    }

    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
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
            Debug.Log($"<color=yellow>[DragonSegment] Permanent piece {SegmentIndex} hit! Relayed {amount} base damage to Boss Brain.</color>");
        }
    }

    public float GetSegmentSize()
    {
        Renderer r = segmentRenderer != null ? segmentRenderer : GetComponentInChildren<Renderer>();
        if (r != null)
        {
            MeshFilter mf = r.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                return mf.sharedMesh.bounds.size.z * mf.transform.lossyScale.z;
            }
            return r.bounds.size.z;
        }
        return 2.0f;
    }

    protected virtual void Die()
    {
        if (!isDestructiblePart) return;

        Debug.Log($"<color=red>[DragonSegment] Segment {SegmentIndex} Destroyed!</color>");

        // Disable physics immediately so arrows don't keep hitting the dying segment
        if (segmentCollider != null)
        {
            segmentCollider.enabled = false;
            }

        // Clean up any arrows sticking out of this segment
        StickingArrow[] attachedArrows = GetComponentsInChildren<StickingArrow>(true);
        foreach (StickingArrow arrow in attachedArrows)
        {
            if (arrow != null)
            {
                Destroy(arrow.gameObject);
            }
        }

        // Find any "ArrowAnchor" objects that the StickingArrow might have parented directly
        foreach (Transform child in transform)
        {
            if (child.name.Contains("ArrowAnchor"))
            {
                Destroy(child.gameObject);
            }
        }

        // Notify the body manager that this piece is gone so it can close the gap
        if (dragonManager != null)
        {
            dragonManager.OnSegmentDestroyed(this);
        }

        // If there is a DissolveEffect, let it play and destroy the object after a delay
        // Otherwise, destroy immediately.
        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            dissolve.TriggerDissolve(() => Destroy(gameObject)); // Give time for the visual effect to play
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Called by the SegmentedDragonManager when the entire boss is defeated.
    /// Forces permanent pieces (Head, Legs, Tail) to finally dissolve.
    /// </summary>
        public virtual void TriggerTotalDeath(System.Action onComplete)
    {
        if (segmentCollider != null)
        {
            segmentCollider.enabled = false;
        }

        DissolveEffect effect = GetComponentInChildren<DissolveEffect>();
        if (effect != null)
        {
            effect.TriggerDissolve(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }
}

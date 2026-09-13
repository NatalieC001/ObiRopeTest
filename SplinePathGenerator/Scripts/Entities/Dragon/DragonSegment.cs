using UnityEngine;
using Dreamteck.Splines;

/// <summary>
/// Attached to individual segments (Head, Body, Tail) of the Asian Dragon Boss.
/// Handles segment health and reports destruction to the main SegmentedDragonManager.
/// </summary>
[RequireComponent(typeof(SplineFollower))]
public class DragonSegment : MonoBehaviour
{
    [Header("Segment Stats")]
    public float health = 100f;

    [Tooltip("The amount of power/energy this specific segment contributes to the boss's total power.")]
    public float powerContribution = 10f;

    [Header("Visuals & Physics")]
    [Tooltip("Reference to the child mesh renderer (useful for triggering visual effects).")]
    [SerializeField] private Renderer segmentRenderer;
    [Tooltip("Reference to the child collider (useful for disabling physics upon death).")]
    [SerializeField] private Collider segmentCollider;

    private SegmentedDragonManager dragonManager;
    private BossCreature bossBrain;
    private SplineFollower follower;

    // The index of this segment in the manager's list (Head = 0)
    public int SegmentIndex { get; set; }
    public SplineFollower Follower => follower;

    private void Awake()
    {
        follower = GetComponent<SplineFollower>();
    }

    public void Initialize(SegmentedDragonManager manager, BossCreature brain, int index)
    {
        dragonManager = manager;
        bossBrain = brain;
        SegmentIndex = index;
    }

    /// <summary>
    /// Called when the player shoots this specific segment.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        health -= amount;

        // Pass damage up to the brain so it can trigger evasions!
        if (bossBrain != null)
        {
            bossBrain.TakeDamage(amount, hitPoint);
        }

        Debug.Log($"<color=orange>[DragonSegment] Segment {SegmentIndex} took {amount} damage. Health: {health}</color>");

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"<color=red>[DragonSegment] Segment {SegmentIndex} Destroyed!</color>");

        // Disable physics immediately so arrows don't keep hitting the dying segment
        if (segmentCollider != null)
        {
            segmentCollider.enabled = false;
        }

        // Notify the brain that this piece is gone so it can close the gap
        if (dragonManager != null)
        {
            dragonManager.OnSegmentDestroyed(this);
        }

        // Note: Actual dissolve visual effects are handled by DissolveEffect.cs
        // acting on the segmentRenderer. We just destroy the root object.
        Destroy(gameObject);
    }
}

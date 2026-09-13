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

    private SegmentedDragonManager dragonManager;
    private SplineFollower follower;

    // The index of this segment in the manager's list (Head = 0)
    public int SegmentIndex { get; set; }
    public SplineFollower Follower => follower;

    private void Awake()
    {
        follower = GetComponent<SplineFollower>();
    }

    public void Initialize(SegmentedDragonManager manager, int index)
    {
        dragonManager = manager;
        SegmentIndex = index;
    }

    /// <summary>
    /// Called when the player shoots this specific segment.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        health -= amount;

        Debug.Log($"<color=orange>[DragonSegment] Segment {SegmentIndex} took {amount} damage. Health: {health}</color>");

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"<color=red>[DragonSegment] Segment {SegmentIndex} Destroyed!</color>");

        // Notify the brain that this piece is gone so it can close the gap
        if (dragonManager != null)
        {
            dragonManager.OnSegmentDestroyed(this);
        }

        // Trigger explosion/dissolve effects here
        Destroy(gameObject);
    }
}

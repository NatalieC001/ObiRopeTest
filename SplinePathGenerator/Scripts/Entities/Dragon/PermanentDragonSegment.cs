using UnityEngine;

/// <summary>
/// A specialized version of DragonSegment specifically for the Head, Legs, and Tail.
/// It overrides standard death logic to ensure it can NEVER be destroyed mid-fight,
/// but still takes damage and relays it to the boss. It only dies when TriggerTotalDeath is called.
/// </summary>
public class PermanentDragonSegment : DragonSegment
{
    private void Start()
    {
        // Force this to be indestructible just in case the manager didn't catch it
        isDestructiblePart = false;
    }

    protected override void Die()
    {
        // Do absolutely nothing. Permanent segments cannot be destroyed mid-fight!
        Debug.Log($"<color=yellow>[PermanentDragonSegment] Segment {SegmentIndex} tried to die, but it is permanent!</color>");
    }

    public override void TriggerTotalDeath()
    {
        // Now it is allowed to die because the Boss's health hit 0!
        if (segmentCollider != null)
        {
            segmentCollider.enabled = false;
        }

        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            // Trigger visual effect, then destroy the object
            dissolve.TriggerDissolve(() =>
            {
                Destroy(gameObject);
            });
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

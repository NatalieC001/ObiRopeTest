using UnityEngine;

/// <summary>
/// A specialized version of DragonSegment specifically for the Head, Legs, and Tail.
/// It overrides standard death logic to ensure it can NEVER be destroyed mid-fight,
/// but still takes damage and relays it to the boss. It only dies when TriggerTotalDeath is called.
/// </summary>
public class PermanentDragonSegment : DragonSegment
{
    private void Awake()
    {
        // Force this to be indestructible before SegmentedDragonManager checks it in SpawnSegment()
        isDestructiblePart = false;
    }

    private void Start()
    {
        // Ensure DissolveEffect waits for TriggerTotalDeath
        DissolveEffect effect = GetComponentInChildren<DissolveEffect>();
        if (effect != null)
        {
            // Safely toggle the private field so this piece doesn't accidentally bypass health logic and visually explode when shot
            System.Reflection.FieldInfo field = effect.GetType().GetField("dissolveImmediatelyOnHit", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(effect, false);
            }
        }
    }

    protected override void Die()
    {
        // Do absolutely nothing. Permanent segments cannot be destroyed mid-fight!
        Debug.Log($"<color=yellow>[PermanentDragonSegment] Segment {SegmentIndex} tried to die, but it is permanent!</color>");
    }

    public override void TriggerTotalDeath(System.Action onComplete)
    {
        // Now it is allowed to die because the Boss's health hit 0!
        Collider childCollider = GetComponentInChildren<Collider>();
        if (childCollider != null)
        {
            childCollider.enabled = false;
        }

        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            // Trigger visual effect, and pass the manager's callback so the root can track it
            dissolve.TriggerDissolve(onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }
}

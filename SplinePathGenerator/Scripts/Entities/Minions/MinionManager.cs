using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A dedicated manager to handle the lifecycle and destruction of StandardCreatures.
/// Provides a centralized place to clean up minions exactly like SegmentedDragonManager handles body segments.
/// </summary>
public class MinionManager : MonoBehaviour
{
    // Tracking lists can be added here if needed for broader swarm behaviors
    public List<StandardCreature> activeMinions = new List<StandardCreature>();

    /// <summary>
    /// Called by a minion exactly when its visual dissolve finishes.
    /// Safely purges it from any tracking arrays so it can destroy itself cleanly.
    /// </summary>
    public void OnMinionDestroyed(StandardCreature minion)
    {
        if (minion != null)
        {
            Debug.Log($"<color=magenta>[MinionManager] Removing minion {minion.gameObject.name} from tracking arrays.</color>");
            activeMinions.Remove(minion);
        }
    }

    public void RegisterMinion(StandardCreature minion)
    {
        if (minion != null && !activeMinions.Contains(minion))
        {
            activeMinions.Add(minion);
        }
    }
}

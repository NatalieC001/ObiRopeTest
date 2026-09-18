using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Listens specifically for minion deaths of minions requested by the boss.
/// Notifies the BossEventBus when they die so the Brain loses power.
/// </summary>
public class MinionDeathRelay : MonoBehaviour
{
    private BossEventBus eventBus;
    private HashSet<GameObject> trackedMinions = new HashSet<GameObject>();

    private void Awake()
    {
        eventBus = FindFirstObjectByType<BossEventBus>();
    }

    public void RegisterBossMinion(GameObject minion)
    {
        if (minion != null && !trackedMinions.Contains(minion))
        {
            trackedMinions.Add(minion);
            // In a real project, we would subscribe to the minion's death event here.
            // For now, we simulate checking if it's destroyed via Update.
        }
    }

    private void Update()
    {
        // Simple polling for null/destroyed gameobjects since we aren't editing StandardCreature right now.
        // In full integration, StandardCreature should call a public method here on death.
        List<GameObject> deadMinions = new List<GameObject>();
        foreach (var m in trackedMinions)
        {
            if (m == null)
            {
                deadMinions.Add(m);
            }
        }

        foreach (var dm in deadMinions)
        {
            trackedMinions.Remove(dm);
            if (eventBus != null)
            {
                eventBus.TriggerBossMinionDied(dm);
            }
        }
    }
}

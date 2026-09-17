using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A centralized manager to track all active StandardCreatures (Minions).
/// When a minion finishes its dissolve sequence, it notifies this manager,
/// which then explicitly notifies the WaveSpawner
/// before obliterating the minion GameObject AND its empty root wrappers.
/// </summary>
public class MinionManager : MonoBehaviour
{
    private List<StandardCreature> activeMinions = new List<StandardCreature>();

    [Tooltip("The spawner that needs to be notified when a minion dies to progress the wave.")]
    [SerializeField] private WaveSpawner waveSpawner;

    private void Awake()
    {
        // Failsafe in case it's not wired in the inspector
        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<WaveSpawner>();
        }
    }

    /// <summary>
    /// Registers a newly spawned minion.
    /// </summary>
    public void RegisterMinion(StandardCreature minion)
    {
        if (minion != null && !activeMinions.Contains(minion))
        {
            activeMinions.Add(minion);
        }
    }

    /// <summary>
    /// Called EXACTLY when a minion's dissolve animation finishes.
    /// Safely purges it from tracking, notifies the wave manager explicitly, and destroys it.
    /// </summary>
    public void OnMinionDestroyed(StandardCreature destroyedMinion)
    {
        if (activeMinions.Contains(destroyedMinion))
        {
            activeMinions.Remove(destroyedMinion);
        }

        Debug.Log($"<color=magenta>[MinionManager] Minion destroyed. Remaining: {activeMinions.Count}</color>");

        // Explicitly notify the WaveSpawner so it can evaluate wave completion instantly!
        if (waveSpawner != null)
        {
            waveSpawner.NotifyTargetDestroyed();
        }

        // Permanently destroy the minion object.
        // Root object cleanup is handled natively by the WaveSpawner at the end of the wave/level.
        if (destroyedMinion != null && destroyedMinion.gameObject != null)
        {
            Destroy(destroyedMinion.gameObject);
        }
    }
}

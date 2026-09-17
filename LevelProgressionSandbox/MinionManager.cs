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

    private void OnEnable()
    {
        StandardCreature.OnCreatureSpawned += HandleCreatureSpawned;
        StandardCreature.OnCreatureDied += HandleCreatureDied;
    }

    private void OnDisable()
    {
        StandardCreature.OnCreatureSpawned -= HandleCreatureSpawned;
        StandardCreature.OnCreatureDied -= HandleCreatureDied;
    }

    private void HandleCreatureSpawned(StandardCreature minion)
    {
        if (minion != null && !activeMinions.Contains(minion))
        {
            activeMinions.Add(minion);

            if (waveSpawner != null)
            {
                waveSpawner.NotifyTargetRegistered();
            }
        }
    }

    private void HandleCreatureDied(StandardCreature destroyedMinion)
    {
        if (activeMinions.Contains(destroyedMinion))
        {
            activeMinions.Remove(destroyedMinion);
        }

        Debug.Log($"<color=magenta>[MinionManager] Minion destroyed. Remaining: {activeMinions.Count}</color>");

        if (waveSpawner != null)
        {
            waveSpawner.NotifyTargetDestroyed();
        }

        // Destroy the individual minion.
        // We DO NOT destroy the root here because nested swarms share the same root.
        // Empty swarm roots will be cleaned up safely by WaveSpawner at the end of the wave.
        if (destroyedMinion != null && destroyedMinion.gameObject != null)
        {
            Destroy(destroyedMinion.gameObject);
        }
    }
}

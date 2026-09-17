using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A centralized manager to track all active StandardCreatures (Minions).
/// When a minion finishes its dissolve sequence, it notifies this manager,
/// which then explicitly notifies the TrainingLevelManager (if one exists)
/// before obliterating the minion GameObject.
/// </summary>
public class MinionManager : MonoBehaviour
{
    private List<StandardCreature> activeMinions = new List<StandardCreature>();
    private TrainingLevelManager trainingManager;

    private void Awake()
    {
        // Try to find the TrainingLevelManager in the scene to notify it of wave progression
        trainingManager = Object.FindAnyObjectByType<TrainingLevelManager>();
    }

    /// <summary>
    /// Registers a newly spawned minion.
    /// </summary>
    public void RegisterMinion(StandardCreature minion)
    {
        if (minion != null && !activeMinions.Contains(minion))
        {
            activeMinions.Add(minion);
            // Debug.Log($"[MinionManager] Registered Minion: {minion.gameObject.name}. Total: {activeMinions.Count}");
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

        // Explicitly notify the TrainingLevelManager so it can evaluate wave completion instantly!
        if (trainingManager != null)
        {
            trainingManager.NotifyTargetDestroyed(destroyedMinion.gameObject);
        }

        // Permanently destroy the root object
        if (destroyedMinion != null && destroyedMinion.gameObject != null)
        {
            Destroy(destroyedMinion.gameObject);
        }
    }
}

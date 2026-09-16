using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A dedicated manager to handle the lifecycle and destruction of StandardCreatures.
/// Provides a centralized place to clean up minions exactly like SegmentedDragonManager handles body segments.
/// </summary>
public class MinionManager : MonoBehaviour
{
    private static MinionManager instance;

    public static MinionManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MinionManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MinionManager");
                    instance = go.AddComponent<MinionManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// Called by a minion exactly when its visual dissolve finishes.
    /// Safely purges it and destroys the root GameObject hierarchy.
    /// </summary>
    public void OnMinionDestroyed(StandardCreature minion)
    {
        if (minion != null)
        {
            Debug.Log($"<color=magenta>[MinionManager] Removing minion {minion.gameObject.name} from the scene cleanly.</color>");

            // TrainingLevelManager's Update loop will detect the null and progress the wave automatically
            Destroy(minion.gameObject);
        }
    }
}

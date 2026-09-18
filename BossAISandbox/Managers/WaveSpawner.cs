using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Scene-level authority for spawning enemies.
/// Expanded to include a boss minion roster that the Dragon queries.
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Level Progression")]
    public int activeEnemyCount = 0;

    [Header("Boss Minion Roster")]
    [Tooltip("List of prefabs the boss is allowed to request. The boss doesn't care about tiers, it just picks from here.")]
    public List<GameObject> bossMinionRoster = new List<GameObject>();

    public GameObject SpawnBossMinion(Vector3 position, Quaternion rotation)
    {
        if (bossMinionRoster == null || bossMinionRoster.Count == 0)
        {
            Debug.LogWarning("[WaveSpawner] Boss requested a minion, but the boss minion roster is empty!");
            return null;
        }

        // Randomly pick from roster for now
        int idx = Random.Range(0, bossMinionRoster.Count);
        GameObject prefab = bossMinionRoster[idx];

        if (prefab != null)
        {
            GameObject minion = Instantiate(prefab, position, rotation);
            activeEnemyCount++;
            return minion;
        }

        return null;
    }

    public void NotifyEnemyDestroyed()
    {
        activeEnemyCount--;
        if (activeEnemyCount < 0) activeEnemyCount = 0;
    }
}

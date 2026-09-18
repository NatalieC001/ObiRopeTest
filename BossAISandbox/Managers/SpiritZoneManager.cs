using UnityEngine;
using System.Collections.Generic;

public class SpiritZoneManager : MonoBehaviour
{
    [Header("Zone Spawning")]
    public GameObject spiritCloudPrefab;
    public int maxActiveClouds = 3;

    private List<GameObject> activeClouds = new List<GameObject>();
    private EnvironmentTagRegistry registry;

    private void Awake()
    {
        registry = FindFirstObjectByType<EnvironmentTagRegistry>();
    }

    public void TryPlaceZone(Vector3 playerOrTargetPosition)
    {
        activeClouds.RemoveAll(c => c == null);

        if (activeClouds.Count >= maxActiveClouds)
        {
            Debug.Log("[SpiritZoneManager] At cloud capacity. Reserving breath.");
            return;
        }

        if (registry == null) return;

        // Smart Placement: Find nearest SpiritAnchor or Chokepoint to cut off the player
        List<EnvironmentTag> anchors = registry.GetTagsWithinRadius(playerOrTargetPosition, 30f, EnvironmentTag.TagType.SpiritAnchor);
        List<EnvironmentTag> chokepoints = registry.GetTagsWithinRadius(playerOrTargetPosition, 30f, EnvironmentTag.TagType.Chokepoint);

        anchors.AddRange(chokepoints);

        EnvironmentTag bestTag = null;
        float bestScore = -1f;

        foreach (var tag in anchors)
        {
            // Score based on distance to player (closer is better, but not exactly on them if predicting)
            float dist = Vector3.Distance(playerOrTargetPosition, tag.transform.position);
            float score = 100f - dist; // Simplified score

            // Check if too close to an existing cloud
            bool tooClose = false;
            foreach (var cloud in activeClouds)
            {
                if (Vector3.Distance(cloud.transform.position, tag.transform.position) < 5f)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose && score > bestScore)
            {
                bestScore = score;
                bestTag = tag;
            }
        }

        if (bestTag != null && spiritCloudPrefab != null)
        {
            GameObject newCloud = Instantiate(spiritCloudPrefab, bestTag.transform.position, Quaternion.identity);
            activeClouds.Add(newCloud);
            Debug.Log($"[SpiritZoneManager] Placed Dark Spirit Cloud at {bestTag.name}");
        }
    }
}

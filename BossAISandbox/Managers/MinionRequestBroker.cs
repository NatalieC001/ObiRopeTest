using UnityEngine;

public enum SpawnIntent
{
    Block,
    Flank,
    Rearguard,
    AllIn,
    Ambush,
    DefendCrystal,
    Circle
}

public class MinionRequestBroker : MonoBehaviour
{
    [Header("Economy")]
    public int maxReserve = 5;
    private int currentReserve;
    public float requestCooldown = 5f;
    private float cooldownTimer = 0f;

    private WaveSpawner waveSpawner;
    private MinionDeathRelay deathRelay;
    private EnvironmentTagRegistry registry;

    private void Awake()
    {
        currentReserve = maxReserve;
        waveSpawner = FindFirstObjectByType<WaveSpawner>();
        deathRelay = FindFirstObjectByType<MinionDeathRelay>();
        registry = FindFirstObjectByType<EnvironmentTagRegistry>();
    }

    private void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
    }

    public bool RequestMinions(SpawnIntent intent, Vector3 targetFocus)
    {
        if (currentReserve <= 0 || cooldownTimer > 0f || waveSpawner == null || registry == null)
        {
            return false;
        }

        Vector3 spawnPos = targetFocus; // default

        // Query tag registry to translate intent into location
        if (intent == SpawnIntent.Block)
        {
            EnvironmentTag cp = registry.GetNearestTag(targetFocus, EnvironmentTag.TagType.Chokepoint);
            if (cp != null) spawnPos = cp.transform.position;
        }
        else if (intent == SpawnIntent.Ambush || intent == SpawnIntent.Flank)
        {
            EnvironmentTag cover = registry.GetNearestTag(targetFocus, EnvironmentTag.TagType.HardCover);
            if (cover != null) spawnPos = cover.transform.position;
        }
        else if (intent == SpawnIntent.DefendCrystal)
        {
            EnvironmentTag loop = registry.GetNearestTag(targetFocus, EnvironmentTag.TagType.ObservationLoop);
            if (loop != null) spawnPos = loop.transform.position;
        }

        GameObject minion = waveSpawner.SpawnBossMinion(spawnPos, Quaternion.identity);

        if (minion != null)
        {
            currentReserve--;
            cooldownTimer = requestCooldown;

            if (deathRelay != null)
            {
                deathRelay.RegisterBossMinion(minion);
            }
            return true;
        }
        return false;
    }

    public void RefillReserve()
    {
        currentReserve = maxReserve;
        Debug.Log("[MinionRequestBroker] Reserve refilled via regeneration.");
    }
}

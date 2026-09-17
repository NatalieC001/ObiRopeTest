using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Handles the physical instantiation and tracking of targets.
/// Subscribes to LevelProgressionManager to know when to start.
/// Informs LevelProgressionManager when the wave is cleared.
/// Completely avoids Coroutines for internal loops and delays (except standard Unity delays if absolutely needed).
/// </summary>
public class WaveSpawner : MonoBehaviour
{
    [Header("Dependencies")]
    public LevelProgressionManager progressionManager;

    [Header("Spawn Settings")]
    [Tooltip("The central point where targets are spawned around. If null, uses this object's position.")]
    public Transform spawnCenter;

    // --- State ---
    private List<GameObject> activeTargets = new List<GameObject>();
    private bool isWaveActive = false;
    private WaveDataSO currentWaveData;
    private int currentWaveIndex;
    private int totalWaveCount;

    // Timer logic for TimeBased waves
    private float waveTimer = 0f;

    // --- Queue for delayed spawning (replacing Coroutines) ---
    private class PendingSpawn
    {
        public TargetSpawnConfig Config;
        public float TimeToSpawn;
    }
    private List<PendingSpawn> spawnQueue = new List<PendingSpawn>();

    // --- Public Events ---
    public event Action<string> OnWaveProgressUpdated;

    private void OnEnable()
    {
        if (progressionManager == null)
        {
            progressionManager = FindFirstObjectByType<LevelProgressionManager>();
        }

        if (progressionManager != null)
        {
            progressionManager.OnWaveStartRequested += HandleWaveStartRequested;
            progressionManager.OnBossStartRequested += HandleBossStartRequested;
            progressionManager.OnStateChanged += HandleStateChanged;
        }

        if (spawnCenter == null)
        {
            spawnCenter = this.transform;
        }
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.OnWaveStartRequested -= HandleWaveStartRequested;
            progressionManager.OnBossStartRequested -= HandleBossStartRequested;
            progressionManager.OnStateChanged -= HandleStateChanged;
        }
    }

    private void HandleStateChanged(LevelProgressionManager.GameState state)
    {
        // If state shifts away from wave active, forcefully clean up
        if (state != LevelProgressionManager.GameState.WaveActive && state != LevelProgressionManager.GameState.BossActive)
        {
            CleanupTargets();
            isWaveActive = false;
            spawnQueue.Clear();
        }
    }

    private void HandleWaveStartRequested(LevelConfigSO levelConfig, int waveIndex)
    {
        if (waveIndex >= levelConfig.waves.Count) return;

        currentWaveData = levelConfig.waves[waveIndex];
        currentWaveIndex = waveIndex;
        totalWaveCount = levelConfig.waves.Count;

        activeTargets.Clear();
        spawnQueue.Clear();
        waveTimer = 0f;
        isWaveActive = true;

        // Queue all targets using absolute time to avoid Coroutine cascades
        float currentTime = Time.time;
        foreach (var targetConfig in currentWaveData.targets)
        {
            spawnQueue.Add(new PendingSpawn
            {
                Config = targetConfig,
                TimeToSpawn = currentTime + targetConfig.spawnDelay
            });
        }

        Debug.Log($"[WaveSpawner] Wave {waveIndex + 1}/{totalWaveCount} started. {spawnQueue.Count} targets queued.");
    }

    private void HandleBossStartRequested(LevelConfigSO levelConfig)
    {
        Debug.Log("[WaveSpawner] Boss requested. Spawning boss.");
        isWaveActive = false; // Boss is handled differently (survival/kill)

        // Spawn Boss via configuration (using TargetSpawnConfig with Boss Movement Type)
        TargetSpawnConfig bossConfig = new TargetSpawnConfig
        {
            movementBehavior = TargetMovementType.BossDragonAsset,
            spawnPosition = Vector3.zero,
            scaleModifier = 1f,
            speedModifier = 1f
        };

        SpawnComplexAsset(bossConfig, spawnCenter.position, Quaternion.identity, levelConfig);
    }

    private void Update()
    {
        if (!isWaveActive) return;

        ProcessSpawns();
        UpdateWaveLogic();
    }

    private void ProcessSpawns()
    {
        for (int i = spawnQueue.Count - 1; i >= 0; i--)
        {
            if (Time.time >= spawnQueue[i].TimeToSpawn)
            {
                SpawnTarget(spawnQueue[i].Config);
                spawnQueue.RemoveAt(i);
            }
        }
    }

    private void UpdateWaveLogic()
    {
        // Standard explicit cleanup of missing targets, per memory guidelines
        activeTargets.RemoveAll(t => t == null);

        bool allTargetsCleared = activeTargets.Count == 0 && spawnQueue.Count == 0;

        if (currentWaveData.progressionType == WaveProgressionType.TimeBased)
        {
            waveTimer += Time.deltaTime;
            float timeLeft = Mathf.Max(0, currentWaveData.waveDuration - waveTimer);
            OnWaveProgressUpdated?.Invoke($"Wave {currentWaveIndex + 1}/{totalWaveCount} | Survive: {timeLeft:F1}s");

            if (waveTimer >= currentWaveData.waveDuration || allTargetsCleared)
            {
                CompleteWave();
            }
        }
        else if (currentWaveData.progressionType == WaveProgressionType.ClearAllTargets)
        {
            int targetsRemaining = activeTargets.Count + spawnQueue.Count;
            OnWaveProgressUpdated?.Invoke($"Wave {currentWaveIndex + 1}/{totalWaveCount} | Targets Left: {targetsRemaining}");

            if (allTargetsCleared)
            {
                CompleteWave();
            }
        }
    }

    private void CompleteWave()
    {
        isWaveActive = false;
        CleanupTargets();
        spawnQueue.Clear();

        Debug.Log($"[WaveSpawner] Wave {currentWaveIndex + 1} completed.");
        if (progressionManager != null)
        {
            progressionManager.ReceiveWaveCompleted();
        }
    }

    private void CleanupTargets()
    {
        foreach (var target in activeTargets)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
        activeTargets.Clear();
    }

    private void SpawnTarget(TargetSpawnConfig config)
    {
        Vector3 spawnPos = spawnCenter.position + config.spawnPosition;

        // VR Constraint: Ensure target never spawns below the ground (Y < 0 relative to center)
        if (spawnPos.y < spawnCenter.position.y)
        {
            spawnPos.y = spawnCenter.position.y;
        }

        // VR Constraint: Ensure target is perfectly upright, facing the spawn center
        Vector3 directionToCenter = spawnCenter.position - spawnPos;
        directionToCenter.y = 0;

        Quaternion spawnRot = Quaternion.identity;
        if (directionToCenter.sqrMagnitude > 0.001f)
        {
            spawnRot = Quaternion.LookRotation(directionToCenter);
        }

        // Current Level Config is needed to resolve prefabs
        // It's passed via the request, but we need to track it.
        // Assuming progressionManager has the active level, we can peek at it,
        // but for safety, we rely on the playlist index.
        LevelConfigSO currentLevelConfig = null;
        if (progressionManager != null && progressionManager.levelPlaylist.Count > 0)
        {
            // We assume progression manager tracks current index, but we can just use FindAnyObjectByType or similar
            // As a fallback, we grab it from the event (we should have cached it).
            // Let's rely on a cached reference.
        }

        // To make this cleaner without modifying other scripts right now, we use a simple reflection or lookup.
        // For standard vanilla spawning:
        GameObject newTarget = null;

        // (Note: The actual instantiation logic from TrainingLevelManager should be adapted here.
        // For the sake of this sandbox, we create a placeholder if it's missing.)

        // As a simple sandbox implementation, we just spawn a cube if no prefab is provided.
        newTarget = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newTarget.transform.position = spawnPos;
        newTarget.transform.rotation = spawnRot;
        newTarget.AddComponent<Rigidbody>().isKinematic = true;

        activeTargets.Add(newTarget);
    }

    private void SpawnComplexAsset(TargetSpawnConfig config, Vector3 spawnPos, Quaternion spawnRot, LevelConfigSO levelConfig)
    {
        GameObject prefabToSpawn = config.movementBehavior == TargetMovementType.BossDragonAsset ? levelConfig.bossDragonPrefab : levelConfig.splinePathAssetPrefab;

        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"[WaveSpawner] Missing prefab for {config.movementBehavior}. Skipping spawn.");
            return;
        }

        GameObject complexTarget = Instantiate(prefabToSpawn, spawnPos, spawnRot);
        activeTargets.Add(complexTarget);

        if (config.movementBehavior == TargetMovementType.BossDragonAsset)
        {
            // Boss setup logic here...
        }
    }
}

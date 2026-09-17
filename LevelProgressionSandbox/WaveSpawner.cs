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
    private WaveData currentWaveData;
    private LevelConfigSO currentLevelConfig;
    private int currentWaveIndex;
    private int totalWaveCount;

    // Timer logic for TimeBased waves
    private float waveTimer = 0f;

    private BossPathManager pathManager;

    // --- Queue for delayed spawning (replacing Coroutines) ---
    private class PendingSpawn
    {
        public CharacterConfigBase Config;
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
            progressionManager.OnStateChanged += HandleStateChanged;
        }

        if (spawnCenter == null)
        {
            spawnCenter = this.transform;
        }

        pathManager = FindFirstObjectByType<BossPathManager>();
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.OnWaveStartRequested -= HandleWaveStartRequested;
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

        currentLevelConfig = levelConfig;
        currentWaveData = levelConfig.waves[waveIndex];
        currentWaveIndex = waveIndex;
        totalWaveCount = levelConfig.waves.Count;

        activeTargets.Clear();
        spawnQueue.Clear();
        waveTimer = 0f;
        isWaveActive = true;

        // Queue all characters using absolute time to avoid Coroutine cascades
        float currentTime = Time.time;
        bool isBossWave = false;

        foreach (var charConfig in currentWaveData.characters)
        {
            if (charConfig is BossConfig)
            {
                isBossWave = true;
            }

            spawnQueue.Add(new PendingSpawn
            {
                Config = charConfig,
                TimeToSpawn = currentTime + charConfig.spawnDelay
            });
        }

        if (isBossWave)
        {
            Debug.Log($"[WaveSpawner] Boss Wave {waveIndex + 1}/{totalWaveCount} started. Spawning paths.");

            // Extract Boss Paths and register them globally BEFORE the boss spawns
            foreach (var charConfig in currentWaveData.characters)
            {
                if (charConfig is BossConfig boss)
                {
                    InstantiateAndRegisterPaths(boss.observationPathPrefabs);
                    InstantiateAndRegisterPaths(boss.escapePathPrefabs);
                }
            }

            progressionManager.NotifyBossWaveStarted();
            // We set wave active to false because Boss combat lifecycle is managed via BossArenaManager
            isWaveActive = false;
        }
        else
        {
            Debug.Log($"[WaveSpawner] Wave {waveIndex + 1}/{totalWaveCount} started. {spawnQueue.Count} targets queued.");
        }
    }

    private void Update()
    {
        // Spawning must process independently of wave logic so that Bosses (who set isWaveActive = false) still instantiate.
        ProcessSpawns();

        if (!isWaveActive) return;
        UpdateWaveLogic();
    }

    private void ProcessSpawns()
    {
        for (int i = spawnQueue.Count - 1; i >= 0; i--)
        {
            if (Time.time >= spawnQueue[i].TimeToSpawn)
            {
                SpawnCharacter(spawnQueue[i].Config);
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

        if (pathManager != null)
        {
            pathManager.ClearAllPaths();
        }
    }

    private void InstantiateAndRegisterPaths(List<GameObject> pathPrefabs)
    {
        if (pathManager == null)
        {
            Debug.LogWarning("[WaveSpawner] No BossPathManager found in scene. Paths will be spawned but un-registered.");
        }

        foreach (var prefab in pathPrefabs)
        {
            if (prefab != null)
            {
                // Instantiate the path into the scene
                GameObject spawnedPath = Instantiate(prefab, spawnCenter.position, Quaternion.identity);

                // Track it locally so it can be cleaned up at wave end
                activeTargets.Add(spawnedPath);

                // Register it with the global source of truth
                if (pathManager != null)
                {
                    pathManager.RegisterPath(spawnedPath);
                }
            }
        }
    }

    private void SpawnCharacter(CharacterConfigBase config)
    {
        Vector3 spawnPos = spawnCenter.position + config.spawnPositionOffset;

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

        GameObject prefabToSpawn = null;

        if (config is MinionConfig minion)
        {
            prefabToSpawn = minion.prefab;
        }
        else if (config is BossConfig boss)
        {
            prefabToSpawn = boss.prefab;
        }

        if (prefabToSpawn == null)
        {
            Debug.LogError($"[WaveSpawner] Prefab is missing from the Character Config!");
            return;
        }

        GameObject spawnedEntity = Instantiate(prefabToSpawn, spawnPos, spawnRot);

        if (config is MinionConfig)
        {
            activeTargets.Add(spawnedEntity);
        }
        else if (config is BossConfig)
        {
            // Boss wiring logic typically offloaded to BossArenaManager
            Debug.Log("[WaveSpawner] Spawned Boss Entity.");
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Manages the progression of levels and waves for the archery training range.
/// Reads data from a LevelConfigSO and spawns targets based on WaveDataSO configurations.
/// </summary>
public class TrainingLevelManager : MonoBehaviour
{
    [Header("Level Configuration")]
    [Tooltip("The level configuration to run.")]
    public LevelConfigSO currentLevelConfig;

    [Header("Spawn Settings")]
    [Tooltip("The central point where targets are spawned around. If null, uses the manager's position.")]
    public Transform spawnCenter;

    private int currentWaveIndex = 0;
    private List<GameObject> activeTargets = new List<GameObject>();
    private bool isWaveActive = false;
    private float waveTimer = 0f;

    // Track active spawn coroutines so we can cancel them if the wave ends early
    private List<Coroutine> activeSpawnCoroutines = new List<Coroutine>();

    // Events for external systems (e.g., UI, scoring)
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCompleted;
    public event Action OnLevelCompleted;

    private void Start()
    {
        if (spawnCenter == null)
        {
            spawnCenter = this.transform;
        }

        if (currentLevelConfig != null)
        {
            StartLevel(currentLevelConfig);
        }
        else
        {
            Debug.LogWarning("[TrainingLevelManager] No LevelConfigSO assigned!");
        }
    }

    private void Update()
    {
        if (!isWaveActive) return;

        WaveDataSO currentWave = currentLevelConfig.waves[currentWaveIndex];

        // Check time-based progression
        if (currentWave.progressionType == WaveProgressionType.TimeBased)
        {
            waveTimer += Time.deltaTime;
            if (waveTimer >= currentWave.waveDuration)
            {
                CompleteCurrentWave();
            }
        }
        else if (currentWave.progressionType == WaveProgressionType.ClearAllTargets)
        {
            // Clean up list of destroyed targets
            activeTargets.RemoveAll(t => t == null);

            // If all targets are cleared, wave is complete
            if (activeTargets.Count == 0 && activeSpawnCoroutines.Count == 0)
            {
                CompleteCurrentWave();
            }
        }
    }

    /// <summary>
    /// Starts a new level from the provided configuration.
    /// </summary>
    public void StartLevel(LevelConfigSO levelConfig)
    {
        Debug.Log($"[TrainingLevelManager] Starting Level: {levelConfig.levelName}");
        currentLevelConfig = levelConfig;
        currentWaveIndex = 0;

        if (currentLevelConfig.waves.Count > 0)
        {
            StartWave(currentWaveIndex);
        }
        else
        {
            Debug.LogWarning("[TrainingLevelManager] Level has no waves configured.");
            OnLevelCompleted?.Invoke();
        }
    }

    private void StartWave(int index)
    {
        if (index >= currentLevelConfig.waves.Count)
        {
            Debug.Log("[TrainingLevelManager] All waves completed!");
            OnLevelCompleted?.Invoke();
            return;
        }

        WaveDataSO waveData = currentLevelConfig.waves[index];
        Debug.Log($"[TrainingLevelManager] Starting Wave {index + 1}/{currentLevelConfig.waves.Count}. Progression: {waveData.progressionType}");

        activeTargets.Clear();
        activeSpawnCoroutines.Clear();
        waveTimer = 0f;
        isWaveActive = true;

        OnWaveStarted?.Invoke(index);

        // Start spawning targets - each target gets its own coroutine to fix cumulative delay issues
        foreach (var targetConfig in waveData.targets)
        {
            Coroutine spawnRoutine = StartCoroutine(SpawnTargetWithDelay(targetConfig));
            activeSpawnCoroutines.Add(spawnRoutine);
        }
    }

    private IEnumerator SpawnTargetWithDelay(TargetSpawnConfig targetConfig)
    {
        if (targetConfig.spawnDelay > 0)
        {
            yield return new WaitForSeconds(targetConfig.spawnDelay);
        }

        SpawnTarget(targetConfig);

        // Remove from active tracking once spawned
        // Note: Using null check for safety within Coroutines
        activeSpawnCoroutines.RemoveAll(c => c == null);
    }

    private void SpawnTarget(TargetSpawnConfig config)
    {
        if (currentLevelConfig.vanillaTargetPrefab == null)
        {
            Debug.LogError("[TrainingLevelManager] Vanilla Target Prefab is not set in LevelConfigSO!");
            return;
        }

        Vector3 spawnPos = spawnCenter.position + config.spawnPosition;
        GameObject newTarget = Instantiate(currentLevelConfig.vanillaTargetPrefab, spawnPos, Quaternion.identity);

        // 1. Apply global and local scaling
        float finalScale = currentLevelConfig.globalScaleMultiplier * config.scaleModifier;
        if (finalScale <= 0) finalScale = 1.0f; // Prevent scale 0
        newTarget.transform.localScale = Vector3.one * finalScale;

        // 2. Try to setup the MovingTarget component (Color requirement)
        MovingTarget movingTarget = newTarget.GetComponent<MovingTarget>();
        if (movingTarget != null)
        {
            movingTarget.SetRequiredElement(config.requiredArrowElement);
            Debug.Log($"[TrainingLevelManager] Target spawned. Needs Element: {config.requiredArrowElement}");
        }

        // 3. Dynamically add movement script (Searching across all assemblies for .asmdef support)
        if (!string.IsNullOrEmpty(config.movementScriptName))
        {
            Type movementType = FindTypeInAllAssemblies(config.movementScriptName);

            if (movementType != null && typeof(MonoBehaviour).IsAssignableFrom(movementType))
            {
                Component movementComp = newTarget.AddComponent(movementType);

                // If the movement script has a "speed" property, try to set it via reflection
                var speedField = movementType.GetField("speed", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                var speedProp = movementType.GetProperty("Speed", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                float finalSpeed = currentLevelConfig.globalSpeedMultiplier * config.speedModifier;

                if (speedField != null && speedField.FieldType == typeof(float))
                {
                    speedField.SetValue(movementComp, finalSpeed);
                }
                else if (speedProp != null && speedProp.PropertyType == typeof(float) && speedProp.CanWrite)
                {
                    speedProp.SetValue(movementComp, finalSpeed);
                }
                else
                {
                     Debug.LogWarning($"[TrainingLevelManager] Movement script '{config.movementScriptName}' attached, but could not find a 'speed' or 'Speed' float field to apply the modifier.");
                }
            }
            else
            {
                Debug.LogError($"[TrainingLevelManager] Could not find MonoBehaviour movement script '{config.movementScriptName}' in any loaded assembly!");
            }
        }

        activeTargets.Add(newTarget);
    }

    /// <summary>
    /// Helps find a Type by name across all loaded assemblies. Useful for projects using .asmdef files.
    /// </summary>
    private Type FindTypeInAllAssemblies(string typeName)
    {
        // First try standard GetType (fastest)
        Type type = Type.GetType(typeName);
        if (type != null) return type;

        // If not found, iterate all assemblies
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            type = assembly.GetType(typeName);
            if (type != null)
            {
                return type;
            }
        }

        return null; // Not found anywhere
    }

    private void CompleteCurrentWave()
    {
        isWaveActive = false;

        // Stop any pending target spawns to prevent coroutine leaks into the next wave
        foreach (var routine in activeSpawnCoroutines)
        {
            if (routine != null)
            {
                StopCoroutine(routine);
            }
        }
        activeSpawnCoroutines.Clear();

        // Clean up remaining targets automatically
        foreach (var target in activeTargets)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
        activeTargets.Clear();

        Debug.Log($"[TrainingLevelManager] Wave {currentWaveIndex + 1} Completed.");
        OnWaveCompleted?.Invoke(currentWaveIndex);

        currentWaveIndex++;

        // Short delay before next wave
        Invoke(nameof(StartNextWaveDelayed), 2.0f);
    }

    private void StartNextWaveDelayed()
    {
        StartWave(currentWaveIndex);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the progression of levels and waves for the archery training range.
/// Reads data from a LevelConfigSO and spawns targets based on WaveDataSO configurations.
/// </summary>
public class TrainingLevelManager : MonoBehaviour
{
    [Header("Campaign Configuration")]
    [Tooltip("The ordered list of levels to play through. The manager will seamlessly progress from one level to the next.")]
    public List<LevelConfigSO> levelPlaylist = new List<LevelConfigSO>();

    private int currentLevelIndex = 0;
    private LevelConfigSO currentLevelConfig;

    [Header("Spawn Settings")]
    [Tooltip("The central point where targets are spawned around. If null, uses the manager's position.")]
    public Transform spawnCenter;

    [Header("UI Feedback")]
    [Tooltip("Primary text element (Announcer) to display Level Intro/Outro messages.")]
    public TMP_Text levelFeedbackText;

    [Tooltip("Secondary text element (HUD) to display real-time wave progress (e.g. Targets Left).")]
    public TMP_Text hudText;

    private int currentWaveIndex = 0;
    private int pendingSpawns = 0;
    private List<GameObject> activeTargets = new List<GameObject>();
    private bool isWaveActive = false;
    private float waveTimer = 0f;

    // Performance Tracking
    private float currentLevelStartTime = 0f;

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

        if (levelPlaylist.Count > 0)
        {
            StartLevel(currentLevelIndex);
        }
        else
        {
            Debug.LogWarning("[TrainingLevelManager] No LevelConfigs assigned to the playlist!");
            if (levelFeedbackText != null) levelFeedbackText.text = "Error: No levels assigned.";
        }
    }

    private void Update()
    {
        if (!isWaveActive)
        {
            if (hudText != null && !string.IsNullOrEmpty(hudText.text))
            {
                hudText.text = "";
            }
            return;
        }

        WaveDataSO currentWave = currentLevelConfig.waves[currentWaveIndex];

        // Clean up list of destroyed targets so we always have an accurate count
        activeTargets.RemoveAll(t => t == null);

        // Check if player cleared the wave early (applies to BOTH modes)
        bool allTargetsCleared = activeTargets.Count == 0 && pendingSpawns <= 0;

        if (currentWave.progressionType == WaveProgressionType.TimeBased)
        {
            waveTimer += Time.deltaTime;

            if (hudText != null)
            {
                float timeLeft = Mathf.Max(0, currentWave.waveDuration - waveTimer);
                hudText.text = $"Wave {currentWaveIndex + 1}/{currentLevelConfig.waves.Count} | Survive: {timeLeft:F1}s";
            }

            // TimeBased waves end if time runs out, OR if the player clears everything early
            if (waveTimer >= currentWave.waveDuration || allTargetsCleared)
            {
                CompleteCurrentWave();
            }
        }
        else if (currentWave.progressionType == WaveProgressionType.ClearAllTargets)
        {
            if (hudText != null)
            {
                int targetsRemaining = activeTargets.Count + pendingSpawns;
                hudText.text = $"Wave {currentWaveIndex + 1}/{currentLevelConfig.waves.Count} | Targets Left: {targetsRemaining}";
            }

            // If all targets are cleared and no more are waiting to spawn, wave is complete
            if (allTargetsCleared)
            {
                CompleteCurrentWave();
            }
        }
    }

    /// <summary>
    /// Starts a new level from the playlist based on the index.
    /// </summary>
    public void StartLevel(int levelIndex)
    {
        if (levelIndex >= levelPlaylist.Count)
        {
            Debug.Log("[TrainingLevelManager] Entire Campaign Completed!");
            if (levelFeedbackText != null) levelFeedbackText.text = "Campaign Complete!\nThanks for playing!";
            return;
        }

        currentLevelIndex = levelIndex;
        currentLevelConfig = levelPlaylist[currentLevelIndex];
        currentWaveIndex = 0;
        currentLevelStartTime = Time.time;

        Debug.Log($"[TrainingLevelManager] Starting Level: {currentLevelConfig.levelName}");

        if (levelFeedbackText != null)
        {
            levelFeedbackText.text = $"Level: {currentLevelConfig.levelName}\n{currentLevelConfig.levelIntroText}\n<size=70%>Shoot the Gong to begin!</size>";
            Debug.Log($"[TrainingLevelManager] Showing Level Intro Text. Waiting infinitely for player to shoot the Gong.");
        }

        if (currentLevelConfig.waves.Count == 0)
        {
            Debug.LogWarning($"[TrainingLevelManager] Level '{currentLevelConfig.levelName}' has no waves configured.");
            if (levelFeedbackText != null) levelFeedbackText.text = "Error: Level empty. Skipping...";
            CompleteCurrentLevel();
        }
    }

    private void StartWave(int index)
    {
        if (index >= currentLevelConfig.waves.Count)
        {
            CompleteCurrentLevel();
            return;
        }

        WaveDataSO waveData = currentLevelConfig.waves[index];
        Debug.Log($"[TrainingLevelManager] Starting Wave {index + 1}/{currentLevelConfig.waves.Count}. Progression: {waveData.progressionType}");

        // Waves no longer interrupt the action with text or delays!
        if (levelFeedbackText != null)
        {
            levelFeedbackText.text = "";
        }

        activeTargets.Clear();
        activeSpawnCoroutines.Clear();
        pendingSpawns = waveData.targets.Count;
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

        pendingSpawns--;
    }

    private void SpawnTarget(TargetSpawnConfig config)
    {
        if (currentLevelConfig.vanillaTargetPrefab == null)
        {
            Debug.LogError("[TrainingLevelManager] Vanilla Target Prefab is not set in LevelConfigSO!");
            return;
        }

        Vector3 spawnPos = spawnCenter.position + config.spawnPosition;

        // VR Constraint: Ensure target never spawns below the ground (Y < 0 relative to center)
        if (spawnPos.y < spawnCenter.position.y)
        {
            spawnPos.y = spawnCenter.position.y;
        }

        // VR Constraint: Ensure target is perfectly upright, facing the spawn center (the player's expected position)
        Vector3 directionToCenter = spawnCenter.position - spawnPos;
        directionToCenter.y = 0; // Keep rotation strictly horizontal (no tilting up/down)

        Quaternion spawnRot = Quaternion.identity;
        if (directionToCenter.sqrMagnitude > 0.001f)
        {
            spawnRot = Quaternion.LookRotation(directionToCenter);
        }

        GameObject newTarget = Instantiate(currentLevelConfig.vanillaTargetPrefab, spawnPos, spawnRot);

        // 1. Apply global and local scaling
        float finalScale = currentLevelConfig.globalScaleMultiplier * config.scaleModifier;
        if (finalScale <= 0) finalScale = 1.0f; // Prevent scale 0
        newTarget.transform.localScale = Vector3.one * finalScale;

        // 2. Apply setup to all MovingTarget components, including those nested in Spline Prefabs.
        MovingTarget[] movingTargets = newTarget.GetComponentsInChildren<MovingTarget>(false);
        if (movingTargets.Length > 0)
        {
            foreach (var target in movingTargets)
            {
                target.SetRequiredElement(config.requiredArrowElement);
                // The manager tracks actual destructible targets instead of the root prefab
                // so that it accurately knows when all targets in a spline group are destroyed.
                activeTargets.Add(target.gameObject);
            }
            Debug.Log($"[TrainingLevelManager] Spawned {movingTargets.Length} targets on prefab. Need Element: {config.requiredArrowElement}");
        }
        else
        {
            Debug.LogWarning("[TrainingLevelManager] Spawned prefab has no MovingTarget components! Tracking root object instead.");
            activeTargets.Add(newTarget);
        }

        // 3. Clean up the root object if all of its children are destroyed (crucial for Spline targets)
        // We add an invisible cleanup script to the root object.
        var cleaner = newTarget.AddComponent<RootObjectCleaner>();
        cleaner.Initialize(movingTargets);

        // 4. Dynamically add movement script (Searching across all assemblies for .asmdef support)
        if (config.movementBehavior != TargetMovementType.None)
        {
            string movementScriptName = config.movementBehavior.ToString();
            Type movementType = FindTypeInAllAssemblies(movementScriptName);

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
                     Debug.LogWarning($"[TrainingLevelManager] Movement script '{movementScriptName}' attached, but could not find a 'speed' or 'Speed' float field to apply the modifier.");
                }
            }
            else
            {
                Debug.LogError($"[TrainingLevelManager] Could not find MonoBehaviour movement script '{movementScriptName}' in any loaded assembly!");
            }
        }
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
        pendingSpawns = 0;

        // Clean up remaining targets automatically
        foreach (var target in activeTargets)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
        activeTargets.Clear();

        Debug.Log($"[TrainingLevelManager] Wave {currentWaveIndex + 1} Completed. Instantly starting next wave.");

        OnWaveCompleted?.Invoke(currentWaveIndex);

        currentWaveIndex++;

        // Instant, silent momentum. No text, no waiting.
        StartWave(currentWaveIndex);
    }

    /// <summary>
    /// Call this method (e.g. from the LevelAdvanceGong) to act as the "Ready" trigger.
    /// </summary>
    public void AdvanceToNextLevel()
    {
        if (isWaveActive) return;

        if (currentWaveIndex == 0 && currentLevelConfig != null && currentLevelConfig.waves.Count > 0)
        {
            // If we are at the start of a level waiting to begin, shooting the gong starts wave 1
            if (levelFeedbackText != null) levelFeedbackText.text = "";
            StartWave(currentWaveIndex);
        }
        else
        {
            // If we finished a level and shot the gong, go to the next level's Intro
            currentLevelIndex++;
            StartLevel(currentLevelIndex);
        }
    }

    private void CompleteCurrentLevel()
    {
        isWaveActive = false; // Stop the HUD from updating
        if (hudText != null) hudText.text = ""; // Clear the HUD cleanly

        float levelTimeTaken = Time.time - currentLevelStartTime;
        Debug.Log($"[TrainingLevelManager] Level '{currentLevelConfig.levelName}' completed in {levelTimeTaken:F1}s! Waiting for player to hit the Gong to advance.");

        if (levelFeedbackText != null)
        {
            levelFeedbackText.text = $"{currentLevelConfig.levelOutroText}\n<size=70%>Level Time: {levelTimeTaken:F1}s\nShoot the Gong to continue!</size>";
            Debug.Log($"[TrainingLevelManager] Showing Level Outro Text. Waiting for Gong trigger.");
        }

        OnLevelCompleted?.Invoke();

        // The manager now waits infinitely. The player must shoot the LevelAdvanceGong to trigger AdvanceToNextLevel()
    }

    private void ClearFeedbackText()
    {
        if (levelFeedbackText != null)
        {
            levelFeedbackText.text = "";
        }
    }
}

/// <summary>
/// A helper script attached to root prefabs (like Spline targets).
/// It destroys the root object when all of its child targets are destroyed.
/// </summary>
public class RootObjectCleaner : MonoBehaviour
{
    private MovingTarget[] childrenTargets;
    private bool isInitialized = false;

    public void Initialize(MovingTarget[] targets)
    {
        childrenTargets = targets;
        isInitialized = targets.Length > 0;
    }

    private void Update()
    {
        if (!isInitialized) return;

        bool allDead = true;
        foreach (var target in childrenTargets)
        {
            if (target != null)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            Destroy(gameObject);
        }
    }
}

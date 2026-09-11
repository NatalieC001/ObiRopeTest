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
    [Tooltip("Text element to display wave completion and level progress to the player.")]
    public TMP_Text waveFeedbackText;

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
            if (waveFeedbackText != null) waveFeedbackText.text = "Error: No levels assigned.";
        }
    }

    private void Update()
    {
        if (!isWaveActive) return;

        WaveDataSO currentWave = currentLevelConfig.waves[currentWaveIndex];

        // Clean up list of destroyed targets so we always have an accurate count
        activeTargets.RemoveAll(t => t == null);

        // Check if player cleared the wave early (applies to BOTH modes)
        bool allTargetsCleared = activeTargets.Count == 0 && pendingSpawns <= 0;

        if (currentWave.progressionType == WaveProgressionType.TimeBased)
        {
            waveTimer += Time.deltaTime;

            // TimeBased waves end if time runs out, OR if the player clears everything early
            if (waveTimer >= currentWave.waveDuration || allTargetsCleared)
            {
                CompleteCurrentWave();
            }
        }
        else if (currentWave.progressionType == WaveProgressionType.ClearAllTargets)
        {
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
            if (waveFeedbackText != null) waveFeedbackText.text = "Campaign Complete!\nThanks for playing!";
            return;
        }

        currentLevelIndex = levelIndex;
        currentLevelConfig = levelPlaylist[currentLevelIndex];
        currentWaveIndex = 0;
        currentLevelStartTime = Time.time;

        Debug.Log($"[TrainingLevelManager] Starting Level: {currentLevelConfig.levelName}");

        if (waveFeedbackText != null)
        {
            waveFeedbackText.text = $"Level: {currentLevelConfig.levelName}\n{currentLevelConfig.levelIntroText}";
            Debug.Log($"[TrainingLevelManager] Showing Level Intro Text. Waiting 3.0s before Wave 1.");
        }

        if (currentLevelConfig.waves.Count > 0)
        {
            // Hardcoded fast progression: wait exactly 3 seconds to read level intro
            Invoke(nameof(StartFirstWaveDelayed), 3.0f);
        }
        else
        {
            Debug.LogWarning($"[TrainingLevelManager] Level '{currentLevelConfig.levelName}' has no waves configured.");
            if (waveFeedbackText != null) waveFeedbackText.text = "Error: Level empty. Skipping...";
            CompleteCurrentLevel();
        }
    }

    private void StartFirstWaveDelayed()
    {
        StartWave(currentWaveIndex);
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
        if (waveFeedbackText != null)
        {
            waveFeedbackText.text = "";
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

        // 2. Try to setup the MovingTarget component (Color requirement)
        MovingTarget movingTarget = newTarget.GetComponent<MovingTarget>();
        if (movingTarget != null)
        {
            movingTarget.SetRequiredElement(config.requiredArrowElement);
            Debug.Log($"[TrainingLevelManager] Target spawned. Needs Element: {config.requiredArrowElement}");
        }

        // 3. Dynamically add movement script (Searching across all assemblies for .asmdef support)
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

    private void CompleteCurrentLevel()
    {
        float levelTimeTaken = Time.time - currentLevelStartTime;
        Debug.Log($"[TrainingLevelManager] Level '{currentLevelConfig.levelName}' completed in {levelTimeTaken:F1}s!");

        if (waveFeedbackText != null)
        {
            waveFeedbackText.text = $"{currentLevelConfig.levelOutroText}\n<size=70%>Level Time: {levelTimeTaken:F1}s</size>";
            Debug.Log($"[TrainingLevelManager] Showing Level Outro Text. Waiting 3.0s before loading the next Level.");
        }

        OnLevelCompleted?.Invoke();

        // Hardcoded fast progression: wait exactly 3 seconds for the player to read the level completion text
        Invoke(nameof(StartNextLevelDelayed), 3.0f);
    }

    private void StartNextLevelDelayed()
    {
        currentLevelIndex++;
        StartLevel(currentLevelIndex);
    }

    private void ClearFeedbackText()
    {
        if (waveFeedbackText != null)
        {
            waveFeedbackText.text = "";
        }
    }
}

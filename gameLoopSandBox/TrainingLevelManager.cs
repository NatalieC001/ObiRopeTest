using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;

/// <summary>
/// Manages the progression of levels and waves for the archery training range.
/// Reads data from a LevelConfigSO and spawns targets based on WaveData configurations.
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

        WaveData currentWave = currentLevelConfig.waves[currentWaveIndex];

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
            // Ensure the text is visible if it was previously faded out
            Color color = levelFeedbackText.color;
            color.a = 1f;
            levelFeedbackText.color = color;

            levelFeedbackText.text = $"Level: {currentLevelConfig.levelName}\n{currentLevelConfig.levelIntroText}\n<size=70%>Shoot the Gong to begin!</size>";
            Debug.Log($"[TrainingLevelManager] Showing Level Intro Text. Waiting 3.0s before Wave 1.");
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

        WaveData waveData = currentLevelConfig.waves[index];
        Debug.Log($"[TrainingLevelManager] Starting Wave {index + 1}/{currentLevelConfig.waves.Count}. Progression: {waveData.progressionType}");

        // Waves no longer interrupt the action with text or delays!
        if (levelFeedbackText != null)
        {
            levelFeedbackText.text = "";
        }

        activeTargets.Clear();
        activeSpawnCoroutines.Clear();
        pendingSpawns = waveData.characters.Count;
        waveTimer = 0f;
        isWaveActive = true;

        OnWaveStarted?.Invoke(index);

        // Start spawning targets - each target gets its own coroutine to fix cumulative delay issues
        foreach (var charConfig in waveData.characters)
        {
            Coroutine spawnRoutine = StartCoroutine(SpawnCharacterWithDelay(charConfig));
            activeSpawnCoroutines.Add(spawnRoutine);
        }
    }

    private IEnumerator SpawnCharacterWithDelay(CharacterConfigBase charConfig)
    {
        if (charConfig.spawnDelay > 0)
        {
            yield return new WaitForSeconds(charConfig.spawnDelay);
        }

        SpawnCharacter(charConfig);

        pendingSpawns--;
    }

    private void SpawnCharacter(CharacterConfigBase charConfig)
    {
        if (charConfig is MinionConfig minionConfig)
        {
            SpawnMinion(minionConfig);
        }
        else if (charConfig is BossConfig bossConfig)
        {
            SpawnBoss(bossConfig);
        }
    }

    private void SpawnMinion(MinionConfig config)
    {
        if (config.prefab == null)
        {
            Debug.LogError("[TrainingLevelManager] Minion Prefab is not set in LevelConfigSO!");
            return;
        }

        Vector3 spawnPos = spawnCenter.position + config.spawnPositionOffset;
        Quaternion spawnRot = Quaternion.identity;

        if (config.spawnPointPrefab != null)
        {
            GameObject spawnPoint = Instantiate(config.spawnPointPrefab, spawnPos, Quaternion.identity);
            spawnPos = spawnPoint.transform.position;
            spawnRot = spawnPoint.transform.rotation;
        }

        GameObject newTarget = Instantiate(config.prefab, spawnPos, spawnRot);

        // Try to setup the MovingTarget component (Color requirement)
        MovingTarget movingTarget = newTarget.GetComponent<MovingTarget>();
        if (movingTarget != null)
        {
            movingTarget.SetRequiredElement(config.requiredArrowElement);
            Debug.Log($"[TrainingLevelManager] Target spawned. Needs Element: {config.requiredArrowElement}");
        }

        // Spawn the movement asset if needed
        if (config.movementAssetPrefab != null && config.movementType == TargetMovementType.SplinePathAsset)
        {
            Instantiate(config.movementAssetPrefab, spawnPos, Quaternion.identity);
            // Additional logic to wire spline path to the target could go here
        }

        // Dynamically add movement script (Searching across all assemblies for .asmdef support)
        if (config.movementType != TargetMovementType.None && config.movementType != TargetMovementType.SplinePathAsset && config.movementType != TargetMovementType.BossDragonAsset)
        {
            string movementScriptName = config.movementType.ToString();
            Type movementType = FindTypeInAllAssemblies(movementScriptName);

            if (movementType != null && typeof(MonoBehaviour).IsAssignableFrom(movementType))
            {
                if (newTarget.GetComponent(movementType) == null)
                {
                    newTarget.AddComponent(movementType);
                }
            }
            else
            {
                Debug.LogError($"[TrainingLevelManager] Could not find MonoBehaviour movement script '{movementScriptName}' in any loaded assembly!");
            }
        }

        activeTargets.Add(newTarget);
    }

    private void SpawnBoss(BossConfig config)
    {
        if (config.prefab == null)
        {
            Debug.LogError("[TrainingLevelManager] Boss Prefab is not set in LevelConfigSO!");
            return;
        }

        Vector3 spawnPos = spawnCenter.position + config.spawnPositionOffset;
        Quaternion spawnRot = Quaternion.identity;

        if (config.spawnPointPrefab != null)
        {
            GameObject spawnPoint = Instantiate(config.spawnPointPrefab, spawnPos, Quaternion.identity);
            spawnPos = spawnPoint.transform.position;
            spawnRot = spawnPoint.transform.rotation;
        }

        GameObject bossObj = Instantiate(config.prefab, spawnPos, spawnRot);
        activeTargets.Add(bossObj);

        if (config.observationPathPrefabs != null)
        {
            foreach (var obsPath in config.observationPathPrefabs)
            {
                if (obsPath != null)
                {
                    Instantiate(obsPath, spawnCenter.position + config.spawnPositionOffset, Quaternion.identity);
                }
            }
        }

        if (config.escapePathPrefabs != null)
        {
            foreach (var escPath in config.escapePathPrefabs)
            {
                if (escPath != null)
                {
                    Instantiate(escPath, spawnCenter.position + config.spawnPositionOffset, Quaternion.identity);
                }
            }
        }

        // If this is the Boss Dragon encounter, we need to wire it up!
        BossArenaManager arenaManager = FindAnyObjectByType<BossArenaManager>();
        if (arenaManager != null)
        {
            BossCreature boss = bossObj.GetComponent<BossCreature>();

            // For this prototype logic, we just find all StandardCreatures spawned so far this wave
            // We use FindObjectsByType because StandardCreature inherits from MovingTarget.

            // Note: StandardCreature script needs to be attached to spawned minions for them to be found here.

            List<StandardCreature> minions = new List<StandardCreature>(FindObjectsByType<StandardCreature>(FindObjectsSortMode.None));

            if (boss != null)
            {
                arenaManager.RegisterBattleParticipants(boss, minions);
                Debug.Log("[TrainingLevelManager] Successfully registered Boss Dragon with the BossArenaManager!");
            }
        }
        else
        {
            Debug.LogError("[TrainingLevelManager] Boss spawned, but no BossArenaManager found in the scene to wire it to!");
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
            // Ensure the text is visible if it was previously faded out
            Color color = levelFeedbackText.color;
            color.a = 1f;
            levelFeedbackText.color = color;

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

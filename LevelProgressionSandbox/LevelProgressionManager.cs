using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the high-level progression of the game loop using an event-driven state machine.
/// Handles the core flow: Gong -> Countdown -> Waves -> Boss -> Victory -> Next Level.
/// Completely avoids Coroutines in favor of simple Update timers.
/// </summary>
public class LevelProgressionManager : MonoBehaviour
{
    public enum GameState
    {
        Initialize,
        WaitingForGong,
        Countdown,
        WaveActive,
        BossActive,
        LevelVictory,
        CampaignComplete
    }

    [Header("Campaign Configuration")]
    [Tooltip("The ordered list of levels to play through.")]
    public List<LevelConfigSO> levelPlaylist = new List<LevelConfigSO>();

    [Header("Settings")]
    public float countdownDuration = 3.0f;
    public float victoryDelay = 4.0f;

    // --- State & Tracking ---
    private GameState currentState = GameState.Initialize;
    private int currentLevelIndex = 0;
    private int currentWaveIndex = 0;
    private float timer = 0f;

    // --- Public Events ---
    // Fired when the state machine transitions
    public event Action<GameState> OnStateChanged;

    // Fired to UI
    public event Action<LevelConfigSO> OnLevelIntroReady;
    public event Action<string> OnWaveTransitionAlert;
    public event Action<float> OnCountdownUpdated;
    public event Action<string> OnLevelOutroReady;
    public event Action OnCampaignComplete;

    // Fired to Spawner
    public event Action<LevelConfigSO, int> OnWaveStartRequested;

    private void Start()
    {
        if (levelPlaylist.Count == 0)
        {
            Debug.LogError("[LevelProgressionManager] No levels in playlist!");
            ChangeState(GameState.CampaignComplete);
            return;
        }

        PrepareLevel(0);
    }

    private void Update()
    {
        switch (currentState)
        {
            case GameState.Countdown:
                HandleCountdownState();
                break;
            case GameState.LevelVictory:
                HandleVictoryState();
                break;
        }
    }

    /// <summary>
    /// Changes the internal state and broadcasts the change to all listeners.
    /// </summary>
    private void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"[LevelProgressionManager] State changed to: {newState}");
        OnStateChanged?.Invoke(currentState);
    }

    /// <summary>
    /// Sets up a level and waits for the player to hit the gong.
    /// </summary>
    private void PrepareLevel(int levelIndex)
    {
        if (levelIndex >= levelPlaylist.Count)
        {
            OnCampaignComplete?.Invoke();
            ChangeState(GameState.CampaignComplete);
            return;
        }

        currentLevelIndex = levelIndex;
        currentWaveIndex = 0;

        LevelConfigSO currentLevel = levelPlaylist[currentLevelIndex];
        OnLevelIntroReady?.Invoke(currentLevel);

        ChangeState(GameState.WaitingForGong);
    }

    /// <summary>
    /// Called by external systems (e.g., LevelAdvanceGong) when the gong is struck.
    /// </summary>
    public void ReceiveGongHit()
    {
        if (currentState == GameState.WaitingForGong)
        {
            Debug.Log("[LevelProgressionManager] Gong hit received. Starting countdown.");
            timer = countdownDuration;
            ChangeState(GameState.Countdown);
        }
    }

    private void HandleCountdownState()
    {
        timer -= Time.deltaTime;

        // Notify UI of time remaining
        OnCountdownUpdated?.Invoke(Mathf.Max(0, timer));

        if (timer <= 0)
        {
            StartNextWave();
        }
    }

    private void StartNextWave()
    {
        LevelConfigSO currentLevel = levelPlaylist[currentLevelIndex];

        // Check if we have waves left
        if (currentWaveIndex < currentLevel.waves.Count)
        {
            ChangeState(GameState.WaveActive);
            OnWaveStartRequested?.Invoke(currentLevel, currentWaveIndex);
        }
        else
        {
            // No waves left -> Level Complete
            TriggerLevelVictory();
        }
    }

    /// <summary>
    /// Spawner can call this if it detects a Boss Config in the wave data, allowing the Manager to update UI/State.
    /// </summary>
    public void NotifyBossWaveStarted()
    {
        if (currentState == GameState.WaveActive)
        {
            ChangeState(GameState.BossActive);
        }
    }

    /// <summary>
    /// Called by external systems (e.g., WaveSpawner) when all targets for a wave are destroyed or time is up.
    /// </summary>
    public void ReceiveWaveCompleted()
    {
        if (currentState == GameState.WaveActive)
        {
            currentWaveIndex++;
            Debug.Log($"[LevelProgressionManager] Wave completed. Starting next sequence.");

            LevelConfigSO currentLevel = levelPlaylist[currentLevelIndex];

            // Check what the next wave will be to display the correct text
            if (currentWaveIndex < currentLevel.waves.Count)
            {
                bool isBossNext = false;
                foreach (var character in currentLevel.waves[currentWaveIndex].characters)
                {
                    if (character is BossConfig)
                    {
                        isBossNext = true;
                        break;
                    }
                }

                if (isBossNext)
                {
                    OnWaveTransitionAlert?.Invoke("BOSS INCOMING!");
                }
                else
                {
                    OnWaveTransitionAlert?.Invoke($"Wave {currentWaveIndex} Cleared!");
                }
            }

            // We borrow the countdown timer to add a short delay between waves so the player can read the alert
            timer = 2.5f;
            ChangeState(GameState.Countdown);
        }
    }

    /// <summary>
    /// Called by external systems (e.g., BossArenaManager) when the boss is defeated.
    /// </summary>
    public void ReceiveBossDefeated()
    {
        if (currentState == GameState.BossActive)
        {
            TriggerLevelVictory();
        }
    }

    private void TriggerLevelVictory()
    {
        LevelConfigSO currentLevel = levelPlaylist[currentLevelIndex];
        OnLevelOutroReady?.Invoke(currentLevel.levelOutroText);

        timer = victoryDelay;
        ChangeState(GameState.LevelVictory);
    }

    private void HandleVictoryState()
    {
        timer -= Time.deltaTime;

        if (timer <= 0)
        {
            // Automatically progress to the next level's intro.
            // The player will then need to hit the gong ONCE to start the next level's countdown.
            PrepareLevel(currentLevelIndex + 1);
        }
    }
}

using UnityEngine;
using TMPro;

/// <summary>
/// Subscribes to LevelProgressionManager and WaveSpawner events to cleanly update UI.
/// Separating this ensures UI logic does not entangle with gameplay state or wave timing.
/// </summary>
public class LevelUIManager : MonoBehaviour
{
    [Header("Managers")]
    [Tooltip("Reference to the Progression Manager orchestrating the state.")]
    public LevelProgressionManager progressionManager;

    [Tooltip("Optional reference to the WaveSpawner to track real-time target counts (if applicable).")]
    public WaveSpawner waveSpawner; // We'll assume the spawner might send updates like targets remaining

    [Header("UI Elements")]
    [Tooltip("Primary text element (Announcer) for Intros, Outros, and Countdowns.")]
    public TMP_Text levelFeedbackText;

    [Tooltip("Secondary text element (HUD) for real-time wave info (e.g. 'Targets Left: 4').")]
    public TMP_Text hudText;

    private void OnEnable()
    {
        if (progressionManager == null)
        {
            progressionManager = FindFirstObjectByType<LevelProgressionManager>();
        }

        if (progressionManager != null)
        {
            progressionManager.OnStateChanged += HandleStateChanged;
            progressionManager.OnLevelIntroReady += DisplayLevelIntro;
            progressionManager.OnLevelOutroReady += DisplayLevelOutro;
            progressionManager.OnCountdownUpdated += UpdateCountdownText;
            progressionManager.OnCampaignComplete += DisplayCampaignComplete;
        }

        if (waveSpawner == null)
        {
            waveSpawner = FindFirstObjectByType<WaveSpawner>();
        }

        if (waveSpawner != null)
        {
            waveSpawner.OnWaveProgressUpdated += UpdateHUDText;
        }
    }

    private void OnDisable()
    {
        if (progressionManager != null)
        {
            progressionManager.OnStateChanged -= HandleStateChanged;
            progressionManager.OnLevelIntroReady -= DisplayLevelIntro;
            progressionManager.OnLevelOutroReady -= DisplayLevelOutro;
            progressionManager.OnCountdownUpdated -= UpdateCountdownText;
            progressionManager.OnCampaignComplete -= DisplayCampaignComplete;
        }

        if (waveSpawner != null)
        {
            waveSpawner.OnWaveProgressUpdated -= UpdateHUDText;
        }
    }

    private void HandleStateChanged(LevelProgressionManager.GameState state)
    {
        // Clear HUD if we are not actively in a wave or boss fight
        if (state != LevelProgressionManager.GameState.WaveActive && state != LevelProgressionManager.GameState.BossActive)
        {
            if (hudText != null)
            {
                hudText.text = "";
            }
        }

        // Clear Announcer text when wave/boss starts (clean screen for gameplay)
        if (state == LevelProgressionManager.GameState.WaveActive || state == LevelProgressionManager.GameState.BossActive)
        {
            if (levelFeedbackText != null)
            {
                levelFeedbackText.text = "";
            }
        }
    }

    private void DisplayLevelIntro(LevelConfigSO levelConfig)
    {
        if (levelFeedbackText != null)
        {
            levelFeedbackText.text = $"Level: {levelConfig.levelName}\n{levelConfig.levelIntroText}\n<size=70%>Shoot the Gong to begin!</size>";
            levelFeedbackText.alpha = 1f; // Ensure it's visible based on memory directives
        }
    }

    private void DisplayLevelOutro(string outroText)
    {
        if (levelFeedbackText != null)
        {
            levelFeedbackText.text = $"{outroText}\n<size=70%>Level Cleared!</size>";
            levelFeedbackText.alpha = 1f;
        }
    }

    private void UpdateCountdownText(float timeLeft)
    {
        if (levelFeedbackText != null)
        {
            // Show rounded up integer for cleaner countdown
            int secondsLeft = Mathf.CeilToInt(timeLeft);
            if (secondsLeft > 0)
            {
                levelFeedbackText.text = $"Starting in...\n<size=150%>{secondsLeft}</size>";
            }
            else
            {
                levelFeedbackText.text = "<size=150%>GO!</size>";
            }
            levelFeedbackText.alpha = 1f;
        }
    }

    private void DisplayCampaignComplete()
    {
        if (levelFeedbackText != null)
        {
            levelFeedbackText.text = "Campaign Complete!\nThanks for playing!";
            levelFeedbackText.alpha = 1f;
        }
    }

    private void UpdateHUDText(string text)
    {
        if (hudText != null)
        {
            hudText.text = text;
            hudText.alpha = 1f;
        }
    }
}

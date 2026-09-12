using UnityEngine;

/// <summary>
/// A tactile, physical object that acts as the "Ready" and "Next Level" button for the Archery Range.
/// Attach this script to your Gong model and ensure it has a valid Collider and Rigidbody (set to Kinematic).
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class LevelAdvanceGong : MonoBehaviour, IArrowTarget
{
    [Tooltip("Reference to the main level manager to trigger the ready state.")]
    public TrainingLevelManager levelManager;

    [Tooltip("Sound to play when the gong is successfully hit.")]
    public AudioClip gongSound;

    private AudioSource audioSource;
    private bool isCoolingDown = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        // Failsafe connection if user forgets to drag it in
        if (levelManager == null)
        {
            levelManager = FindFirstObjectByType<TrainingLevelManager>();
        }
    }

    /// <summary>
    /// Called automatically by the StickingArrow when an arrow physically collides with this GameObject.
    /// </summary>
    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        // Don't trigger multiple times instantly if multiple arrows hit it at once
        if (isCoolingDown) return;

        if (levelManager != null)
        {
            Debug.Log($"<color=cyan>[LevelAdvanceGong] Gong struck! Advancing to next level...</color>");

            // Play satisfying feedback sound
            if (gongSound != null)
            {
                audioSource.PlayOneShot(gongSound);
            }

            // Trigger the level advance
            levelManager.AdvanceToNextLevel();

            // Prevent spamming
            isCoolingDown = true;
            Invoke(nameof(ResetCooldown), 3.0f);
        }
        else
        {
            Debug.LogError("[LevelAdvanceGong] Cannot advance! TrainingLevelManager is missing!");
        }
    }

    private void ResetCooldown()
    {
        isCoolingDown = false;
    }
}

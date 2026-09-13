using UnityEngine;

/// <summary>
/// The 'Brain' for an advanced Boss creature.
/// It monitors health and battle state, and commands the TacticalBossSplineManager
/// to make intelligent evasion choices (like jumping to escape splines) when threatened.
/// </summary>
[RequireComponent(typeof(TacticalBossSplineManager))]
public class BossCreature : MonoBehaviour
{
    [Header("Boss Stats")]
    public float maxHealth = 500f;
    private float currentHealth;

    [Tooltip("The boss will attempt to jump to an escape route if taking rapid damage.")]
    public float evasionDamageThreshold = 50f;

    private TacticalBossSplineManager tacticalManager;
    private float recentDamageAccumulator = 0f;
    private float damageDecayTimer = 0f;

    private void Awake()
    {
        tacticalManager = GetComponent<TacticalBossSplineManager>();
        currentHealth = maxHealth;
    }

    private void Update()
    {
        // Decay the damage accumulator over time so the boss only evades
        // burst damage, not slow, consistent pokes.
        if (recentDamageAccumulator > 0)
        {
            damageDecayTimer += Time.deltaTime;
            if (damageDecayTimer > 3f) // Reset accumulator after 3 seconds of no damage
            {
                recentDamageAccumulator = 0f;
                damageDecayTimer = 0f;
            }
        }
    }

    /// <summary>
    /// Called when the player shoots the boss.
    /// </summary>
    public void TakeDamage(float amount, Vector3 hitPoint)
    {
        currentHealth -= amount;
        Debug.Log($"<color=orange>[BossCreature] Took {amount} damage! Remaining Health: {currentHealth}</color>");

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Tactical Decision Logic: Evaluate if we need to evade
        EvaluateThreat(amount);
    }

    private void EvaluateThreat(float damageTaken)
    {
        recentDamageAccumulator += damageTaken;
        damageDecayTimer = 0f; // Reset decay

        if (recentDamageAccumulator >= evasionDamageThreshold)
        {
            Debug.Log("<color=red>[BossCreature] Threat level high! Ordering tactical evasion.</color>");

            // Command the manager to jump to an environmental spline
            tacticalManager.EvadeToEscapeRoute();

            // Reset accumulator so it doesn't immediately evade again
            recentDamageAccumulator = 0f;
        }
    }

    private void Die()
    {
        Debug.Log("<color=red>[BossCreature] The Boss has been defeated!</color>");

        // Stop movement
        tacticalManager.enabled = false;

        // Add death animations/dissolves here
        Destroy(gameObject, 1f);
    }
}

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

    public enum BossPhase
    {
        Orchestrator, // Watching minions, taking no action
        Engaged       // Actively fighting and evading
    }

    [Header("Battle State")]
    public BossPhase currentPhase = BossPhase.Orchestrator;

    private TacticalBossSplineManager tacticalManager;
    private float recentDamageAccumulator = 0f;
    private float damageDecayTimer = 0f;

    private void Awake()
    {
        tacticalManager = GetComponent<TacticalBossSplineManager>();
        currentHealth = maxHealth;
    }

    /// <summary>
    /// Called by the BossArenaManager when the boss is spawned.
    /// Injects the scene's environmental splines.
    /// </summary>
    public void InitializeArena(BossArenaManager arena)
    {
        currentPhase = BossPhase.Orchestrator;
        tacticalManager.InitializeRoutes(arena.observationSpline, arena.tacticalEscapeRoutes);
    }

    /// <summary>
    /// Called by the Arena Manager when all minions are dead.
    /// </summary>
    public void EngagePlayer()
    {
        currentPhase = BossPhase.Engaged;
        tacticalManager.StartEvasionRoutine();
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

        // If the boss is hit while orchestrating, maybe it engages early!
        if (currentPhase == BossPhase.Orchestrator)
        {
            Debug.Log("<color=red>[BossCreature] You dared to shoot the boss while it was watching? It attacks early!</color>");
            EngagePlayer();
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

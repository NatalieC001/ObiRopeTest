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
        Orchestrator, // Watching minions, charging on Crystal
        Engaged,      // Actively fighting the player
        Exhausted,    // Fleeing through environment to recharge
        Recharging    // Back on Observation spline, regaining stamina
    }

    [Header("Battle State")]
    public BossPhase currentPhase = BossPhase.Orchestrator;

    [Header("Energy System")]
    [Tooltip("How much stamina the boss has for attacking before it must retreat.")]
    public float maxStamina = 100f;
    [Tooltip("How fast stamina drains while attacking.")]
    public float staminaDrainRate = 10f;
    [Tooltip("How fast stamina recovers while on the observation spline.")]
    public float staminaRechargeRate = 15f;
    private float currentStamina;

    private TacticalBossSplineManager tacticalManager;
    private float recentDamageAccumulator = 0f;
    private float damageDecayTimer = 0f;

    private void Awake()
    {
        tacticalManager = GetComponent<TacticalBossSplineManager>();
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    /// <summary>
    /// Called by the BossArenaManager when the boss is spawned.
    /// Injects the scene's environmental splines.
    /// </summary>
    public void InitializeArena(BossArenaManager arena)
    {
        currentPhase = BossPhase.Orchestrator;
        tacticalManager.InitializeRoutes(arena.observationSpline, arena.tacticalEscapeRoutes);

        // Also ensure the visual dragon body is spawned and attached to the starting track
        SegmentedDragonManager dragonBody = GetComponent<SegmentedDragonManager>();
        if (dragonBody != null)
        {
            dragonBody.InitializeDragon(arena.observationSpline);
        }
    }

    /// <summary>
    /// Called by the Arena Manager when minions drop below threshold.
    /// </summary>
    public void EngagePlayer()
    {
        currentPhase = BossPhase.Engaged;
        // In a full implementation, this would point the dragon directly at the watchtower.
        // For now, it enters the combat state.
        Debug.Log("<color=magenta>[BossCreature] Phase 2: Dragon attacking player!</color>");
    }

    private void Update()
    {
        // Handle Phase 2 Stamina Drain
        if (currentPhase == BossPhase.Engaged)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                EnterExhaustedPhase();
            }
        }
        else if (currentPhase == BossPhase.Recharging)
        {
            // Regain stamina. Note: Health never heals to prevent infinite fights!
            currentStamina += staminaRechargeRate * Time.deltaTime;
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                Debug.Log("<color=green>[BossCreature] Stamina full! Diving back in to attack!</color>");
                EngagePlayer();
            }
        }

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

    private void EnterExhaustedPhase()
    {
        currentPhase = BossPhase.Exhausted;
        Debug.Log("<color=cyan>[BossCreature] Phase 3: Dragon is exhausted! Fleeing to recharge!</color>");

        // Command the manager to flee through environmental splines
        tacticalManager.StartEvasionRoutine();
    }

    /// <summary>
    /// Called by the Tactical Manager when the boss finishes an escape route
    /// and returns to the observation deck.
    /// </summary>
    public void BeginRecharging()
    {
        currentPhase = BossPhase.Recharging;
        Debug.Log("<color=cyan>[BossCreature] Phase 4: Recharging stamina on the observation deck!</color>");
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

        // Tactical Decision Logic: Evaluate if we need to evade (only if engaged or exhausted)
        if (currentPhase != BossPhase.Orchestrator)
        {
            EvaluateThreat(amount);
        }
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

        // Command all indestructible pieces (Head, Legs, Tail) to dissolve
        SegmentedDragonManager dragonBody = GetComponent<SegmentedDragonManager>();
        if (dragonBody != null)
        {
            dragonBody.TriggerTotalDeath();
        }

        // Clean up the main boss entity
        Destroy(gameObject, 1f);
    }
}

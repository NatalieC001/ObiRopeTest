using UnityEngine;

/// <summary>
/// The 'Brain' for an advanced Boss creature.
/// It monitors health and battle state, and commands the TacticalBossSplineManager
/// to make intelligent evasion choices (like jumping to escape splines) when threatened.
/// </summary>
[RequireComponent(typeof(TacticalBossSplineManager))]
public class BossCreature : MonoBehaviour
{
    [System.Serializable]
    public struct ElementalModifier
    {
        public ElementTypeOB7 arrowType;
        [Tooltip("1.0 = normal damage. 2.0 = double damage (weakness). 0.0 = immune. -1.0 = heals the boss!")]
        public float damageMultiplier;
    }

    [Header("Boss Stats")]
    public float maxHealth = 500f;
    private float currentHealth;

    [Header("Elemental Resistances")]
    [Tooltip("If an arrow type isn't listed here, it deals standard 1.0x damage.")]
    public ElementalModifier[] elementalModifiers;

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
    private CreatureStatusEffects statusEffects;
    private float recentDamageAccumulator = 0f;
    private float damageDecayTimer = 0f;

    // Used to track if the TrainingLevelManager properly initialized us, or if we were manually dragged into the scene for testing.
    private bool isInitialized = false;

    private void Awake()
    {
        tacticalManager = GetComponent<TacticalBossSplineManager>();
        statusEffects = GetComponent<CreatureStatusEffects>();
        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    private void Start()
    {
        // If we were manually dragged into the scene for testing, we won't be initialized by the Level Manager.
        // Let's self-register with the Arena Manager so we can generate our body and test!
        if (!isInitialized)
        {
            BossArenaManager arena = FindAnyObjectByType<BossArenaManager>();
            if (arena != null)
            {
                Debug.Log("<color=yellow>[BossCreature] Self-registering for Editor Testing mode!</color>");
                arena.RegisterStrayBoss(this);
            }
            else
            {
                Debug.LogError("[BossCreature] Dragged into scene for testing, but no BossArenaManager found to provide tracks!");

                // Fallback: If there's no Arena Manager, at least try to spawn the body parts using the component's own transform as a dummy track.
                SegmentedDragonManager dragonBody = GetComponent<SegmentedDragonManager>();
                if (dragonBody != null)
                {
                    Debug.LogWarning("[BossCreature] Attempting to initialize Dragon Body without a track just to show anatomy...");
                    dragonBody.InitializeDragon(null);
                }
            }
        }
    }

    /// <summary>
    /// Called by the BossArenaManager when the boss is spawned.
    /// Injects the scene's environmental splines.
    /// </summary>
    public void InitializeArena(BossArenaManager arena)
    {
        isInitialized = true;
        currentPhase = BossPhase.Orchestrator;

        if (arena.observationSpline == null)
        {
            Debug.LogError("[BossCreature] BossArenaManager is missing an Observation Spline! The dragon has no track to spawn on!");
        }

        tacticalManager.InitializeRoutes(arena.observationSpline, arena.tacticalEscapeRoutes);

        // Also ensure the visual dragon body is spawned and attached to the starting track
        SegmentedDragonManager dragonBody = GetComponent<SegmentedDragonManager>();
        if (dragonBody != null)
        {
            Debug.Log("[BossCreature] Instructing SegmentedDragonManager to spawn anatomy...");
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

        // Unhook from the spline so the dragon can freestyle toward the player
        if (tacticalManager != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                tacticalManager.StartFreestylePursuit(playerObj.transform);
            }
        }
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
            else if (tacticalManager != null)
            {
                // Let the tactical manager handle the freestyle movement
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    tacticalManager.UpdateFreestylePursuit(playerObj.transform);
                }
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
    public void TakeDamage(float baseAmount, Vector3 hitPoint, ElementTypeOB7 arrowType = ElementTypeOB7.Normal)
    {
        // 0. Trigger Status Effects (Slows, Freezes)
        if (statusEffects != null)
        {
            statusEffects.ApplyElementalEffect(arrowType);
        }

        // 1. Calculate actual damage based on elemental weaknesses/resistances
        float actualDamage = baseAmount;
        if (elementalModifiers != null)
        {
            foreach (var mod in elementalModifiers)
            {
                if (mod.arrowType == arrowType)
                {
                    actualDamage *= mod.damageMultiplier;
                    break;
                }
            }
        }

        // 1.5 Apply Brittle modifier if frozen by previous ice arrows
        if (statusEffects != null && statusEffects.IsBrittle && actualDamage > 0)
        {
            Debug.Log($"<color=cyan>[BossCreature] Boss Brittle shattered! Damage doubled.</color>");
            actualDamage *= 2.0f;
        }

        // 2. Apply damage or healing
        if (actualDamage < 0)
        {
            currentHealth -= actualDamage;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            Debug.Log($"<color=green>[BossCreature] Absorbed {arrowType} magic! HEALED for {-actualDamage}. Current Health: {currentHealth}</color>");
            return;
        }

        currentHealth -= actualDamage;
        Debug.Log($"<color=orange>[BossCreature] Hit by {arrowType} arrow! Took {actualDamage} damage. Remaining Health: {currentHealth}</color>");

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
            EvaluateThreat(actualDamage);
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

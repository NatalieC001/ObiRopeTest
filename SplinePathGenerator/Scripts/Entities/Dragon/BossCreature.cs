using UnityEngine;

/// <summary>NEEEEEWW
/// The 'Brain' for an advanced Boss creature.
/// It monitors health and battle state, and commands the TacticalBossSplineManager
/// to make intelligent evasion choices (like jumping to escape splines) when threatened.
/// </summary>
[RequireComponent(typeof(AirborneBossMovement))]
public class BossCreature : MonoBehaviour
{

    private BossEventBus eventBus;
    private DesireEvaluator desireEvaluator;
    private EnvironmentTagRegistry tagRegistry;
    private AirborneBossMovement movementManager;
    private ElementalBreathController breathController;
    private MinionRequestBroker requestBroker;
    private RegeneratorController regenerator;
    private CreatureStatusEffects statusEffects;
    private BossPathManager pathManager;

    private float decisionTimer = 0f;
    private const float decisionTickRate = 1f;

    public HealthCrystal LastThreatenedCrystal { get; private set; }
    private float crystalThreatTimeout = 0f;

    // Track minion power deduction
    public int lostMinionCount = 0;

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



    private float recentDamageAccumulator = 0f;
    private float damageDecayTimer = 0f;

    private void Awake()
    {
        movementManager = GetComponent<AirborneBossMovement>();
        breathController = GetComponent<ElementalBreathController>();
        regenerator = GetComponent<RegeneratorController>();
        statusEffects = GetComponent<CreatureStatusEffects>();

        requestBroker = FindFirstObjectByType<MinionRequestBroker>();
        eventBus = FindFirstObjectByType<BossEventBus>();
        desireEvaluator = FindFirstObjectByType<DesireEvaluator>();
        tagRegistry = FindFirstObjectByType<EnvironmentTagRegistry>();
        pathManager = FindFirstObjectByType<BossPathManager>();

        if (eventBus != null)
        {
            eventBus.OnBossDamaged += OnBossDamagedInterrupt;
            eventBus.OnCrystalDamaged += OnCrystalDamagedInterrupt;
            eventBus.OnCrystalDestroyed += OnCrystalDestroyedInterrupt;
            eventBus.OnBossMinionDied += OnMinionDiedInterrupt;
            eventBus.OnBossStatusEnded += OnStatusEndedInterrupt;
        }

        currentHealth = maxHealth;
        currentStamina = maxStamina;
    }

    private void OnDestroy()
    {
        if (eventBus != null)
        {
            eventBus.OnBossDamaged -= OnBossDamagedInterrupt;
            eventBus.OnCrystalDamaged -= OnCrystalDamagedInterrupt;
            eventBus.OnCrystalDestroyed -= OnCrystalDestroyedInterrupt;
            eventBus.OnBossMinionDied -= OnMinionDiedInterrupt;
            eventBus.OnBossStatusEnded -= OnStatusEndedInterrupt;
        }
    }

    private void Start()
    {
        currentPhase = BossPhase.Orchestrator;

        GameObject initialPath = null;
        if (pathManager != null)
        {
            // Directly query the scene-level BossPathManager for an Observation path
            System.Collections.Generic.List<GameObject> obsPaths = pathManager.GetObservationPaths(PathTypeTag.PathType.Airborne);
            if (obsPaths.Count > 0)
            {
                // Select a random valid observation path
                initialPath = obsPaths[Random.Range(0, obsPaths.Count)];

                // Force the boss to instantly snap to the start of this path
                transform.position = initialPath.transform.position;

                if (movementManager != null)
                {
                    // Instruct the movement manager to begin following it
                    movementManager.RequestReturnToCoil(initialPath);
                }
            }
            else
            {
                Debug.LogWarning("[BossCreature] Start: No Airborne Observation paths found in BossPathManager! Spawning freely.");
            }
        }
        else
        {
            Debug.LogError("[BossCreature] Start: BossPathManager is missing from the scene!");
        }

        // Spawn anatomy
        SegmentedDragonManager dragonBody = GetComponent<SegmentedDragonManager>();
        if (dragonBody != null)
        {
            Dreamteck.Splines.SplineComputer startingSpline = initialPath != null ? initialPath.GetComponentInChildren<Dreamteck.Splines.SplineComputer>() : null;
            Debug.Log($"[BossCreature] Instructing SegmentedDragonManager to spawn anatomy. Initial spline: {(startingSpline != null ? startingSpline.name : "none")}");
            dragonBody.InitializeDragon(startingSpline);
        }

        // Give the evaluator time to think, ensuring its first state matches what it wants to do!
        EvaluateDesires();
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
        if (movementManager != null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                            if (playerObj != null)
            {
                if (movementManager != null) movementManager.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerObj.transform.position);
            }
            }
        }
    }

    private void Update()
    {

        if (crystalThreatTimeout > 0f)
        {
            crystalThreatTimeout -= Time.deltaTime;
            if (crystalThreatTimeout <= 0f) LastThreatenedCrystal = null;
        }

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionTickRate;
            EvaluateDesires();
        }

// Handle Phase 2 Stamina Drain
        if (currentPhase == BossPhase.Engaged)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                EnterExhaustedPhase();
            }
            else if (movementManager != null)
            {
                // Let the tactical manager handle the freestyle movement
                GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    if (movementManager != null) movementManager.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerObj.transform.position);
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

        // Permanently decay stamina so fights don't last forever
        maxStamina *= 0.7f;
        if (maxStamina < 20f) maxStamina = 20f; // Minimum stamina floor so it can still fight briefly

        // Command the manager to flee through environmental splines
        // movementManager.ForceImmediateEvasion();
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
    /// Force the boss to begin evasive maneuvers immediately.
    /// Called by other systems (e.g. DragonSegment) when the boss is hit and should react instantly.
    /// This was added to fix the CS1061 compile error and to ensure the dragon uses escape splines on hit.
    /// </summary>
    public void ForceImmediateEvasion()
    {
        if (movementManager == null)
        {
            Debug.LogWarning("[BossCreature] ForceImmediateEvasion called but TacticalBossSplineManager not found.");
            return;
        }

        Debug.Log("<color=red>[BossCreature] ForceImmediateEvasion: ordering immediate tactical evasion.</color>");

        // If the boss was orchestrating, engage first so movement routines behave correctly
        if (currentPhase == BossPhase.Orchestrator)
        {
            EngagePlayer();
        }

        // Trigger the evasion routine on the tactical manager (this will use the configured escape splines)
        movementManager.ForceImmediateEvasion();

        // Reset accumulators so we don't re-trigger immediately
        recentDamageAccumulator = 0f;
        damageDecayTimer = 0f;

        // Move into Exhausted state so tactical flow (recharge after route) is consistent
        currentPhase = BossPhase.Exhausted;
    }

    /// <summary>
    /// Called when the player shoots the boss.
    /// </summary>

    public void EvaluateDesires()
    {
        if (desireEvaluator == null || tagRegistry == null) return;

        DesireResult result = desireEvaluator.Evaluate(this, tagRegistry);

        if (movementManager != null)
        {
            if (result.StrongestDesire == DesireType.CrystalDefense && LastThreatenedCrystal != null)
            {
                EnvironmentTag loop = tagRegistry.GetNearestTag(LastThreatenedCrystal.transform.position, EnvironmentTag.TagType.ObservationLoop);
                if (loop != null)
                {
                    movementManager.RequestReturnToCoil(loop.gameObject);
                }

                if (breathController != null)
                {
                    breathController.FireBreath(BreathType.Fire, result.TargetPosition);
                }

                if (requestBroker != null)
                {
                    requestBroker.RequestMinions(SpawnIntent.DefendCrystal, LastThreatenedCrystal.transform.position);
                }
            }
            else if (result.StrongestDesire == DesireType.Regeneration)
            {
                if (regenerator != null && result.TargetTransform != null)
                {
                    HealthCrystal crystal = result.TargetTransform.GetComponentInParent<HealthCrystal>();
                    if (crystal != null)
                    {
                        regenerator.BeginRegeneration(crystal);
                    }
                }
            }
            else if (result.StrongestDesire == DesireType.Survival)
            {
                movementManager.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Withdraw, transform.position + Vector3.up * 30f);
            }
        }
    }

    private void OnBossDamagedInterrupt(BossCreature boss, ElementTypeOB7 type)
    {
        if (boss == this) EvaluateDesires();
    }

    private void OnCrystalDamagedInterrupt(HealthCrystal crystal)
    {
        LastThreatenedCrystal = crystal;
        crystalThreatTimeout = 5f;
        EvaluateDesires();
    }

    private void OnCrystalDestroyedInterrupt(HealthCrystal crystal)
    {
        if (LastThreatenedCrystal == crystal) LastThreatenedCrystal = null;
        EvaluateDesires();
    }

    private void OnMinionDiedInterrupt(GameObject minion)
    {
        lostMinionCount++;
        EvaluateDesires();
    }

    private void OnStatusEndedInterrupt()
    {
        EvaluateDesires();
    }

    public float GetCurrentHealthPct()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }

    public void TakeDamage(float baseAmount, Vector3 hitPoint, ElementTypeOB7 arrowType = ElementTypeOB7.Normal)
    {
        // 0. Trigger Status Effects (Slows, Freezes)
        if (statusEffects != null)
        {
            statusEffects.ApplyElementalEffect(arrowType);
        }

        if (eventBus != null) eventBus.TriggerBossDamaged(this, arrowType);

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
            movementManager.ForceImmediateEvasion();

            // Reset accumulator so it doesn't immediately evade again
            recentDamageAccumulator = 0f;
        }
    }

    private void Die()
    {
        Debug.Log("<color=red>[BossCreature] The Boss has been defeated!</color>");

        // Stop movement
        if (movementManager != null) movementManager.enabled = false;

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
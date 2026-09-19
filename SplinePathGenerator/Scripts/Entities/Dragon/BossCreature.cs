using UnityEngine;

/// <summary>
/// The 'State Holder' for the Boss creature.
/// Stores health, stamina, and processes incoming damage to pass to the BossEventBus.
/// The actual AI logic has been moved to QuestMachine (DragonTick.cs).
/// </summary>
[RequireComponent(typeof(AirborneBossMovement))]
public class BossCreature : MonoBehaviour
{
    private BossEventBus eventBus;
    private CreatureStatusEffects statusEffects;

    public HealthCrystal LastThreatenedCrystal { get; private set; }
    public float crystalThreatTimeout = 0f;

    public int lostMinionCount = 0;

    [System.Serializable]
    public struct ElementalModifier
    {
        public ElementTypeOB7 arrowType;
        public float damageMultiplier;
    }

    [Header("Boss Stats")]
    public float maxHealth = 500f;
    public float currentHealth { get; private set; }

    [Header("Elemental Resistances")]
    public ElementalModifier[] elementalModifiers;

    [Header("Energy System")]
    public float maxStamina = 100f;
    public float staminaDrainRate = 10f;
    public float staminaRechargeRate = 15f;
    public float currentStamina { get; set; }

    private WaveSpawner waveSpawner;

    public float recentDamageAccumulator = 0f;
    public float damageDecayTimer = 0f;
    public float evasionDamageThreshold = 50f;

    public enum BossPhase
    {
        Orchestrator,
        Engaged,
        Exhausted,
        Recharging
    }
    public BossPhase currentPhase = BossPhase.Orchestrator;

    public void Initialize(WaveSpawner spawner)
    {
        waveSpawner = spawner;
    }

    private void Awake()
    {
        statusEffects = GetComponent<CreatureStatusEffects>();
        eventBus = FindFirstObjectByType<BossEventBus>();

        if (eventBus != null)
        {
            eventBus.OnCrystalDamaged += OnCrystalDamagedInterrupt;
            eventBus.OnCrystalDestroyed += OnCrystalDestroyedInterrupt;
        }

        currentHealth = maxHealth;
        currentStamina = maxStamina;

        // Spawn anatomy
        SegmentedDragonManager dragonBody = GetComponent<SegmentedDragonManager>();
        if (dragonBody != null)
        {
            BossPathManager pathManager = FindFirstObjectByType<BossPathManager>();
            GameObject initialPath = null;
            if (pathManager != null)
            {
                System.Collections.Generic.List<GameObject> obsPaths = pathManager.GetObservationPaths(PathTypeTag.PathType.Airborne);
                if (obsPaths.Count > 0)
                {
                    initialPath = obsPaths[UnityEngine.Random.Range(0, obsPaths.Count)];
                    Dreamteck.Splines.SplineComputer spline = initialPath.GetComponentInChildren<Dreamteck.Splines.SplineComputer>();
                    if (spline != null) transform.position = spline.EvaluatePosition(0.0);
                    else transform.position = initialPath.transform.position;

                    AirborneBossMovement movementManager = GetComponent<AirborneBossMovement>();
                    if (movementManager != null)
                    {
                        movementManager.RequestReturnToCoil(initialPath);
                    }
                }
            }
            dragonBody.InitializeDragon(initialPath != null ? initialPath.GetComponentInChildren<Dreamteck.Splines.SplineComputer>() : null);
        }

        Renderer[] allRenderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in allRenderers)
        {
            r.enabled = true;
        }
    }

    private void OnDestroy()
    {
        if (eventBus != null)
        {
            eventBus.OnCrystalDamaged -= OnCrystalDamagedInterrupt;
            eventBus.OnCrystalDestroyed -= OnCrystalDestroyedInterrupt;
        }
    }

    private void OnCrystalDamagedInterrupt(HealthCrystal crystal)
    {
        LastThreatenedCrystal = crystal;
        crystalThreatTimeout = 5f;
    }

    private void OnCrystalDestroyedInterrupt(HealthCrystal crystal)
    {
        if (LastThreatenedCrystal == crystal) LastThreatenedCrystal = null;
    }

    public float GetCurrentHealthPct()
    {
        return maxHealth > 0 ? currentHealth / maxHealth : 0f;
    }

    public void TakeDamage(float baseAmount, Vector3 hitPoint, ElementTypeOB7 arrowType = ElementTypeOB7.Normal)
    {
        if (statusEffects != null) statusEffects.ApplyElementalEffect(arrowType);
        if (eventBus != null) eventBus.TriggerBossDamaged(this, arrowType);

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

        if (statusEffects != null && statusEffects.IsBrittle && actualDamage > 0) actualDamage *= 2.0f;

        if (actualDamage < 0)
        {
            currentHealth -= actualDamage;
            if (currentHealth > maxHealth) currentHealth = maxHealth;
            return;
        }

        currentHealth -= actualDamage;
        recentDamageAccumulator += actualDamage;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        WaveSpawner spawnerToNotify = waveSpawner ?? FindFirstObjectByType<WaveSpawner>();
        if (spawnerToNotify != null) spawnerToNotify.NotifyTargetDestroyed();

        AirborneBossMovement movementManager = GetComponent<AirborneBossMovement>();
        if (movementManager != null) movementManager.enabled = false;

        SegmentedDragonManager dragonBody = GetComponent<SegmentedDragonManager>();
        if (dragonBody != null) dragonBody.TriggerTotalDeath();

        Destroy(gameObject, 1f);
    }
}

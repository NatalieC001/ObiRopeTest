using UnityEngine;

/// <summary>
/// A foundation script to identify standard creatures in the game.
/// These are typically the grunts that ride geometric spline shapes and use the Swarm logic,
/// clearly separated from the basic 'MovingTarget' test objects.
/// </summary>
public class StandardCreature : MonoBehaviour
{
    [System.Serializable]
    public struct ElementalModifier
    {
        public ElementTypeOB7 arrowType;
        [Tooltip("1.0 = normal damage. 2.0 = double damage (weakness). 0.0 = immune. -1.0 = heals the creature!")]
        public float damageMultiplier;
    }

    [Header("Creature Stats")]
    public float health = 100f;
    public float maxHealth = 100f;

    [Header("Elemental Resistances")]
    [Tooltip("If an arrow type isn't listed here, it deals standard 1.0x damage.")]
    public ElementalModifier[] elementalModifiers;

    [Header("Physics & Targeting")]
    [Tooltip("The physics layer this creature will be forced onto so arrows can detect it. Displayed here as a reminder!")]
    [SerializeField] private string targetLayer = "Enemy";

    private CreatureStatusEffects statusEffects;
    private MinionManager myManager;

    private void Awake()
    {
        // Force the physics layer so arrows detect this creature, even if the dev forgot to set it!
        int layerIndex = LayerMask.NameToLayer(targetLayer);
        if (layerIndex != -1)
        {
            gameObject.layer = layerIndex;
        }
        else
        {
            Debug.LogWarning($"[StandardCreature] Layer '{targetLayer}' does not exist in your project settings!");
        }

        // Try to find the status effect component (optional, but recommended)
        statusEffects = GetComponent<CreatureStatusEffects>();
    }

    /// <summary>
    /// Injected explicitly by the WaveSpawner when this object is instantiated.
    /// Eliminates the need for brittle Singletons.
    /// </summary>
    public void Initialize(MinionManager manager)
    {
        myManager = manager;
        if (myManager != null)
        {
            myManager.RegisterMinion(this);
        }
    }

    private void Start()
    {
        health = maxHealth;
    }

    /// <summary>
    /// Called when the player shoots this creature.
    /// </summary>
    public virtual void TakeDamage(float baseAmount, Vector3 hitPoint, ElementTypeOB7 arrowType = ElementTypeOB7.Normal)
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
                    break; // Found the specific modifier, stop searching
                }
            }
        }

        // 1.5 Apply Brittle modifier if frozen by previous ice arrows
        if (statusEffects != null && statusEffects.IsBrittle && actualDamage > 0)
        {
            Debug.Log($"<color=cyan>[StandardCreature] Brittle shattered! Damage doubled.</color>");
            actualDamage *= 2.0f;
        }

        // 2. Apply damage or healing
        if (actualDamage < 0)
        {
            // Healing logic (negative damage = positive health)
            health -= actualDamage;
            if (health > maxHealth) health = maxHealth;
            Debug.Log($"<color=green>[StandardCreature] {gameObject.name} absorbed {arrowType} magic and HEALED for {-actualDamage}!</color>");
            return;
        }

        health -= actualDamage;

        Debug.Log($"[StandardCreature] {gameObject.name} hit by {arrowType} arrow. Took {actualDamage} damage. Health remaining: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"[StandardCreature] {gameObject.name} has died. Triggering visuals.");

        // 1. Instantly stop movement
        Dreamteck.Splines.SplineFollower follower = GetComponent<Dreamteck.Splines.SplineFollower>();
        if (follower != null)
        {
            follower.follow = false;
        }

        // Do NOT disable colliders prematurely!
        // If we do, arrows will detach or stick to invisible objects.
        // The visuals must complete first while holding the physical arrows.

        // 2. Trigger Dissolve (if it exists), otherwise finalize death immediately.
        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            dissolve.OnDissolveCompleted += HandleDissolveCompleted;
            dissolve.StartDissolve();
        }
        else
        {
            HandleDissolveCompleted();
        }
    }

    private void HandleDissolveCompleted()
    {
        // Ping the injected manager that this minion has visually finished dying
        // The manager handles tracking logic and physical scene destruction of the root
        if (myManager != null)
        {
            myManager.OnMinionDestroyed(this);
        }
        else
        {
            Debug.LogWarning("[StandardCreature] Minion died but no manager was injected! Destroying self manually.");
            Destroy(transform.root.gameObject);
        }
    }
}

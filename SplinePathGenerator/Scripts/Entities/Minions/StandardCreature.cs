using UnityEngine;

/// <summary>
/// A foundation script to identify standard creatures in the game.
/// These are typically the grunts that ride geometric spline shapes and use the Swarm logic,
/// clearly separated from the basic 'MovingTarget' test objects.
/// </summary>
public class StandardCreature : MonoBehaviour, IArrowTarget
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
    private MinionManager minionManager;

    public void Initialize(MinionManager manager)
    {
        minionManager = manager;
        if (minionManager != null)
        {
            minionManager.RegisterMinion(this);
        }
    }

    private void Awake()
    {
        // Force the physics layer so arrows detect this creature, even if the dev forgot to set it!
        int layerIndex = LayerMask.NameToLayer(targetLayer);
        if (layerIndex != -1)
        {
            gameObject.layer = layerIndex;

            // Also explicitly ensure the collider children are on the layer, as that's what physics actually hits
            Collider[] childColliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider col in childColliders)
            {
                if (col != null)
                {
                    col.gameObject.layer = layerIndex;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[StandardCreature] Layer '{targetLayer}' does not exist in your project settings!");
        }

        // Try to find the status effect component (optional, but recommended)
        statusEffects = GetComponent<CreatureStatusEffects>();
    }

    private void Start()
    {
        health = maxHealth;

        // Exactly mirror the DragonSegment intercept logic:
        // We subscribe to the dissolve completion event right at the start.
        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            dissolve.OnDissolveCompleted += FinalizeDestruction;
        }
    }

    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        TakeDamage(damage, impactPoint, elementType);
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
        Debug.Log($"[StandardCreature] {gameObject.name} has died.");

        // 1. Ensure we detach from any Dreamteck splines properly upon death
        Dreamteck.Splines.SplineFollower follower = GetComponent<Dreamteck.Splines.SplineFollower>();
        if (follower != null)
        {
            follower.follow = false;
        }

        // 2. Disable all physical colliders in children immediately so arrows stop sticking/colliding
        Collider[] childColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in childColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        // 3. Trigger dissolve on any arrows sticking out of this creature simultaneously
        StickingArrow[] attachedArrows = GetComponentsInChildren<StickingArrow>(true);
        foreach (StickingArrow arrow in attachedArrows)
        {
            if (arrow != null)
            {
                DissolveEffect arrowDissolve = arrow.GetComponentInChildren<DissolveEffect>();
                if (arrowDissolve != null)
                {
                    arrowDissolve.TriggerDissolve();
                }
            }
        }

        // Explicitly command the segment's visual effect to start dissolving!
        // This will eventually fire the OnDissolveCompleted event we subscribed to in Start,
        // which will trigger FinalizeDestruction().
        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            dissolve.TriggerDissolve();
        }
        else
        {
            // Fallback: If no DissolveEffect exists to fire the event, we just destroy it now
            FinalizeDestruction();
        }
    }

    private void OnDestroy()
    {
        // Clean up the event listener to avoid memory leaks
        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            dissolve.OnDissolveCompleted -= FinalizeDestruction;
        }
    }

    /// <summary>
    /// Called EXACTLY when the visual dissolve finishes via callback.
    /// Safely purges the segment from the tracking arrays and obliterates the GameObject hierarchy.
    /// </summary>
    private void FinalizeDestruction()
    {
        // Tell the manager to wipe this piece from the tracking array and close the gap!
        if (minionManager != null)
        {
            minionManager.OnMinionDestroyed(this);
        }

        // Permanently destroy the root object, which automatically takes the Mesh, Collider, and Arrows with it.
        Destroy(gameObject);
    }
}

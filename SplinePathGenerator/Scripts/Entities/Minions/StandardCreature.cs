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

    private bool isDead = false;

    private void Awake()
    {
        // Force the physics layer so arrows detect this creature, even if the dev forgot to set it!
        int layerIndex = LayerMask.NameToLayer(targetLayer);
        if (layerIndex != -1)
        {
            gameObject.layer = layerIndex;

            // Explicitly iterate through all child colliders to ensure nested physical colliders receive the layer
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
            {
                col.gameObject.layer = layerIndex;
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
    }

    /// <summary>
    /// Called when the player shoots this creature.
    /// </summary>
    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {

        TakeDamage(damage, impactPoint, elementType);
    }

    public virtual void TakeDamage(float baseAmount, Vector3 hitPoint, ElementTypeOB7 arrowType = ElementTypeOB7.Normal)
    {
        if (isDead) return;
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
        isDead = true;
        Debug.Log($"[StandardCreature] {gameObject.name} has died.");

        // Ensure we detach from any Dreamteck splines properly upon death
        Dreamteck.Splines.SplineFollower follower = GetComponent<Dreamteck.Splines.SplineFollower>();
        if (follower != null)
        {
            follower.follow = false;
        }

        DissolveEffect dissolve = GetComponentInChildren<DissolveEffect>();
        if (dissolve != null)
        {
            dissolve.TriggerDissolve(() => {
                Destroy(gameObject);
            });
        }
        else
        {
            Destroy(gameObject);
        }
    }
}

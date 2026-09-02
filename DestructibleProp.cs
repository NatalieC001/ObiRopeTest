using UnityEngine;
using System.Collections;

/// <summary>
/// A standalone, robust gameplay target for the Bash (Lightweight) mechanic.
/// Replaces the sandbox MovingTarget.cs for actual gameplay elements like barrels, crates, and small enemies.
/// Relies on a centralized Manager (RopeArrowPairOB7) to handle complex physics state transitions.
/// </summary>
[RequireComponent(typeof(ObjectWeight))]
[RequireComponent(typeof(Rigidbody))]
public class DestructibleProp : MonoBehaviour, IArrowTarget
{
    [Header("Destructible Settings")]
    [Tooltip("The amount of damage this prop can take before breaking.")]
    public float maxHealth = 1f;

    [Tooltip("How long to wait before destroying the GameObject after death.")]
    public float deathDespawnDelay = 2.5f;

    [Tooltip("Minimum crash speed required to take instant fatal damage upon collision.")]
    public float fatalCrashThreshold = 4f;

    [Tooltip("The element type this prop natively interacts with. Defaults to Normal.")]
    public ElementTypeOB7 baseElementType = ElementTypeOB7.Normal;

    [Header("Effects")]
    [SerializeField] private AudioSource impactAudio;
    [SerializeField] private MeshRenderer propRenderer;

    private float currentHealth;
    private bool isDead = false;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;

        // By default, lightweight bash targets start kinematic so they don't roll around
        // until acted upon by forces, arrows, or manager logic.
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (isDead) return;

        // Play impact sound if hitting something hard while not kinematic
        if (!rb.isKinematic && impactAudio != null && collision.relativeVelocity.magnitude > 1f)
        {
            if (!impactAudio.isPlaying)
            {
                impactAudio.Play();
            }
        }

        // Fatal crash logic for Bash mechanics
        if (!rb.isKinematic && collision.relativeVelocity.magnitude >= fatalCrashThreshold)
        {
            Debug.Log($"[DestructibleProp] '{gameObject.name}' crashed at {collision.relativeVelocity.magnitude:F1} speed! Taking fatal crash damage.");
            TakeDamage(maxHealth, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
        }
    }

    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        Debug.Log($"<color=cyan>[DestructibleProp] '{gameObject.name}' hit! Damage: {damage}, Element: {elementType}.</color>");

        if (damage <= 0f && elementType == ElementTypeOB7.Normal)
        {
            Debug.Log($"<color=cyan>[DestructibleProp] 0-damage Rope Arrow hit detected.</color>");
            return;
        }

        if (baseElementType == ElementTypeOB7.Normal || elementType == baseElementType)
        {
            TakeDamage(damage, impactPoint);
        }
        else
        {
            Debug.Log($"[DestructibleProp] Ineffective element! Needs {baseElementType}, got {elementType}.");
        }
    }

    private void TakeDamage(float damage, Vector3 impactPoint)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die(impactPoint);
        }
        // Physics state transitions (like becoming dynamic) are managed explicitly by the manager.
    }

    private void Die(Vector3 impactPoint)
    {
        isDead = true;
        Debug.Log($"<color=red>[DestructibleProp] '{gameObject.name}' destroyed!</color>");

        // Force breaking ropes is currently handled by the sandbox manager iteration in MovingTarget.
        // For a robust system, the Manager should detect target destruction via event or polling.
        // To maintain backwards compatibility without rewriting Manager destruction polling yet:
        if (RopeArrowManagerObi7.Instance != null)
        {
            for (int i = RopeArrowManagerObi7.Instance.activePairs.Count - 1; i >= 0; i--)
            {
                var pair = RopeArrowManagerObi7.Instance.activePairs[i];
                if (IsArrowAttachedToMe(pair.Arrow1) || IsArrowAttachedToMe(pair.Arrow2))
                {
                    pair.ForceBreakRope();
                }
            }
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.AddForceAtPosition((transform.position - impactPoint).normalized * 5f, impactPoint, ForceMode.Impulse);
        }

        DissolveEffect[] dissolveComponents = GetComponentsInChildren<DissolveEffect>();
        foreach (DissolveEffect effect in dissolveComponents)
        {
            effect.TriggerDissolve();
        }

        Destroy(gameObject, deathDespawnDelay);
    }

    private bool IsArrowAttachedToMe(StickingArrow arrow)
    {
        if (arrow == null) return false;
        Transform current = arrow.transform.parent;
        while (current != null)
        {
            if (current.gameObject == this.gameObject)
            {
                return true;
            }
            current = current.parent;
        }
        return false;
    }

}

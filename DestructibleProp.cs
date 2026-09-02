using UnityEngine;
using System.Collections;

/// <summary>
/// ====================================================================================================
/// DESTRUCTIBLE PROP - STANDALONE BASH TARGET
/// ====================================================================================================
///
/// WHAT THIS CLASS DOES:
/// This is the primary standalone class used to create "Bashable" gameplay elements like wooden barrels,
/// explosive crates, and small enemy units. It implements the IArrowTarget interface, allowing it to
/// receive damage and elemental effects from arrows.
///
/// ARCHITECTURE & PHYSICS DELEGATION:
/// Unlike earlier sandbox prototypes, this class is intentionally "dumb" regarding its own complex
/// physics state changes. It does NOT toggle its own Rigidbody (e.g., turning isKinematic on/off)
/// when it is hit by a rope. Instead, the logic referee (RopeArrowPairOB7) determines the rope state
/// based on ObjectWeight, and the BashPhysicsManager executes the actual Rigidbody property changes,
/// dictating when this prop should fly through the air and when it should freeze.
///
/// HOW TO SET THIS UP IN UNITY:
/// 1. Attach this script to your 3D Model (e.g., a Goblin or Barrel).
/// 2. ObjectWeight Component (Required): Set Mobility to "Lightweight".
/// 3. Rigidbody Component (Required):
///     - Mass: Keep it relatively low (e.g., 1 to 5) so it snaps quickly when bashed.
///     - Drag: 0 (The BashPhysicsManager will temporarily spike the drag to 3.0 during a bash to prevent VR whiplash).
///     - isKinematic: Check this box (TRUE). The prop should sit still until the manager rips it away.
///     - Interpolate: Set to "Interpolate" for smooth VR movement.
/// 4. Collider Component (Required): A BoxCollider or SphereCollider so arrows can hit it.
/// 5. AudioSource Component (Required): For playing impact sounds automatically.
/// </summary>
[RequireComponent(typeof(ObjectWeight))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
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
    [Tooltip("If left empty, will automatically grab the required AudioSource on this object.")]
    [SerializeField] private AudioSource impactAudio;
    [SerializeField] private MeshRenderer propRenderer;

    private float currentHealth;
    private bool isDead = false;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (impactAudio == null)
        {
            impactAudio = GetComponent<AudioSource>();
        }

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

        // Clean up any ropes explicitly attached to this object when it is destroyed.
        // This is a standalone destruction handler for the new Bash mechanics that ensures
        // ropes break cleanly without relying on legacy manager loops from the sandbox tests.
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

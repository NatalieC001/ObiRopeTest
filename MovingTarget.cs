
using System;
using UnityEngine;
using Random = UnityEngine.Random;




[System.Serializable]
public struct ElementVisualConfig
{
    public ElementTypeOB7 elementType;
    public Material elementMaterial;
}

public class MovingTarget : MonoBehaviour, IArrowTarget
{
    [Header("Element Configuration")]
    [Tooltip("Add the different element types and their materials here. The target will pick one to use.")]
    [SerializeField]
    private ElementVisualConfig[] availableElements;

    [Tooltip("The MeshRenderer to apply the element's material to.")]
    [SerializeField]
    private MeshRenderer targetRenderer;

    // Stores the currently active element type
    public ElementTypeOB7 CurrentElementType { get; private set; } = ElementTypeOB7.Normal;

    [Header("Shifting Element Settings")]
    [Tooltip("If checked, this target will randomly swap elements while the player is aiming at it!")]
    [SerializeField]
    private bool shiftsElementsMidGame = false;

    [Tooltip("How many seconds between elemental color swaps if shifting is enabled.")]
    [SerializeField]
    private float shiftInterval = 3f;

    [Header("Target Settings")]
    [SerializeField]
    private float health = 1f;

    [Tooltip("How long to wait before destroying the GameObject")]
    [SerializeField]
    private float deathDespawnDelay = 2.5f;

    [SerializeField]
    private AudioSource audioSource;

    private Rigidbody rb;
    private bool stopped = false;
    private Material originalMaterial;

    public bool IsStopped => stopped;

    private float shiftTimer = 0f;
    private int currentMaxDifficulty = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (targetRenderer != null)
        {
            originalMaterial = targetRenderer.material;
        }
    }

    private void Start()
    {
        // If not managed by SandboxResetManager, just default to Normal.
    }

    private void Update()
    {
        if (shiftsElementsMidGame && !stopped && CurrentElementType != ElementTypeOB7.Normal)
        {
            shiftTimer += Time.deltaTime;
            if (shiftTimer >= shiftInterval)
            {
                shiftTimer = 0f;
                SetRandomElement();
            }
        }
    }

    public void SetRoundDifficulty(int round)
    {
        currentMaxDifficulty = round;

        if (round == 0)
        {
            CurrentElementType = ElementTypeOB7.Normal;
            if (targetRenderer != null && originalMaterial != null)
            {
                targetRenderer.material = originalMaterial;
            }
        }
        else
        {
            SetRandomElement();
        }
    }

    public void SetRandomElement()
    {
        if (availableElements == null || availableElements.Length == 0)
        {
            CurrentElementType = ElementTypeOB7.Normal;
            if (targetRenderer != null && originalMaterial != null)
            {
                targetRenderer.material = originalMaterial;
            }
            return;
        }

        int maxAllowedIndex = Mathf.Min(currentMaxDifficulty, availableElements.Length);
        int randomIndex = Random.Range(0, maxAllowedIndex);

        ElementVisualConfig config = availableElements[randomIndex];
        CurrentElementType = config.elementType;

        if (config.elementMaterial != null && targetRenderer != null)
        {
            targetRenderer.material = config.elementMaterial;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (stopped && !rb.isKinematic && audioSource != null)
        {
            audioSource.Play();
        }

        if (!stopped && rb != null && !rb.isKinematic)
        {
            float crashSpeed = collision.relativeVelocity.magnitude;
            float fatalCrashThreshold = 4f;

            if (crashSpeed >= fatalCrashThreshold)
            {
                Debug.Log($"Target crashed at {crashSpeed:F1} speed! Taking fatal crash damage.");
                TakeDamage(health, collision.contacts.Length > 0 ? collision.contacts[0].point : transform.position);
            }
        }
    }

    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        Debug.Log($"<color=cyan>[MovingTarget] '{gameObject.name}' received OnArrowHit with Damage: {damage}, Element: {elementType}.</color>");

        if (damage <= 0f && elementType == ElementTypeOB7.Normal)
        {
            // Registration is now handled exclusively by StickingArrow.cs to prevent double-registration bugs.
            Debug.Log($"<color=cyan>[MovingTarget] 0-damage hit detected! Rope Arrow registration is delegated to the arrow itself.</color>");
            return;
        }

        if (CurrentElementType == ElementTypeOB7.Normal)
        {
            TakeDamage(damage, impactPoint);
            return;
        }

        if (elementType == CurrentElementType)
        {
            TakeDamage(damage, impactPoint);
        }
        else
        {
            Debug.Log($"Target needs {CurrentElementType} arrow, but got {elementType}!");
        }
    }

    public void EnableInternalLogic()
    {
        Debug.Log($"Re-enable NavMesh or other scripts here since the creature has escaped tether.");

        ObjectWeight weight = GetComponent<ObjectWeight>();
        if (weight == null)
        {
            Debug.LogWarning($"<color=yellow>[MovingTarget] '{gameObject.name}' is missing an ObjectWeight component. Defaulting behavior.</color>");
            return;
        }

        if (weight.mobility == RopeTargetMobility.Lightweight && rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public void DisableInternalLogic()
    {
        Debug.Log($"Disable NavMesh or other scripts here while we have captured creature or target.");

        ObjectWeight weight = GetComponent<ObjectWeight>();
        if (weight == null)
        {
            Debug.LogWarning($"<color=yellow>[MovingTarget] '{gameObject.name}' is missing an ObjectWeight component. Defaulting behavior.</color>");
            return;
        }

        if (weight.mobility == RopeTargetMobility.Lightweight && rb != null)
        {
            rb.isKinematic = false;
        }
    }

    public void GetHit(Transform ropeAnchor)
    {
        TakeDamage(1f, transform.position);
    }

    private void TakeDamage(float damage, Vector3 impactPoint)
    {
        if (stopped) return;

        health -= damage;
        if (health <= 0)
        {
            stopped = true;

            // Check if this target has a rope attached and destroy it
            if (RopeArrowManagerObi7.Instance != null)
            {
                for (int i = RopeArrowManagerObi7.Instance.activePairs.Count - 1; i >= 0; i--)
                {
                    var pair = RopeArrowManagerObi7.Instance.activePairs[i];

                    // Check if Arrow1 or Arrow2 is stuck in this target
                    // The arrow is parented to an anchor, which is parented to the target
                    bool arrow1OnThisTarget = false;
                    bool arrow2OnThisTarget = false;

                    if (pair.Arrow1 != null)
                    {
                        Transform current = pair.Arrow1.transform.parent;
                        while (current != null)
                        {
                            if (current.gameObject == this.gameObject)
                            {
                                arrow1OnThisTarget = true;
                                break;
                            }
                            current = current.parent;
                        }
                    }

                    if (pair.Arrow2 != null)
                    {
                        Transform current = pair.Arrow2.transform.parent;
                        while (current != null)
                        {
                            if (current.gameObject == this.gameObject)
                            {
                                arrow2OnThisTarget = true;
                                break;
                            }
                            current = current.parent;
                        }
                    }

                    if (arrow1OnThisTarget || arrow2OnThisTarget)
                    {
                        // Schedule the rope destruction for next frame to avoid physics callback errors
                        StartCoroutine(DelayedDestroyRope(pair));
                        RopeArrowManagerObi7.Instance.activePairs.RemoveAt(i);
                        break;
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
                // Ensure we do not dissolve rope arrows that are stuck in this target
                StickingArrow parentArrow = effect.GetComponentInParent<StickingArrow>();
                if (parentArrow != null && parentArrow.arrowCategory == ArrowCategory.Rope)
                {
                    continue;
                }

                effect.TriggerDissolve();
            }

            Destroy(gameObject, deathDespawnDelay);
        }
    }

    private System.Collections.IEnumerator DelayedDestroyRope(RopeArrowPairOB7 pair)
    {
        yield return null; // Wait one frame
        if (pair != null)
        {
            pair.ForceBreakRope();
        }
    }
}

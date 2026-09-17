using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ATTACH TO: The GameObject that should dissolve (Enemies, Targets, or the Arrow itself).
/// Implements IArrowTarget so it can instantly dissolve when shot if desired, but can also be triggered manually (e.g., by the Arrow's own script after a delay).
/// </summary>
public class DissolveEffect : MonoBehaviour, IArrowTarget
{
    [Header("Renderers")]
    [Tooltip("The Renderer(s) containing the material to dissolve. If empty, tries to find one on this GameObject or children.")]
    [SerializeField] private Renderer[] targetRenderers;

    [Header("Shader Properties")]
    [Tooltip("The name of the float property that controls the dissolve amount (0 to 1).")]
    [SerializeField] private string dissolvePropertyName = "_Dissolve";
    [Tooltip("If your shader uses a float property to toggle the dissolve effect on/off, specify its name here. Leave empty if not used.")]
    [SerializeField] private string dissolveTogglePropertyName = "_UseDissolve";
    [Tooltip("The Shader Keyword that must be enabled for the dissolve math to run (e.g. TCP2_DISSOLVE)")]
    [SerializeField] private string requiredShaderKeyword = "TCP2_DISSOLVE";

    [Header("Settings")]
    [Tooltip("How long the dissolve animation should take in seconds.")]
    [SerializeField] private float dissolveDuration = 1.0f;
    [Tooltip("Should this script start dissolving the moment it receives an OnArrowHit event? (True for Targets/Enemies WITHOUT health logic, False for objects that manage their own health/delays).")]
    [SerializeField] private bool dissolveImmediatelyOnHit = true;

    // Completion callback invoked when dissolve finishes
    private Action onCompleteCallback;
    public event Action OnDissolveCompleted;

    private MaterialPropertyBlock propBlock;
    private bool isDissolving = false;

    private void Awake()
    {
        if (targetRenderers == null || targetRenderers.Length == 0)
        {
            targetRenderers = GetComponentsInChildren<Renderer>();
        }

        propBlock = new MaterialPropertyBlock();
        ResetDissolve(); // Setup initial state
    }

    /// <summary>
    /// Triggered by the IArrowTarget interface when an arrow hits this object.
    /// </summary>
    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        // Try to forward the damage to a creature brain first!
        // We check GetComponentInParent so it finds the brain even if DissolveEffect is nested on a child mesh.
        StandardCreature minionBrain = GetComponentInParent<StandardCreature>();
        if (minionBrain != null)
        {
            minionBrain.TakeDamage(damage, impactPoint, elementType);
            return; // The brain will call TriggerDissolve() manually when health hits 0.
        }

        BossCreature bossBrain = GetComponentInParent<BossCreature>();
        if (bossBrain != null)
        {
            bossBrain.TakeDamage(damage, impactPoint, elementType);
            return; // The boss handles its own phase changes and death.
        }

        // If no brain exists (e.g. hitting a simple target), rely on the manual toggle.
        if (dissolveImmediatelyOnHit && !isDissolving && gameObject.activeInHierarchy)
        {
            TriggerDissolve();
        }
    }

    /// <summary>
    /// Call this publicly to force the dissolve to start (e.g., from an enemy health script, or after an arrow sticks delay)
    /// without a completion callback.
    /// </summary>
    public void TriggerDissolve()
    {
        TriggerDissolve(null);
    }

    /// <summary>
    /// Call this to start the dissolve and receive a completion callback when the dissolve finishes.
    /// </summary>
    public void TriggerDissolve(Action onComplete)
    {
        if (!isDissolving && gameObject.activeInHierarchy)
        {
            onCompleteCallback = onComplete;
            StartCoroutine(DissolveRoutine());
        }
        else if (isDissolving && onComplete != null)
        {
            // If already dissolving, still call the callback after current routine finishes:
            // chain it onto OnDissolveCompleted event so caller still gets notified.
            OnDissolveCompleted += onComplete;
        }
    }

    /// <summary>
    /// Resets the material property blocks so the mesh is fully visible again.
    /// Essential for objects coming out of a LeanPool.
    /// </summary>
    public void ResetDissolve()
    {
        isDissolving = false;

        if (targetRenderers == null || targetRenderers.Length == 0) return;

        foreach (var rend in targetRenderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propBlock);

            // Start with dissolve disabled (0)
            if (!string.IsNullOrEmpty(dissolveTogglePropertyName))
            {
                propBlock.SetFloat(dissolveTogglePropertyName, 0f);
            }

            // Start with dissolve amount at 0 (fully visible)
            propBlock.SetFloat(dissolvePropertyName, 0f);

            rend.SetPropertyBlock(propBlock);

            // Disable the keyword to save GPU performance while fully visible
            if (!string.IsNullOrEmpty(requiredShaderKeyword))
            {
                foreach (var mat in rend.materials)
                {
                    mat.DisableKeyword(requiredShaderKeyword);
                }
            }

            rend.enabled = true;
        }
    }

    /// <summary>
    /// The Coroutine that animating the dissolve property from 0 to 1 over time.
    /// Invokes the onComplete callback and OnDissolveCompleted event when finished.
    /// </summary>
    public IEnumerator DissolveRoutine()
    {
        isDissolving = true;

        if (targetRenderers == null || targetRenderers.Length == 0) yield break;

        // Enable the dissolve toggle and KEYWORD
        foreach (var rend in targetRenderers)
        {
            if (rend != null)
            {
                // CRITICAL FIX: Unity PropertyBlocks cannot reliably enable Shader Keywords.
                // We MUST enable the keyword directly on the Material instance so the GPU processes the dissolve math.
                if (!string.IsNullOrEmpty(requiredShaderKeyword))
                {
                    foreach (var mat in rend.materials)
                    {
                        mat.EnableKeyword(requiredShaderKeyword);
                    }
                }

                rend.GetPropertyBlock(propBlock);
                if (!string.IsNullOrEmpty(dissolveTogglePropertyName))
                {
                    propBlock.SetFloat(dissolveTogglePropertyName, 1f);
                }
                rend.SetPropertyBlock(propBlock);
            }
        }

        float timeElapsed = 0f;

        while (timeElapsed < dissolveDuration)
        {
            timeElapsed += Time.deltaTime;
            float dissolveValue = Mathf.Clamp01(timeElapsed / dissolveDuration);

            foreach (var rend in targetRenderers)
            {
                if (rend != null)
                {
                    rend.GetPropertyBlock(propBlock);
                    propBlock.SetFloat(dissolvePropertyName, dissolveValue);
                    rend.SetPropertyBlock(propBlock);
                }
            }
            yield return null;
        }

        // Finish exactly at 1 and disable renderers visually
        foreach (var rend in targetRenderers)
        {
            if (rend != null)
            {
                rend.GetPropertyBlock(propBlock);
                propBlock.SetFloat(dissolvePropertyName, 1f);

                // You can optionally disable the toggle here, but the object is invisible anyway
                if (!string.IsNullOrEmpty(dissolveTogglePropertyName))
                {
                    propBlock.SetFloat(dissolveTogglePropertyName, 0f);
                }
                rend.SetPropertyBlock(propBlock);

                // Hide the mesh entirely
                rend.enabled = false;
            }
        }

        // Invoke callbacks / events
        try
        {
            onCompleteCallback?.Invoke();
            OnDissolveCompleted?.Invoke();
        }
        finally
        {
            onCompleteCallback = null;
            // Clear subscribers to avoid memory leaks if desired (keep if you want persistent listeners)
            // OnDissolveCompleted = null; // uncomment if you want one-shot behavior only
            isDissolving = false;
        }
    }
}
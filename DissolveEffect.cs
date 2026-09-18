using System;
using UnityEngine;

/// <summary>
/// ATTACH TO: The GameObject that should dissolve (Enemies, Targets, or the Arrow itself).
/// This is now purely a data container and facilitator. It does NOT implement IArrowTarget.
/// </summary>
// NEW: Changes 3 of 3:
// 1. Removed IArrowTarget interface from class declaration.
// 2. Changed dissolve fields to public so other scripts can access them without reflection.
// 3. Updated TriggerDissolve to delegate execution to DissolveManager.
    // NEW: 1. No longer implements IArrowTarget.
public class DissolveEffect : MonoBehaviour
{
    [Header("Renderers")]
    [Tooltip("The Renderer(s) containing the material to dissolve. If empty, tries to find one on this GameObject or children.")]
    [SerializeField] private Renderer[] targetRenderers;
    public Renderer[] TargetRenderers => targetRenderers;

    [Header("Shader Properties")]
    // NEW: 2. Changed to public fields.
    [Tooltip("The name of the float property that controls the dissolve amount (0 to 1).")]
    public string dissolvePropertyName = "_Dissolve";
    [Tooltip("If your shader uses a float property to toggle the dissolve effect on/off, specify its name here. Leave empty if not used.")]
    public string dissolveTogglePropertyName = "_UseDissolve";
    [Tooltip("The Shader Keyword that must be enabled for the dissolve math to run (e.g. TCP2_DISSOLVE)")]
    public string requiredShaderKeyword = "TCP2_DISSOLVE";

    [Header("Settings")]
    [Tooltip("How long the dissolve animation should take in seconds.")]
    public float dissolveDuration = 1.0f;

    public event Action OnDissolveCompleted;

    private MaterialPropertyBlock propBlock;

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
    /// Call this publicly to force the dissolve to start (e.g., from an enemy health script, or after an arrow sticks delay)
    /// without a completion callback.
    /// </summary>
    public void TriggerDissolve()
    {
        TriggerDissolve(null);
    }
    // NEW: 3. Delegates execution to DissolveManager.

    /// <summary>
    /// Call this to start the dissolve and receive a completion callback when the dissolve finishes.
    /// </summary>
    public void TriggerDissolve(Action onComplete)
    {
        if (gameObject.activeInHierarchy)
        {
            DissolveManager.Instance.StartDissolve(this, onComplete);
        }
    }

    public void InvokeOnDissolveCompleted()
    {
        OnDissolveCompleted?.Invoke();
    }

    /// <summary>
    /// Resets the material property blocks so the mesh is fully visible again.
    /// Essential for objects coming out of a LeanPool.
    /// </summary>
    public void ResetDissolve()
    {
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        foreach (var rend in targetRenderers)
        {
            if (rend == null) continue;

            rend.GetPropertyBlock(propBlock);

            if (!string.IsNullOrEmpty(dissolveTogglePropertyName))
            {
                propBlock.SetFloat(dissolveTogglePropertyName, 0f);
            }

            propBlock.SetFloat(dissolvePropertyName, 0f);
            rend.SetPropertyBlock(propBlock);

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
}

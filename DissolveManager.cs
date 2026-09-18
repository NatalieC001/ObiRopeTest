using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A centralized manager responsible for handling the visual dissolve effect of objects in the scene.
/// This decouples the visual effect coroutines from the objects being destroyed.
/// </summary>
public class DissolveManager : MonoBehaviour
{
    private static DissolveManager instance;
    public static DissolveManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DissolveManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("DissolveManager");
                    instance = go.AddComponent<DissolveManager>();
                }
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    /// <summary>
    /// Starts the dissolve routine for the specified renderers using their configuration.
    /// </summary>
    public void StartDissolve(DissolveEffect config, Action onComplete = null)
    {
        if (config == null || config.TargetRenderers == null || config.TargetRenderers.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(DissolveRoutine(config, onComplete));
    }

    private IEnumerator DissolveRoutine(DissolveEffect config, Action onComplete)
    {
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();

        // Setup initial state and enable keywords
        foreach (var rend in config.TargetRenderers)
        {
            if (rend != null)
            {
                if (!string.IsNullOrEmpty(config.requiredShaderKeyword))
                {
                    foreach (var mat in rend.materials)
                    {
                        mat.EnableKeyword(config.requiredShaderKeyword);
                    }
                }

                rend.GetPropertyBlock(propBlock);
                if (!string.IsNullOrEmpty(config.dissolveTogglePropertyName))
                {
                    propBlock.SetFloat(config.dissolveTogglePropertyName, 1f);
                }
                propBlock.SetFloat(config.dissolvePropertyName, 0f);
                rend.SetPropertyBlock(propBlock);
            }
        }

        float timeElapsed = 0f;
        while (timeElapsed < config.dissolveDuration)
        {
            timeElapsed += Time.deltaTime;
            float dissolveValue = Mathf.Clamp01(timeElapsed / config.dissolveDuration);

            foreach (var rend in config.TargetRenderers)
            {
                if (rend != null)
                {
                    rend.GetPropertyBlock(propBlock);
                    propBlock.SetFloat(config.dissolvePropertyName, dissolveValue);
                    rend.SetPropertyBlock(propBlock);
                }
            }
            yield return null;
        }

        // Finalize state
        foreach (var rend in config.TargetRenderers)
        {
            if (rend != null)
            {
                rend.GetPropertyBlock(propBlock);
                propBlock.SetFloat(config.dissolvePropertyName, 1f);

                if (!string.IsNullOrEmpty(config.dissolveTogglePropertyName))
                {
                    propBlock.SetFloat(config.dissolveTogglePropertyName, 0f);
                }
                rend.SetPropertyBlock(propBlock);
                rend.enabled = false; // Hide the mesh entirely
            }
        }

        config.InvokeOnDissolveCompleted();
        onComplete?.Invoke();
    }
}

using UnityEngine;
using System.Collections;

public enum BreathType
{
    Fire,
    Ice,
    Electric,
    Sticky,
    DarkSpirit
}

public class ElementalBreathController : MonoBehaviour
{
    [Header("Breath Prefabs")]
    public GameObject fireBreathPrefab;
    public GameObject iceBreathPrefab;
    public GameObject electricBreathPrefab;
    public GameObject stickyBreathPrefab;
    public GameObject darkSpiritBreathPrefab;

    [Header("Fire Point")]
    public Transform mouthTransform;

    private BossCreature brain;
    private SpiritZoneManager spiritZoneManager;
    private bool isBreathing = false;

    private void Awake()
    {
        brain = GetComponent<BossCreature>();
        spiritZoneManager = FindFirstObjectByType<SpiritZoneManager>();
    }

    public void FireBreath(BreathType type, Vector3 targetPosition)
    {
        if (isBreathing || mouthTransform == null) return;
        StartCoroutine(BreathRoutine(type, targetPosition));
    }

    private IEnumerator BreathRoutine(BreathType type, Vector3 targetPosition)
    {
        isBreathing = true;

        // 1. Inhale tell (VR readability)
        // Debug.Log($"[ElementalBreathController] Dragon inhaling for {type} breath...");
        yield return new WaitForSeconds(1.5f);

        // 2. Fire projectile/effect
        GameObject prefabToFire = GetPrefabForType(type);
        if (prefabToFire != null)
        {
            if (type == BreathType.DarkSpirit && spiritZoneManager != null)
            {
                // Dark Spirit Cloud places a zone
                spiritZoneManager.TryPlaceZone(targetPosition);
            }
            else
            {
                // Standard breath projectile
                Vector3 dir = (targetPosition - mouthTransform.position).normalized;
                GameObject proj = Instantiate(prefabToFire, mouthTransform.position, Quaternion.LookRotation(dir));
                // Add velocity to proj, etc...
            }
        }

        // 3. Cooldown/End
        yield return new WaitForSeconds(0.5f);
        isBreathing = false;
    }

    private GameObject GetPrefabForType(BreathType type)
    {
        switch (type)
        {
            case BreathType.Fire: return fireBreathPrefab;
            case BreathType.Ice: return iceBreathPrefab;
            case BreathType.Electric: return electricBreathPrefab;
            case BreathType.Sticky: return stickyBreathPrefab;
            case BreathType.DarkSpirit: return darkSpiritBreathPrefab;
            default: return null;
        }
    }
}

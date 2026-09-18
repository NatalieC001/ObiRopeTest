using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(SphereCollider))]
public class GroundHazard : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 15f;
    public float damagePerSecond = 10f;
    public bool applyArrowWeakeningDebuff = true;

    private SphereCollider triggerCollider;
    private HashSet<Collider> objectsInside = new HashSet<Collider>();

    private void Awake()
    {
        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponent<IArrowTarget>() != null)
        {
            objectsInside.Add(other);
            if (applyArrowWeakeningDebuff && other.CompareTag("Player"))
            {
                // In a real project, call Player.ApplyDebuff(WeakenedArrows)
                Debug.Log("[GroundHazard] Player entered Spirit Cloud! Arrows weakened.");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (objectsInside.Contains(other))
        {
            objectsInside.Remove(other);
            if (applyArrowWeakeningDebuff && other.CompareTag("Player"))
            {
                // Call Player.RemoveDebuff(WeakenedArrows)
                Debug.Log("[GroundHazard] Player left Spirit Cloud! Arrows normalized.");
            }
        }
    }

    private void Update()
    {
        // Apply DoT
        foreach (var obj in objectsInside)
        {
            if (obj != null)
            {
                IArrowTarget target = obj.GetComponent<IArrowTarget>();
                if (target != null)
                {
                    target.OnArrowHit(damagePerSecond * Time.deltaTime, obj.transform.position, ElementTypeOB7.Normal);
                }
            }
        }
    }
}

using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HealthCrystal : MonoBehaviour, IArrowTarget
{
    [Header("Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    public bool isDestroyed = false;

    private BossEventBus eventBus;

    private void Awake()
    {
        currentHealth = maxHealth;
        eventBus = FindFirstObjectByType<BossEventBus>();
    }

    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        if (eventBus != null)
        {
            if (currentHealth <= 0f)
            {
                isDestroyed = true;
                eventBus.TriggerCrystalDestroyed(this);
                // Trigger visual destruction, disable mesh, etc.
                Debug.Log($"[HealthCrystal] {gameObject.name} Destroyed!");
            }
            else
            {
                eventBus.TriggerCrystalDamaged(this);
            }
        }
    }
}

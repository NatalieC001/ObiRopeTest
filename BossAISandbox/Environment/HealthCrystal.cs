using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HealthCrystal : MonoBehaviour, IArrowTarget
{
    [Header("Settings")]
    public float maxHealth = 100f;
    private float currentHealth;
    public bool isDestroyed = false;
    public bool IsDestroyed => isDestroyed;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void OnArrowHit(float damage, Vector3 impactPoint, ElementTypeOB7 elementType)
    {
        if (isDestroyed) return;

        currentHealth -= damage;

        if (currentHealth <= 0f)
        {
            isDestroyed = true;
            PixelCrushers.MessageSystem.SendMessage(this, "Brain", "CrystalDestroyed", string.Empty);
            // Trigger visual destruction, disable mesh, etc.
            Debug.Log($"[HealthCrystal] {gameObject.name} Destroyed!");
        }
        else
        {
            PixelCrushers.MessageSystem.SendMessage(this, "Brain", "CrystalDamaged", string.Empty);
        }
    }
}

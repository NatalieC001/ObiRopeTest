using UnityEngine;

/// <summary>
/// A foundation script to identify standard creatures in the game.
/// These are typically the grunts that ride geometric spline shapes and use the Swarm logic,
/// clearly separated from the basic 'MovingTarget' test objects.
/// </summary>
public class StandardCreature : MonoBehaviour
{
    [Header("Creature Stats")]
    public float health = 100f;
    public ElementTypeOB7 elementalType = ElementTypeOB7.Normal;

    private void Start()
    {
        // Initialization logic for standard creatures goes here
        // e.g., setting up animations, audio, etc.
    }

    /// <summary>
    /// Called when the player shoots this creature.
    /// </summary>
    public virtual void TakeDamage(float amount, Vector3 hitPoint)
    {
        health -= amount;

        Debug.Log($"[StandardCreature] {gameObject.name} took {amount} damage. Health remaining: {health}");

        if (health <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        Debug.Log($"[StandardCreature] {gameObject.name} has died.");

        // Ensure we detach from any Dreamteck splines properly upon death
        Dreamteck.Splines.SplineFollower follower = GetComponent<Dreamteck.Splines.SplineFollower>();
        if (follower != null)
        {
            follower.follow = false;
        }

        // Trigger death effects here (dissolve, ragdoll, etc.)
        Destroy(gameObject);
    }
}

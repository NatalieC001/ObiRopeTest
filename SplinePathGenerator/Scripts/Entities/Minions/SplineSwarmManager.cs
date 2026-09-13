using UnityEngine;
using Dreamteck.Splines;
using DG.Tweening;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Manages a group of enemies baked into a Spline Path.
/// Coordinates them to peel off the main geometric track, dive at the player in a swarm,
/// and smoothly return and redistribute if they survive.
/// </summary>
public class SplineSwarmManager : MonoBehaviour
{
    [Header("Swarm Attack Settings")]
    [Tooltip("How often (in seconds) the swarm attempts an attack run.")]
    public float attackInterval = 10f;

    [Tooltip("How many enemies are peeled off for a single attack run.")]
    public int swarmSize = 5;

    [Tooltip("How close (in meters) the swarm gets to the player. Keeps them out of your face in VR.")]
    public float vrComfortDistance = 3.5f;

    private Transform playerTransform;
    private PathSpawnInfo spawnInfo;
    private List<SplineFollower> allEnemies = new List<SplineFollower>();
    private bool isAttacking = false;

    private void Start()
    {
        spawnInfo = GetComponent<PathSpawnInfo>();

        // 1. Find the Player (Assumes XR Origin with "Player" tag)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
        else
        {
            Debug.LogWarning("[SplineSwarmManager] Player not found! Ensure the XR Origin is tagged 'Player'.");
        }

        // 2. Register all baked enemies
        RegisterEnemies();

        // 3. Start the attack cycle
        if (allEnemies.Count > 0 && playerTransform != null)
        {
            StartCoroutine(AttackCycleRoutine());
        }
    }

    private void RegisterEnemies()
    {
        allEnemies.Clear();
        // Assuming this script sits on the root prefab, find all followers in children
        SplineFollower[] followers = GetComponentsInChildren<SplineFollower>();
        foreach (var follower in followers)
        {
            // Only add them if they are alive (not destroyed)
            if (follower != null && follower.gameObject != null)
            {
                allEnemies.Add(follower);
            }
        }
    }

    private IEnumerator AttackCycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);

            // Clean list of dead enemies
            RegisterEnemies();

            if (allEnemies.Count > 0 && !isAttacking)
            {
                yield return StartCoroutine(ExecuteSwarmAttack());
            }
        }
    }

    private IEnumerator ExecuteSwarmAttack()
    {
        isAttacking = true;

        // 1. Select the attackers (randomly pick up to 'swarmSize' alive enemies)
        List<SplineFollower> attackers = new List<SplineFollower>();
        List<SplineFollower> available = new List<SplineFollower>(allEnemies);

        // Memory Dictionary to remember where they belong on the track!
        Dictionary<SplineFollower, double> savedPositions = new Dictionary<SplineFollower, double>();

        int actualSwarmSize = Mathf.Min(swarmSize, available.Count);
        for (int i = 0; i < actualSwarmSize; i++)
        {
            int randomIndex = Random.Range(0, available.Count);
            SplineFollower chosen = available[randomIndex];
            attackers.Add(chosen);
            available.RemoveAt(randomIndex);
        }

        // Dynamically grab the dive speed from the baked settings
        float diveDuration = spawnInfo != null ? spawnInfo.attackDuration : 3f;

        // 2. Launch the attack
        foreach (var attacker in attackers)
        {
            if (attacker == null) continue;

            // Save their exact spot on the track before peeling off
            savedPositions[attacker] = attacker.GetPercent();

            // Turn off Dreamteck following so we can manually control them
            attacker.follow = false;

            // Calculate the comfortable VR target position
            Vector3 directionToPlayer = (playerTransform.position - attacker.transform.position).normalized;
            Vector3 comfortableTargetPos = playerTransform.position - (directionToPlayer * vrComfortDistance);

            // Add a little spherical randomness so they swarm the area rather than clumping in one spot
            Vector3 randomOffset = Random.insideUnitSphere * 1.5f;
            comfortableTargetPos += randomOffset;

            // Ensure they don't dive into the floor (VR Constraint)
            if (comfortableTargetPos.y < 0) comfortableTargetPos.y = 0.5f;

            // Fly to player
            attacker.transform.DOMove(comfortableTargetPos, diveDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetTarget(attacker.gameObject)
                .SetLink(attacker.gameObject);
        }

        // Wait for the attack to happen and linger for a moment
        yield return new WaitForSeconds(diveDuration);

        // 3. Return surviving attackers exactly to the hole they left
        foreach (var attacker in attackers)
        {
            if (attacker == null || attacker.gameObject == null) continue; // Died during attack

            // Retrieve their saved memory of where they belong
            double memoryPercent = savedPositions[attacker];

            // Calculate where that specific spot currently is in the world
            SplineSample sample = attacker.spline.Evaluate(memoryPercent);

            // Fly back directly to their saved spot (no shuffling/ghosting through others!)
            attacker.transform.DOMove(sample.position, 2f)
                .SetEase(Ease.InOutQuad)
                .SetTarget(attacker.gameObject)
                .SetLink(attacker.gameObject) // Safely kill tween on destroy
                .OnComplete(() =>
                {
                    if (attacker != null)
                    {
                        // Snap their internal spline tracker to the correct memory percent before resuming
                        attacker.SetPercent(memoryPercent);
                        attacker.follow = true;
                    }
                });
        }

        // Wait for return flight
        yield return new WaitForSeconds(2f);

        isAttacking = false;
    }

    private void OnDestroy()
    {
        // Cleanup tweens if the manager is destroyed
        foreach (var enemy in allEnemies)
        {
            if (enemy != null)
            {
                DOTween.Kill(enemy.gameObject);
            }
        }
    }
}

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
    private Dictionary<SplineFollower, int> enemyFormationIndices = new Dictionary<SplineFollower, int>();
    private SplineFollower virtualAnchor;

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

        // 2. Register all baked enemies and build the Virtual Anchor
        RegisterEnemiesAndBuildAnchor();

        // 3. Start the attack cycle
        if (allEnemies.Count > 0 && playerTransform != null)
        {
            StartCoroutine(AttackCycleRoutine());
        }
    }

    private void RegisterEnemiesAndBuildAnchor()
    {
        allEnemies.Clear();
        enemyFormationIndices.Clear();

        // Assuming this script sits on the root prefab, find all followers in children
        SplineFollower[] followers = GetComponentsInChildren<SplineFollower>();

        int currentIndex = 0;
        foreach (var follower in followers)
        {
            // Only add them if they are alive (not destroyed) and not our hidden anchor
            if (follower != null && follower.gameObject != null && follower.gameObject.name != "Swarm_Virtual_Anchor")
            {
                allEnemies.Add(follower);
                enemyFormationIndices[follower] = currentIndex;
                currentIndex++;
            }
        }

        // Create the Virtual Anchor if it doesn't exist
        if (virtualAnchor == null && allEnemies.Count > 0)
        {
            GameObject anchorObj = new GameObject("Swarm_Virtual_Anchor");
            anchorObj.transform.SetParent(this.transform);

            virtualAnchor = anchorObj.AddComponent<SplineFollower>();
            virtualAnchor.spline = allEnemies[0].spline;
            virtualAnchor.wrapMode = SplineFollower.Wrap.Loop;

            // Sync its speed with the swarm
            virtualAnchor.followSpeed = spawnInfo != null ? spawnInfo.pathMovementSpeed : 5f;

            // The anchor sits exactly at 0% (the front of the formation)
            virtualAnchor.SetPercent(0);
            virtualAnchor.follow = true;
        }
    }

    private void CleanDeadEnemies()
    {
        // Remove nulls but preserve the array structure so indices don't shift!
        // This is why we don't clear and rebuild the list during combat.
        for (int i = allEnemies.Count - 1; i >= 0; i--)
        {
            if (allEnemies[i] == null || allEnemies[i].gameObject == null)
            {
                allEnemies.RemoveAt(i);
            }
        }
    }

    private IEnumerator AttackCycleRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(attackInterval);

            // Clean list of dead enemies without shifting assigned formation indices
            CleanDeadEnemies();

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

        // 3. Return surviving attackers exactly to the hole they left in the moving formation
        foreach (var attacker in attackers)
        {
            if (attacker == null || attacker.gameObject == null) continue; // Died during attack

            // Calculate where their neighborhood hole is CURRENTLY located
            int myIndex = enemyFormationIndices[attacker];
            int totalOriginalSwarm = enemyFormationIndices.Count;

            // The step spacing originally used when spawning
            double percentStep = 1.0 / totalOriginalSwarm;

            // Add their exact index spacing to the Virtual Anchor's current moving position
            double currentAnchorPercent = virtualAnchor.GetPercent();
            double expectedPercent = currentAnchorPercent + (percentStep * myIndex);

            // Wrap around if over 1.0
            if (expectedPercent > 1.0) expectedPercent -= 1.0;

            // Find where that exact moving spot is in 3D space right now
            SplineSample sample = virtualAnchor.spline.Evaluate(expectedPercent);

            // Fly directly into that gap!
            attacker.transform.DOMove(sample.position, 2f)
                .SetEase(Ease.InOutQuad)
                .SetTarget(attacker.gameObject)
                .SetLink(attacker.gameObject)
                .OnComplete(() =>
                {
                    if (attacker != null)
                    {
                        // Re-sync with the anchor's exact offset so they seamlessly rejoin the circling flow
                        double finalAnchorPercent = virtualAnchor.GetPercent();
                        double finalExpectedPercent = finalAnchorPercent + (percentStep * myIndex);
                        if (finalExpectedPercent > 1.0) finalExpectedPercent -= 1.0;

                        attacker.SetPercent(finalExpectedPercent);
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

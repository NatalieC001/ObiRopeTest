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

        // Dynamically grab the walk time from the baked settings
        float walkTime = spawnInfo != null ? spawnInfo.attackWalkTime : 3f;

        // Start all individual attacker routines
        int activeAttackers = attackers.Count;
        foreach (var attacker in attackers)
        {
            if (attacker != null)
            {
                StartCoroutine(ExecuteIndividualAttackRun(attacker, walkTime, () => { activeAttackers--; }));
            }
            else
            {
                activeAttackers--;
            }
        }

        // Wait until all attackers have completed their round trip or died
        while (activeAttackers > 0)
        {
            yield return null;
        }

        isAttacking = false;
    }

    private IEnumerator ExecuteIndividualAttackRun(SplineFollower attacker, float walkTime, System.Action onComplete)
    {
        // 1. Calculate target destination near player (VR Comfort Distance)
        Vector3 directionToPlayer = (playerTransform.position - attacker.transform.position).normalized;
        Vector3 comfortableTargetPos = playerTransform.position - (directionToPlayer * vrComfortDistance);
        comfortableTargetPos += Random.insideUnitSphere * 1.5f;
        if (comfortableTargetPos.y < 0) comfortableTargetPos.y = 0.5f;

        // 2. Detach from spline and fly towards player
        attacker.follow = false;

        bool reachedPlayer = false;
        attacker.transform.DOMove(comfortableTargetPos, walkTime)
            .SetEase(Ease.InOutSine)
            .SetTarget(attacker.gameObject)
            .SetLink(attacker.gameObject)
            .OnComplete(() => reachedPlayer = true);

        // Wait for arrival (or death)
        while (!reachedPlayer && attacker != null && attacker.gameObject != null)
        {
            yield return null;
        }

        // If it died mid-flight, exit early
        if (attacker == null || attacker.gameObject == null)
        {
            onComplete?.Invoke();
            yield break;
        }

        // 3. Immediately begin return journey (Dynamic Homing)
        // Instead of moving to a static point, we continuously update our target position
        // to chase our exact designated hole in the moving spline formation.
        float elapsedTime = 0f;
        Vector3 startReturnPos = attacker.transform.position;

        int myIndex = enemyFormationIndices[attacker];
        double percentStep = 1.0 / enemyFormationIndices.Count;

        while (elapsedTime < walkTime && attacker != null && attacker.gameObject != null)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / walkTime;

            // Ease out quad for smooth slowdown as it approaches the track
            t = t * (2 - t);

            // Mathematically calculate EXACTLY where our hole is right now on this frame
            double currentAnchorPercent = virtualAnchor.GetPercent();
            double expectedPercent = currentAnchorPercent + (percentStep * myIndex);
            if (expectedPercent > 1.0) expectedPercent -= 1.0;

            SplineSample currentMovingHole = virtualAnchor.spline.Evaluate(expectedPercent);

            // Interpolate position
            attacker.transform.position = Vector3.Lerp(startReturnPos, currentMovingHole.position, t);

            yield return null;
        }

        // 4. Snap and re-engage SplineFollower
        if (attacker != null && attacker.gameObject != null)
        {
            double finalAnchorPercent = virtualAnchor.GetPercent();
            double finalExpectedPercent = finalAnchorPercent + (percentStep * myIndex);
            if (finalExpectedPercent > 1.0) finalExpectedPercent -= 1.0;

            attacker.SetPercent(finalExpectedPercent);
            attacker.follow = true;
        }

        onComplete?.Invoke();
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

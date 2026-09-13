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

    [Tooltip("How long the attack run lasts before the enemies return to the track.")]
    public float attackDuration = 4f;

    [Tooltip("How many enemies are peeled off for a single attack run.")]
    public int swarmSize = 5;

    private Transform playerTransform;
    private List<SplineFollower> allEnemies = new List<SplineFollower>();
    private bool isAttacking = false;

    private void Start()
    {
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

        int actualSwarmSize = Mathf.Min(swarmSize, available.Count);
        for (int i = 0; i < actualSwarmSize; i++)
        {
            int randomIndex = Random.Range(0, available.Count);
            attackers.Add(available[randomIndex]);
            available.RemoveAt(randomIndex);
        }

        // 2. Launch the attack
        foreach (var attacker in attackers)
        {
            if (attacker == null) continue;

            // Turn off Dreamteck following so we can manually control them
            attacker.follow = false;

            // Tween them towards the player using DOTween
            Vector3 attackTarget = playerTransform.position;

            // Add a little randomness so they swarm around the player rather than a single point
            Vector3 randomOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 2f), Random.Range(-1f, 1f));
            attackTarget += randomOffset;

            // Fly to player
            attacker.transform.DOMove(attackTarget, attackDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetTarget(attacker.gameObject); // Bind to GameObject in case it dies
        }

        // Wait for the attack to happen
        yield return new WaitForSeconds(attackDuration);

        // 3. Return surviving attackers to the track
        foreach (var attacker in attackers)
        {
            if (attacker == null || attacker.gameObject == null) continue; // Died during attack

            // Find where they *should* be on the track
            SplineSample sample = attacker.spline.Evaluate(attacker.GetPercent());

            // Fly back to track
            attacker.transform.DOMove(sample.position, 2f)
                .SetEase(Ease.InOutQuad)
                .SetTarget(attacker.gameObject)
                .OnComplete(() =>
                {
                    if (attacker != null)
                    {
                        // Resume Dreamteck following
                        attacker.follow = true;
                    }
                });
        }

        // Wait for return flight
        yield return new WaitForSeconds(2f);

        // 4. Smoothly redistribute survivors across the shape
        RedistributeSurvivors();

        isAttacking = false;
    }

    private void RedistributeSurvivors()
    {
        RegisterEnemies();
        if (allEnemies.Count == 0) return;

        double percentStep = 1.0 / allEnemies.Count;

        for (int i = 0; i < allEnemies.Count; i++)
        {
            SplineFollower survivor = allEnemies[i];
            if (survivor == null) continue;

            double targetPercent = percentStep * i;
            double currentPercent = survivor.GetPercent();

            // We use DOTween DOVirtual to smoothly animate the Dreamteck percent value
            DOVirtual.Float((float)currentPercent, (float)targetPercent, 2f, (p) =>
            {
                if (survivor != null)
                {
                    survivor.SetPercent(p);
                }
            })
            .SetEase(Ease.InOutQuad)
            .SetTarget(survivor.gameObject);
        }
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

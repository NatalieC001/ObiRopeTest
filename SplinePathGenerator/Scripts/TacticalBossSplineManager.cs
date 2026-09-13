using UnityEngine;
using Dreamteck.Splines;
using DG.Tweening;
using System.Collections;

/// <summary>
/// A manager designed for advanced Boss-level creatures.
/// Allows the boss to dynamically hop between pre-authored environmental splines
/// (e.g., spirals around pillars, escape routes) to evade attacks or recharge,
/// while occasionally diving at the player.
/// </summary>
[RequireComponent(typeof(SplineFollower))]
public class TacticalBossSplineManager : MonoBehaviour
{
    [Header("Environmental Splines")]
    [Tooltip("Drag and drop the SplineComputer components from your scene (e.g., PillarWrap_Spline) here.")]
    public SplineComputer[] tacticalEscapeRoutes;

    [Header("Boss Settings")]
    [Tooltip("How long the boss stays on an escape route before attacking.")]
    public float timeOnRoute = 5f;

    [Tooltip("How long it takes to smoothly hop from one route to another.")]
    public float hopDuration = 1.5f;

    private SplineFollower bossFollower;
    private Transform playerTransform;

    private void Awake()
    {
        bossFollower = GetComponent<SplineFollower>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }

        if (tacticalEscapeRoutes.Length > 0)
        {
            StartCoroutine(TacticalRoutine());
        }
        else
        {
            Debug.LogWarning("[TacticalBossSplineManager] No tactical escape routes assigned! Boss cannot use environmental splines.");
        }
    }

    private IEnumerator TacticalRoutine()
    {
        while (true)
        {
            // 1. Ride the current environmental spline
            yield return new WaitForSeconds(timeOnRoute);

            // 2. Perform a tactical hop to a new escape route
            yield return StartCoroutine(HopToRandomRouteRoutine());

            // 3. (Optional) Dive attack logic can go here.
            // For now, it focuses on the tactical evasion around environment pieces.
        }
    }

    /// <summary>
    /// Called externally by the BossCreature brain when it feels threatened and needs to evade immediately.
    /// </summary>
    public void EvadeToEscapeRoute()
    {
        // Stop the ambient routine and force an immediate hop
        StopAllCoroutines();
        StartCoroutine(HopToRandomRouteRoutine());

        // Restart ambient routine after evasion
        StartCoroutine(RestartRoutineAfterHop());
    }

    private IEnumerator RestartRoutineAfterHop()
    {
        yield return new WaitForSeconds(hopDuration);
        StartCoroutine(TacticalRoutine());
    }

    private IEnumerator HopToRandomRouteRoutine()
    {
        if (tacticalEscapeRoutes.Length == 0) yield break;

        // Pick a route we aren't currently on
        SplineComputer targetRoute = tacticalEscapeRoutes[Random.Range(0, tacticalEscapeRoutes.Length)];
        if (targetRoute == bossFollower.spline && tacticalEscapeRoutes.Length > 1)
        {
            // Simple retry to get a different one
            targetRoute = tacticalEscapeRoutes[(System.Array.IndexOf(tacticalEscapeRoutes, targetRoute) + 1) % tacticalEscapeRoutes.Length];
        }

        // Disable following so we can manually tween to the new track
        bossFollower.follow = false;

        // Get the starting point of the new route
        SplineSample startSample = targetRoute.Evaluate(0);

        // Smoothly tween the boss to the new environmental spline
        yield return transform.DOMove(startSample.position, hopDuration)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        // Attach to the new route and resume moving
        bossFollower.spline = targetRoute;
        bossFollower.SetPercent(0);
        bossFollower.follow = true;
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}

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
    [Header("Boss Settings")]
    [Tooltip("How long the boss stays on an escape route before attacking.")]
    public float timeOnRoute = 5f;

    [Tooltip("How long it takes to smoothly hop from one route to another.")]
    public float hopDuration = 1.5f;

    // These are injected at runtime by BossArenaManager to avoid Prefab serialization issues
    private SplineComputer observationSpline;
    private SplineComputer[] tacticalEscapeRoutes;

    private SplineFollower bossFollower;
    private SegmentedDragonManager bodyManager;
    private Transform playerTransform;

    private void Awake()
    {
        bossFollower = GetComponent<SplineFollower>();
        bodyManager = GetComponent<SegmentedDragonManager>();
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
        }
    }

    /// <summary>
    /// Injected by BossCreature/BossArenaManager at spawn.
    /// Sends the boss immediately to the observation deck.
    /// </summary>
    public void InitializeRoutes(SplineComputer observation, SplineComputer[] escapes)
    {
        observationSpline = observation;
        tacticalEscapeRoutes = escapes;

        if (observationSpline != null)
        {
            bossFollower.follow = false;
            transform.position = observationSpline.Evaluate(0).position;
            bossFollower.spline = observationSpline;
            bossFollower.SetPercent(0);
            bossFollower.follow = true;

            if (bodyManager != null)
            {
                bodyManager.SwitchToNewSpline(observationSpline);
            }
        }
    }

    /// <summary>
    /// Begins the Phase 3 evasion sequence. Hops to an escape route and rides it to the end.
    /// </summary>
    public void StartEvasionRoutine()
    {
        if (tacticalEscapeRoutes != null && tacticalEscapeRoutes.Length > 0)
        {
            StopAllCoroutines();
            StartCoroutine(EvasionAndRechargeRoutine());
        }
        else
        {
            Debug.LogWarning("[TacticalBossSplineManager] No tactical escape routes assigned! Boss cannot use environmental splines.");
        }
    }

    /// <summary>
    /// Called externally by the BossCreature brain when it feels threatened during Phase 2.
    /// Acts exactly like the exhaustion evasion, but triggered by burst damage.
    /// </summary>
    public void EvadeToEscapeRoute()
    {
        StartEvasionRoutine();
    }

    private IEnumerator EvasionAndRechargeRoutine()
    {
        // 1. Pick a random escape route
        SplineComputer targetRoute = tacticalEscapeRoutes[Random.Range(0, tacticalEscapeRoutes.Length)];
        if (targetRoute == bossFollower.spline && tacticalEscapeRoutes.Length > 1)
        {
            targetRoute = tacticalEscapeRoutes[(System.Array.IndexOf(tacticalEscapeRoutes, targetRoute) + 1) % tacticalEscapeRoutes.Length];
        }

        // 2. Disable following and tween through the air to the start of the escape route
        bossFollower.follow = false;
        SplineSample startSample = targetRoute.Evaluate(0);

        yield return transform.DOMove(startSample.position, hopDuration)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        // 3. Attach to the escape route and ride it
        bossFollower.spline = targetRoute;
        bossFollower.wrapMode = SplineFollower.Wrap.Default; // Crucial: Don't loop, stop at the end!
        bossFollower.SetPercent(0);
        bossFollower.follow = true;

        if (bodyManager != null)
        {
            bodyManager.SwitchToNewSpline(targetRoute);
        }

        // 4. Wait until the boss reaches the end of the escape route (Percent >= ~0.99)
        while (bossFollower.GetPercent() < 0.99f)
        {
            yield return null;
        }

        // 5. Finished the escape route! Now tween back to the Observation Spline to recharge
        bossFollower.follow = false;
        SplineSample obsStartSample = observationSpline.Evaluate(0);

        yield return transform.DOMove(obsStartSample.position, hopDuration)
            .SetEase(Ease.InOutQuad)
            .WaitForCompletion();

        // 6. Attach to observation spline, set to loop, and notify brain
        bossFollower.spline = observationSpline;
        bossFollower.wrapMode = SplineFollower.Wrap.Loop;
        bossFollower.SetPercent(0);
        bossFollower.follow = true;

        if (bodyManager != null)
        {
            bodyManager.SwitchToNewSpline(observationSpline);
        }

        BossCreature brain = GetComponent<BossCreature>();
        if (brain != null)
        {
            brain.BeginRecharging();
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}

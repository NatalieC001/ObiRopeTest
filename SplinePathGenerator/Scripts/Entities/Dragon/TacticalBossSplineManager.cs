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

    private SplineComputer temporaryBridgeSpline;

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
        if (bossFollower == null) bossFollower = GetComponent<SplineFollower>();
        if (bodyManager == null) bodyManager = GetComponent<SegmentedDragonManager>();
        if (bossFollower == null) return;

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

    private void GenerateBridgeSpline(Vector3 startPos, Vector3 startForward, Vector3 endPos, Vector3 endForward)
    {
        if (temporaryBridgeSpline == null)
        {
            GameObject bridgeObj = new GameObject("TemporaryBridgeSpline");
            temporaryBridgeSpline = bridgeObj.AddComponent<SplineComputer>();
            temporaryBridgeSpline.space = SplineComputer.Space.World;
        }

        // Calculate a nice S-curve or arc using bezier tangents
        float distance = Vector3.Distance(startPos, endPos);
        float tangentLength = distance * 0.4f; // Adjust for smoother/sharper curves

        SplinePoint[] points = new SplinePoint[2];

        // Start point
        points[0] = new SplinePoint();
        points[0].position = startPos;
        points[0].normal = Vector3.up;
        points[0].size = 1f;
        points[0].color = Color.white;
        points[0].tangent = startPos + (startForward * tangentLength);
        points[0].tangent2 = startPos - (startForward * tangentLength);

        // End point
        points[1] = new SplinePoint();
        points[1].position = endPos;
        points[1].normal = Vector3.up;
        points[1].size = 1f;
        points[1].color = Color.white;
        points[1].tangent = endPos - (endForward * tangentLength); // Tangent coming into the point
        points[1].tangent2 = endPos + (endForward * tangentLength);

        temporaryBridgeSpline.SetPoints(points);
        temporaryBridgeSpline.type = Spline.Type.Bezier;
        temporaryBridgeSpline.RebuildImmediate();
    }

    private IEnumerator EvasionAndRechargeRoutine()
    {
        // 1. Pick a random escape route
        SplineComputer targetRoute = tacticalEscapeRoutes[Random.Range(0, tacticalEscapeRoutes.Length)];
        if (targetRoute == bossFollower.spline && tacticalEscapeRoutes.Length > 1)
        {
            targetRoute = tacticalEscapeRoutes[(System.Array.IndexOf(tacticalEscapeRoutes, targetRoute) + 1) % tacticalEscapeRoutes.Length];
        }

        // 2. Generate a bridge from our current position on the observation track to the start of the escape track
        SplineSample currentSample = bossFollower.spline.Evaluate(bossFollower.GetPercent());
        SplineSample targetStartSample = targetRoute.Evaluate(0);

        GenerateBridgeSpline(currentSample.position, currentSample.forward, targetStartSample.position, targetStartSample.forward);

        // 3. Mount the bridge and ride it!
        bossFollower.follow = false;
        bossFollower.spline = temporaryBridgeSpline;
        bossFollower.wrapMode = SplineFollower.Wrap.Default;
        bossFollower.SetPercent(0);
        bossFollower.follow = true;

        if (bodyManager != null) bodyManager.SwitchToNewSpline(temporaryBridgeSpline);

        // Wait until we cross the bridge
        while (bossFollower.GetPercent() < 0.99f)
        {
            yield return null;
        }

        // 4. We arrived at the escape route! Merge seamlessly onto it.
        bossFollower.follow = false;
        bossFollower.spline = targetRoute;
        bossFollower.wrapMode = SplineFollower.Wrap.Default;
        bossFollower.SetPercent(0);
        bossFollower.follow = true;

        if (bodyManager != null) bodyManager.SwitchToNewSpline(targetRoute);

        // Wait until the boss reaches the end of the escape route (Percent >= ~0.99)
        while (bossFollower.GetPercent() < 0.99f)
        {
            yield return null;
        }

        // 5. Finished the escape route! Build another bridge back to the Observation deck
        currentSample = bossFollower.spline.Evaluate(bossFollower.GetPercent());
        targetStartSample = observationSpline.Evaluate(0);

        GenerateBridgeSpline(currentSample.position, currentSample.forward, targetStartSample.position, targetStartSample.forward);

        // Mount the return bridge
        bossFollower.follow = false;
        bossFollower.spline = temporaryBridgeSpline;
        bossFollower.wrapMode = SplineFollower.Wrap.Default;
        bossFollower.SetPercent(0);
        bossFollower.follow = true;

        if (bodyManager != null) bodyManager.SwitchToNewSpline(temporaryBridgeSpline);

        // Wait until we cross the return bridge
        while (bossFollower.GetPercent() < 0.99f)
        {
            yield return null;
        }

        // 6. Attach back to observation spline, set to loop, and notify brain
        bossFollower.follow = false;
        bossFollower.spline = observationSpline;
        bossFollower.wrapMode = SplineFollower.Wrap.Loop;
        bossFollower.SetPercent(0);
        bossFollower.follow = true;

        if (bodyManager != null) bodyManager.SwitchToNewSpline(observationSpline);

        BossCreature brain = GetComponent<BossCreature>();
        if (brain != null)
        {
            brain.BeginRecharging();
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (temporaryBridgeSpline != null)
        {
            Destroy(temporaryBridgeSpline.gameObject);
        }
    }
}

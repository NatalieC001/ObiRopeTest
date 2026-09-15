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

    public void StartFreestylePursuit(Transform target)
    {
        StopAllCoroutines();
        // Detach from spline
        bossFollower.follow = false;

        // Notify body manager that we aren't bound to a specific track anymore
        if (bodyManager != null) bodyManager.SwitchToNewSpline(null);
    }

    public void UpdateFreestylePursuit(Transform target)
    {
        if (target == null) return;

        // Simple pursuit logic in the air
        Vector3 direction = (target.position - transform.position).normalized;
        // Fly towards player
        transform.position += direction * 15f * Time.deltaTime;

        // Rotate smoothly towards player
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
        }
    }

    private IEnumerator FreestyleToSplineRoutine(SplineComputer targetRoute)
    {
        // 1. Detach from current track/air
        bossFollower.follow = false;

        // Tell the body manager we are freestyle. It will just follow history naturally.
        if (bodyManager != null) bodyManager.SwitchToNewSpline(null);

        // 2. Freestyle fly through the air to the start of the target spline
        SplineSample targetStartSample = targetRoute.Evaluate(0);
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        float timer = 0f;
        while (timer < hopDuration)
        {
            timer += Time.deltaTime;
            float t = timer / hopDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            // Lerp position and rotation directly
            transform.position = Vector3.Lerp(startPos, targetStartSample.position, t);
            transform.rotation = Quaternion.Slerp(startRot, targetStartSample.rotation, t);

            yield return null;
        }

        // 3. We arrived! Attach to the target route.
        bossFollower.follow = false;
        bossFollower.spline = targetRoute;
        bossFollower.wrapMode = SplineFollower.Wrap.Default;
        bossFollower.SetPercent(0);
        bossFollower.follow = true;

        if (bodyManager != null) bodyManager.SwitchToNewSpline(targetRoute);
    }

    private IEnumerator EvasionAndRechargeRoutine()
    {
        // 1. Pick a random escape route
        SplineComputer targetRoute = tacticalEscapeRoutes[Random.Range(0, tacticalEscapeRoutes.Length)];
        if (targetRoute == bossFollower.spline && tacticalEscapeRoutes.Length > 1)
        {
            targetRoute = tacticalEscapeRoutes[(System.Array.IndexOf(tacticalEscapeRoutes, targetRoute) + 1) % tacticalEscapeRoutes.Length];
        }

        // 2. Freestyle fly to the escape route
        yield return StartCoroutine(FreestyleToSplineRoutine(targetRoute));

        // 3. Ride the escape route until the end
        while (bossFollower.GetPercent() < 0.99f)
        {
            yield return null;
        }

        // 4. Over-extension: fly linearly off the end of the spline so the tail fully clears the pillar
        bossFollower.follow = false;
        if (bodyManager != null) bodyManager.SwitchToNewSpline(null);

        Vector3 currentForward = transform.forward;
        float flyOutTimer = 0f;
        float flyOutDuration = 2f;
        while (flyOutTimer < flyOutDuration)
        {
            flyOutTimer += Time.deltaTime;
            transform.position += currentForward * 20f * Time.deltaTime;
            yield return null;
        }

        // 5. Freestyle fly back to the Observation deck
        yield return StartCoroutine(FreestyleToSplineRoutine(observationSpline));

        // Loop the observation deck
        bossFollower.wrapMode = SplineFollower.Wrap.Loop;

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

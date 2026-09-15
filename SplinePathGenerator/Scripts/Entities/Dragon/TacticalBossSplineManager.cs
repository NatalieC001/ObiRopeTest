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

    [Header("Freestyle Flight Settings")]
    [Tooltip("How fast the dragon flies forward when off a spline.")]
    public float freestyleFlightSpeed = 20f;

    [Tooltip("How fast the dragon turns/steers when flying in the air.")]
    public float freestyleTurnSpeed = 3f;

    [Tooltip("When pursuing the player, the dragon will stop steering directly at them and swoop past if it gets this close.")]
    public float swoopDistance = 15f;

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

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        // Natural momentum: Always fly forward
        transform.position += transform.forward * freestyleFlightSpeed * Time.deltaTime;

        // If we are far away, steer towards the player. If we are too close,
        // maintain current forward momentum to "swoop" past them instead of stalling or turning on a dime!
        if (distanceToPlayer > swoopDistance)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                // Slow rotation gives a wide, natural turning arc
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * freestyleTurnSpeed * 0.75f);
            }
        }
    }

    private IEnumerator FreestyleToSplineRoutine(SplineComputer targetRoute)
    {
        // 1. Detach from current track/air
        bossFollower.follow = false;

        // Tell the body manager we are freestyle. It will just follow history naturally.
        if (bodyManager != null) bodyManager.SwitchToNewSpline(null);

        SplineSample targetStartSample = targetRoute.Evaluate(0);

        // 2. Freestyle fly through the air towards the start of the target spline.
        // We do not teleport or rigid-lerp. We use constant forward momentum and steer.
        bool hasReachedTarget = false;

        while (!hasReachedTarget)
        {
            // Always fly forward continuously to maintain momentum
            transform.position += transform.forward * freestyleFlightSpeed * Time.deltaTime;

            // Calculate direction to the destination
            Vector3 directionToStart = (targetStartSample.position - transform.position).normalized;

            // Distance to destination
            float distance = Vector3.Distance(transform.position, targetStartSample.position);

            if (distance > 10f)
            {
                // Steer towards the target point
                if (directionToStart != Vector3.zero)
                {
                    Quaternion lookRot = Quaternion.LookRotation(directionToStart);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * freestyleTurnSpeed);
                }
            }
            else
            {
                // We are very close to the start of the spline!
                // Start aligning our rotation to match the direction the spline expects us to be facing
                if (targetStartSample.forward != Vector3.zero)
                {
                    Quaternion alignRot = Quaternion.LookRotation(targetStartSample.forward);
                    transform.rotation = Quaternion.Slerp(transform.rotation, alignRot, Time.deltaTime * freestyleTurnSpeed * 2f);
                }

                // If we are practically touching the start point, we can latch on!
                if (distance < 2f)
                {
                    hasReachedTarget = true;
                }
            }

            yield return null;
        }

        // 3. We arrived! Attach to the target route seamlessly.
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

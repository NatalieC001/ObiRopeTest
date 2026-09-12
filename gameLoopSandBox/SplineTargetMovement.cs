using System.Collections.Generic;
using UnityEngine;
using Dreamteck.Splines;
using DG.Tweening;

/// <summary>
/// Handles the visual duplication and movement of targets along a Dreamteck Spline.
/// Attach this script to a parent GameObject that also has a Dreamteck SplineComputer.
///
/// Integrates both Dreamteck (for path evaluation/following) and DOTween
/// for exciting, entertaining offsets, trailing effects, and easing.
/// </summary>
[RequireComponent(typeof(SplineComputer))]
public class SplineTargetMovement : MonoBehaviour
{
    [Header("Spline Configuration")]
    [Tooltip("The actual target object to duplicate along the spline. If null, the first child will be used.")]
    public GameObject targetPrefab;

    [Tooltip("How many targets should be placed on this spline?")]
    public int targetCount = 1;

    [Header("Movement Style (Entertainment First)")]
    [Tooltip("How should the targets behave on the spline?")]
    public SplineMovementStyle movementStyle = SplineMovementStyle.RigidFollower;

    [Tooltip("How fast the targets move (or how fast the path is completed).")]
    public float speed = 5f;

    [Tooltip("Delay between targets when trailing each other.")]
    public float trailingDelay = 0.5f;

    [Tooltip("DOTween easing for exciting movement patterns.")]
    public Ease movementEase = Ease.Linear;

    [Header("Transform Modifiers")]
    [Tooltip("Should the targets continually spin/rotate in place?")]
    public bool rotateInPlace = false;

    [Tooltip("Speed of the in-place rotation if enabled.")]
    public Vector3 rotationSpeed = new Vector3(0, 90f, 0);

    private SplineComputer spline;
    private List<GameObject> activeTargets = new List<GameObject>();

    public enum SplineMovementStyle
    {
        StaticShape,        // EVENLY SPACED on the spline, not moving.
        RigidFollower,      // EVENLY SPACED, all moving together continuously (Dreamteck native).
        DynamicTrailing,    // ALL FOLLOW the leader from start to finish, with DOTween delay & easing.
        OrbitLeader         // 1 Leader moves on spline, others orbit it using DOTween!
    }

    private void Awake()
    {
        spline = GetComponent<SplineComputer>();

        if (targetPrefab == null)
        {
            if (transform.childCount > 0)
            {
                targetPrefab = transform.GetChild(0).gameObject;
            }
            else
            {
                Debug.LogError("[SplineTargetMovement] No Target Prefab assigned and no child object found to duplicate.");
                return;
            }
        }

        GenerateTargets();

        // Destroy the template target completely so it doesn't get tracked by the Manager.
        Destroy(targetPrefab);
    }

    private void GenerateTargets()
    {
        if (targetCount <= 0 || spline == null) return;

        // Generate the targets
        for (int i = 0; i < targetCount; i++)
        {
            GameObject newTarget = Instantiate(targetPrefab, transform);
            newTarget.name = $"{targetPrefab.name}_SplineClone_{i}";
            newTarget.SetActive(true);
            activeTargets.Add(newTarget);

            // Add continuous DOTween rotation for entertainment if requested
            if (rotateInPlace)
            {
                newTarget.transform.DOLocalRotate(rotationSpeed, 1f, RotateMode.FastBeyond360)
                         .SetRelative(true)
                         .SetEase(Ease.Linear)
                         .SetLoops(-1, LoopType.Incremental);
            }
        }

        // Apply movement logic based on entertainment style
        switch (movementStyle)
        {
            case SplineMovementStyle.StaticShape:
                SetupStaticShape();
                break;
            case SplineMovementStyle.RigidFollower:
                SetupRigidFollower();
                break;
            case SplineMovementStyle.DynamicTrailing:
                SetupDynamicTrailing();
                break;
            case SplineMovementStyle.OrbitLeader:
                SetupOrbitLeader();
                break;
        }
    }

    /// <summary>
    /// Style 1: Evenly spaces targets along the spline. No animation.
    /// Good for shapes (Stars, Circles).
    /// </summary>
    private void SetupStaticShape()
    {
        double percentStep = 1.0 / targetCount;
        for (int i = 0; i < activeTargets.Count; i++)
        {
            double percent = percentStep * i;
            SplineSample sample = spline.Evaluate(percent);
            activeTargets[i].transform.position = sample.position;
            activeTargets[i].transform.rotation = sample.rotation;
        }
    }

    /// <summary>
    /// Style 2: Standard Dreamteck looping followers evenly spaced out.
    /// </summary>
    private void SetupRigidFollower()
    {
        double percentStep = 1.0 / targetCount;
        for (int i = 0; i < activeTargets.Count; i++)
        {
            double startingPercent = percentStep * i;
            SplineFollower follower = activeTargets[i].AddComponent<SplineFollower>();
            follower.spline = spline;
            follower.wrapMode = SplineFollower.Wrap.Loop;
            follower.followSpeed = speed;
            follower.SetPercent(startingPercent);
        }
    }

    /// <summary>
    /// Style 3: Targets snake and follow each other using DOTween DOVirtual to evaluate the spline.
    /// Includes easing and delays!
    /// </summary>
    private void SetupDynamicTrailing()
    {
        float duration = 10f / speed; // Arbitrary conversion of speed to duration

        for (int i = 0; i < activeTargets.Count; i++)
        {
            GameObject target = activeTargets[i];
            float delay = i * trailingDelay;

            // We use DOTween DOVirtual to animate a float from 0 to 1, then evaluate the spline manually
            // This allows us to use DOTween's incredible Easing library on Dreamteck paths!
            DOVirtual.Float(0f, 1f, duration, (percent) =>
            {
                if (target != null && spline != null)
                {
                    SplineSample sample = spline.Evaluate(percent);
                    target.transform.position = sample.position;
                    target.transform.rotation = sample.rotation;
                }
            })
            .SetTarget(target) // Explicitly bind tween to target to prevent memory leaks
            .SetDelay(delay)
            .SetEase(movementEase)
            .SetLoops(-1, LoopType.Restart); // Loop forever, snaking through the path
        }
    }

    /// <summary>
    /// Style 4: A single leader travels the spline using Dreamteck.
    /// The rest orbit around the leader using DOTween. Highly dynamic and entertaining!
    /// </summary>
    private void SetupOrbitLeader()
    {
        if (activeTargets.Count == 0) return;

        // Leader gets the standard follower
        GameObject leader = activeTargets[0];
        SplineFollower follower = leader.AddComponent<SplineFollower>();
        follower.spline = spline;
        follower.wrapMode = SplineFollower.Wrap.Loop;
        follower.followSpeed = speed;

        // The rest become children of the leader and orbit using DOTween!
        float angleStep = 360f / (activeTargets.Count - 1);
        float orbitRadius = 1.5f; // Hardcoded for fun, could be exposed

        // Create an invisible pivot object that follows the spline with the leader
        // This prevents orbiters from being destroyed if the leader is destroyed early.
        GameObject orbitPivot = new GameObject($"{targetPrefab.name}_OrbitPivot");
        orbitPivot.transform.SetParent(transform);
        SplineFollower pivotFollower = orbitPivot.AddComponent<SplineFollower>();
        pivotFollower.spline = spline;
        pivotFollower.wrapMode = SplineFollower.Wrap.Loop;
        pivotFollower.followSpeed = speed;

        for (int i = 1; i < activeTargets.Count; i++)
        {
            GameObject orbiter = activeTargets[i];
            orbiter.transform.SetParent(orbitPivot.transform);

            // Set initial orbit position
            float startAngle = angleStep * (i - 1);
            Vector3 startPos = new Vector3(Mathf.Cos(startAngle * Mathf.Deg2Rad), Mathf.Sin(startAngle * Mathf.Deg2Rad), 0) * orbitRadius;
            orbiter.transform.localPosition = startPos;

            // Animate orbiting around the pivot's Z axis
            orbiter.transform.DOLocalRotate(new Vector3(0, 0, 360), 3f, RotateMode.FastBeyond360)
                           .SetRelative(true)
                           .SetEase(Ease.Linear)
                           .SetLoops(-1, LoopType.Incremental);
        }
    }

    private void OnDestroy()
    {
        foreach (var target in activeTargets)
        {
            if (target != null)
            {
                target.transform.DOKill();
                DOTween.Kill(target); // Kill any active Tweens on these objects
            }
        }
    }
}

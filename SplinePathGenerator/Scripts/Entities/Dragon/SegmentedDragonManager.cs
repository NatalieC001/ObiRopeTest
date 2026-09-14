using UnityEngine;
using Dreamteck.Splines;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// Controls the multi-part Asian Dragon boss.
/// Handles instantiating the segments along the spline, maintaining their spacing,
/// and shrinking/closing gaps smoothly when segments are destroyed.
/// </summary>
public class SegmentedDragonManager : MonoBehaviour
{
    [Header("Dragon Anatomy Prefabs")]
    public GameObject headPrefab;
    [Tooltip("Optional: Inserted between Head and Body.")]
    public GameObject frontLegsPrefab;
    public GameObject bodyPrefab;
    [Tooltip("Optional: Inserted between Body and Tail.")]
    public GameObject backLegsPrefab;
    public GameObject tailPrefab;

    [Header("Structure")]
    [Tooltip("How many plain body segments to insert between the legs.")]
    public int numberOfBodySegments = 8;

    [Tooltip("The physical distance between each segment along the spline.")]
    public float segmentSpacing = 2f;

    [Tooltip("The total combined power of the boss based on remaining segments.")]
    public float totalBossPower { get; private set; }

    // List tracking all live segments. Head is index 0.
    private List<DragonSegment> activeSegments = new List<DragonSegment>();
    private SplineComputer bossSpline;

    // The Dreamteck follower component for the Head. The rest of the body follows this.
    private SplineFollower headFollower;
    private BossCreature bossBrain;

    public void InitializeDragon(SplineComputer track)
    {
        bossSpline = track;
        totalBossPower = 0f;
        activeSegments.Clear();
        bossBrain = GetComponent<BossCreature>();

        int currentIndex = 0;

        // 1. Spawn Head
        SpawnSegment(headPrefab, currentIndex, track);
        headFollower = activeSegments[0].Follower;
        currentIndex++;

        // 2. Spawn Front Legs (if assigned)
        if (frontLegsPrefab != null)
        {
            SpawnSegment(frontLegsPrefab, currentIndex, track);
            currentIndex++;
        }

        // 3. Spawn Body
        for (int i = 0; i < numberOfBodySegments; i++)
        {
            SpawnSegment(bodyPrefab, currentIndex, track);
            currentIndex++;
        }

        // 4. Spawn Back Legs (if assigned)
        if (backLegsPrefab != null)
        {
            SpawnSegment(backLegsPrefab, currentIndex, track);
            currentIndex++;
        }

        // 5. Spawn Tail
        SpawnSegment(tailPrefab, currentIndex, track);

        // 6. Force initial positioning
        UpdateSegmentSpacing(false);
    }

    private void SpawnSegment(GameObject prefab, int index, SplineComputer track)
    {
        if (prefab == null) return;

        GameObject segmentObj = Instantiate(prefab, transform);
        segmentObj.name = $"DragonSegment_{index}";

        SplineFollower follower = segmentObj.GetComponent<SplineFollower>();
        if (follower == null) follower = segmentObj.AddComponent<SplineFollower>();

        follower.spline = track;
        follower.wrapMode = SplineFollower.Wrap.Loop;

        DragonSegment segment = segmentObj.GetComponent<DragonSegment>();
        if (segment == null) segment = segmentObj.AddComponent<DragonSegment>();

        segment.Initialize(this, bossBrain, index);
        activeSegments.Add(segment);

        totalBossPower += segment.powerContribution;
    }

    /// <summary>
    /// Updates the target spline for all active segments (e.g. when the boss evades to a new track).
    /// </summary>
    public void SwitchToNewSpline(SplineComputer newTrack)
    {
        bossSpline = newTrack;
        foreach (var segment in activeSegments)
        {
            if (segment != null && segment.Follower != null)
            {
                segment.Follower.spline = newTrack;
            }
        }
    }

    // Controls whether the Update loop forces rigid spacing. Disabled briefly when closing a gap.
    private bool isClosingGap = false;
    private float gapCloseTimer = 0f;
    private float gapCloseDuration = 1f;

    // A dictionary to store the start distance offsets of each segment when a gap close begins
    // Offset is tracked rather than absolute position, so they can dynamically follow the moving head
    private Dictionary<DragonSegment, float> gapStartOffsets = new Dictionary<DragonSegment, float>();

    private void Update()
    {
        if (activeSegments.Count == 0 || headFollower == null) return;

        if (isClosingGap)
        {
            gapCloseTimer += Time.deltaTime;
            float t = gapCloseTimer / gapCloseDuration;

            // Smoothly ease the interpolation
            t = Mathf.SmoothStep(0f, 1f, t);

            if (t >= 1f)
            {
                t = 1f;
                isClosingGap = false; // Done closing gap, resume rigid follow
            }

            UpdateSegmentSpacing(true, t);
        }
        else
        {
            // Keep segments trailing rigidly behind the head based on distance
            UpdateSegmentSpacing(false, 1f);
        }
    }

    /// <summary>
    /// Forces all segments behind the head to fall into line.
    /// If animateSmoothly is true, it blends between their stored start offset and their new target offset.
    /// </summary>
    private void UpdateSegmentSpacing(bool animateSmoothly, float lerpT)
    {
        if (activeSegments.Count == 0 || headFollower == null || bossSpline == null) return;

        double totalSplineLength = bossSpline.CalculateLength();
        double headDistance = totalSplineLength * headFollower.GetPercent();

        for (int i = 1; i < activeSegments.Count; i++)
        {
            DragonSegment segment = activeSegments[i];

            // The new required spacing offset behind the head
            float targetOffset = segmentSpacing * i;

            float currentOffsetToApply = targetOffset;

            // If we are animating, smoothly transition from their old offset to their new offset
            if (animateSmoothly && gapStartOffsets.ContainsKey(segment))
            {
                float startOffset = gapStartOffsets[segment];
                currentOffsetToApply = Mathf.Lerp(startOffset, targetOffset, lerpT);
            }

            // Apply the offset behind the actively moving head
            double currentDistance = headDistance - currentOffsetToApply;

            // Handle looping if the target goes below 0 length
            if (currentDistance < 0)
            {
                currentDistance += totalSplineLength;
            }

            segment.Follower.SetPercent(currentDistance / totalSplineLength);
        }
    }

    /// <summary>
    /// Called by a DragonSegment when it is destroyed.
    /// </summary>
    public void OnSegmentDestroyed(DragonSegment destroyedSegment)
    {
        totalBossPower -= destroyedSegment.powerContribution;

        bool wasHead = (activeSegments.IndexOf(destroyedSegment) == 0);
        activeSegments.Remove(destroyedSegment);

        Debug.Log($"<color=magenta>[SegmentedDragonManager] A segment fell! Boss power reduced to {totalBossPower}. Closing gap!</color>");

        if (activeSegments.Count == 0)
        {
            // The whole dragon is dead!
            headFollower = null;
            return;
        }

        if (wasHead)
        {
            // Promote the next body segment in line to be the new lead tracker
            headFollower = activeSegments[0].Follower;
        }

        // Re-index remaining segments so they know their new place in line
        // AND store their current physical offset from the head to use as the starting point for the tween
        gapStartOffsets.Clear();

        double totalSplineLength = bossSpline.CalculateLength();
        double headDist = totalSplineLength * headFollower.GetPercent();

        for (int i = 0; i < activeSegments.Count; i++)
        {
            activeSegments[i].SegmentIndex = i;

            // Calculate how far back this segment currently is from the head
            double segDist = totalSplineLength * activeSegments[i].Follower.GetPercent();

            // Handle looping seam calculations
            double offsetDist = headDist - segDist;
            if (offsetDist < -totalSplineLength / 2) // If segment is near 100% and head is near 0%
            {
                offsetDist += totalSplineLength;
            }
            else if (offsetDist > totalSplineLength / 2)
            {
                offsetDist -= totalSplineLength;
            }

            gapStartOffsets[activeSegments[i]] = (float)offsetDist;
        }

        // Pause standard rigidly-spaced updates and begin the smooth gap close
        isClosingGap = true;
        gapCloseTimer = 0f;
    }

    /// <summary>
    /// Called by BossCreature when the overall health reaches 0.
    /// Commands all remaining permanent pieces (Head, Legs, Tail) to die.
    /// </summary>
    public void TriggerTotalDeath()
    {
        Debug.Log("<color=red>[SegmentedDragonManager] The entire dragon is collapsing!</color>");
        foreach (var segment in activeSegments)
        {
            if (segment != null)
            {
                segment.TriggerTotalDeath();
            }
        }
        activeSegments.Clear();
    }
}

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
    public GameObject bodyPrefab;
    public GameObject tailPrefab;

    [Header("Structure")]
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

        // 1. Spawn Head
        SpawnSegment(headPrefab, 0, track);
        headFollower = activeSegments[0].Follower;

        // 2. Spawn Body
        for (int i = 1; i <= numberOfBodySegments; i++)
        {
            SpawnSegment(bodyPrefab, i, track);
        }

        // 3. Spawn Tail
        SpawnSegment(tailPrefab, numberOfBodySegments + 1, track);

        // 4. Force initial positioning
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

    private void Update()
    {
        if (activeSegments.Count == 0 || headFollower == null) return;

        if (!isClosingGap)
        {
            // Keep segments trailing rigidly behind the head based on distance
            UpdateSegmentSpacing(false);
        }
    }

    /// <summary>
    /// Forces all segments behind the head to fall into line.
    /// Can be instant (every frame) or smoothly tweened (when closing a gap).
    /// </summary>
    private void UpdateSegmentSpacing(bool animateSmoothly)
    {
        if (activeSegments.Count == 0 || headFollower == null || bossSpline == null) return;

        // Calculate absolute distance along the spline length
        double totalSplineLength = bossSpline.CalculateLength();
        double headDistance = totalSplineLength * headFollower.GetPercent();

        // If we are animating smoothly, we need to know when the longest animation finishes
        float longestTweenDuration = 0f;

        for (int i = 1; i < activeSegments.Count; i++)
        {
            DragonSegment segment = activeSegments[i];

            // Calculate where this segment should be (spacing * its position in line)
            double targetDistance = headDistance - (segmentSpacing * i);

            // Handle looping if the target goes below 0 length
            if (targetDistance < 0)
            {
                targetDistance += totalSplineLength;
            }

            if (animateSmoothly)
            {
                float tweenDuration = 1f;
                longestTweenDuration = tweenDuration;

                // Smoothly slide them forward to close a gap using DOVirtual
                double startDist = totalSplineLength * segment.Follower.GetPercent();
                DOVirtual.Float((float)startDist, (float)targetDistance, tweenDuration, (d) =>
                {
                    if (segment != null && segment.Follower != null)
                    {
                        // Convert distance back to percentage for Dreamteck 3.0.6 compatibility
                        segment.Follower.SetPercent(d / totalSplineLength);
                    }
                })
                .SetEase(Ease.InOutQuad)
                .SetLink(segment.gameObject); // Safely kill tween if segment is destroyed
            }
            else
            {
                // Instant update for frame-by-frame slithering
                segment.Follower.SetPercent(targetDistance / totalSplineLength);
            }
        }

        // Resume rigid updates after the gap finishes closing
        if (animateSmoothly)
        {
            DOVirtual.DelayedCall(longestTweenDuration, () =>
            {
                isClosingGap = false;
            });
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
        for (int i = 0; i < activeSegments.Count; i++)
        {
            activeSegments[i].SegmentIndex = i;
        }

        // Pause standard rigidly-spaced updates
        isClosingGap = true;

        // Animate the remaining segments forward to close the gap left by the missing piece
        UpdateSegmentSpacing(true);
    }
}

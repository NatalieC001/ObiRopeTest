using System;
using System.Collections.Generic;
using UnityEngine;
using Dreamteck.Splines;
using DG.Tweening;

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

    // --- TETHER STATE ---
    private RopeArrow currentTether = null;
    public bool IsTethered { get; private set; } = false;
    public Transform TetherAnchorTransform { get; private set; } = null;
    public float TetherMaxLength { get; private set; } = 0f;

    // --- BREADCRUMB SLITHERING SYSTEM ---
    private struct PositionData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float distanceTraveled; // Total distance head had traveled when recording this
    }
    private List<PositionData> positionHistory = new List<PositionData>();
    private float headTotalDistance = 0f;

    // Spacing animation state
    private bool isClosingGap = false;
    private float gapCloseTimer = 0f;
    private float gapCloseDuration = 0.5f;

    // Start / current distances used for animation and the final targets (physical spacing)
    private Dictionary<DragonSegment, float> currentSpacings = new Dictionary<DragonSegment, float>();
    private Dictionary<DragonSegment, float> targetSpacings = new Dictionary<DragonSegment, float>();

    /// <summary>
    /// Event invoked whenever the number of active segments changes (passes new remaining count).
    /// Useful for UI feedback (how many segments left).
    /// </summary>
    public event Action<int> OnSegmentCountChanged;

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

        if (headFollower != null)
        {
            headFollower.enabled = false;
            headFollower.follow = false;
        }

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

        // Disable followers for body parts (manager controls transforms)
        for (int i = 1; i < activeSegments.Count; i++)
        {
            var f = activeSegments[i].Follower;
            if (f != null)
            {
                f.enabled = false;
            }
        }

        RopeArrowManagerObi7.OnRopeBroken += OnRopeBroken;

        // Initialize breadcrumb history and initial spacings
        positionHistory.Clear();
        float totalLength = activeSegments.Count * segmentSpacing * 2f;
        int samples = Mathf.CeilToInt(totalLength / 0.1f) + 1;

        if (bossSpline != null)
        {
            SplineFollower rootFollower = GetComponent<SplineFollower>();
            double startPercent = rootFollower != null ? rootFollower.GetPercent() : 0.0;
            float splineLength = bossSpline.CalculateLength();

            for (int i = 0; i < samples; i++)
            {
                float distBack = i * 0.1f;
                double percent = startPercent - (distBack / splineLength);
                if (bossSpline.isClosed)
                {
                    while (percent < 0.0) percent += 1.0;
                    while (percent > 1.0) percent -= 1.0;
                }
                else
                {
                    if (percent < 0.0) percent = 0.0;
                    if (percent > 1.0) percent = 1.0;
                }

                percent = System.Math.Clamp(percent, 0.0, 1.0);
                SplineSample sample = bossSpline.Evaluate(percent);

                positionHistory.Add(new PositionData
                {
                    position = sample.position,
                    rotation = sample.rotation,
                    distanceTraveled = -distBack
                });
            }
        }
        else
        {
            positionHistory.Add(new PositionData
            {
                position = transform.position,
                rotation = transform.rotation,
                distanceTraveled = 0f
            });
        }

        // Compute initial target spacings using physical lengths and set current==target so there's no animation on spawn
        ComputeTargetSpacingsFromPhysicalSizes();
        currentSpacings.Clear();
        foreach (var kv in targetSpacings) currentSpacings[kv.Key] = kv.Value;

        // Place segments at their targets immediately
        UpdateSegmentSpacing(false, 1f);

        // Notify listeners of initial count
        OnSegmentCountChanged?.Invoke(activeSegments.Count);
    }

    private void OnDestroy()
    {
        RopeArrowManagerObi7.OnRopeBroken -= OnRopeBroken;
    }

    private void SpawnSegment(GameObject prefab, int index, SplineComputer track)
    {
        if (prefab == null) return;

        GameObject segmentObj = Instantiate(prefab, transform);
        segmentObj.name = $"DragonSegment_{index}";

        SplineFollower follower = segmentObj.GetComponent<SplineFollower>();
        if (follower == null) follower = segmentObj.AddComponent<SplineFollower>();

        if (track != null)
        {
            follower.spline = track;
        }

        DragonSegment segment = segmentObj.GetComponent<DragonSegment>();
        if (segment == null) segment = segmentObj.AddComponent<DragonSegment>();

        segment.SetFollower(follower);

        bool destructible = prefab == bodyPrefab;
        segment.isDestructiblePart = destructible;

        segment.Initialize(this, bossBrain, index);
        activeSegments.Add(segment);

        totalBossPower += segment.powerContribution;
    }

    public void HandleRopeAttached(DragonSegment segment, RopeArrow rope)
    {
        if (rope == null || segment == null) return;

        currentTether = rope;
        IsTethered = true;
        TetherAnchorTransform = rope.GetTailTransform();
        if (TetherAnchorTransform != null)
        {
            TetherMaxLength = Vector3.Distance(transform.position, TetherAnchorTransform.position);
        }
        else
        {
            TetherMaxLength = 0f;
        }

        Debug.Log($"<color=cyan>[SegmentedDragonManager] Tether attached to segment {segment.SegmentIndex}. MaxLength: {TetherMaxLength:F2}</color>");
    }

    public void ReleaseTetherFromSegment(DragonSegment segment, RopeArrow rope)
    {
        if (rope == null) return;
        if (currentTether == rope)
        {
            Debug.Log("<color=cyan>[SegmentedDragonManager] Tether released.</color>");
            currentTether = null;
            IsTethered = false;
            TetherAnchorTransform = null;
            TetherMaxLength = 0f;
        }
    }

    private void OnRopeBroken(RopeArrow rope)
    {
        if (rope == null) return;
        if (currentTether == rope)
        {
            ReleaseTetherFromSegment(null, rope);
        }
    }

    public void SwitchToNewSpline(SplineComputer newTrack)
    {
        bossSpline = newTrack;

        // Also update followers' spline reference for head/body safety
        foreach (var seg in activeSegments)
        {
            if (seg != null && seg.Follower != null)
            {
                seg.Follower.spline = newTrack;
            }
        }
    }

    private void LateUpdate()
    {
        if (activeSegments.Count == 0) return;

        // Update breadcrumb history (root boss object)
        Vector3 currentHeadPos = transform.position;
        if (positionHistory.Count == 0) return;
        PositionData lastData = positionHistory[0];

        float distMovedSinceLastFrame = Vector3.Distance(currentHeadPos, lastData.position);

        if (distMovedSinceLastFrame > 0.05f)
        {
            headTotalDistance += distMovedSinceLastFrame;

            positionHistory.Insert(0, new PositionData
            {
                position = currentHeadPos,
                rotation = transform.rotation,
                distanceTraveled = headTotalDistance
            });

            float maxNeededHistoryDistance = segmentSpacing * activeSegments.Count * 2f;
            if (headTotalDistance - positionHistory[positionHistory.Count - 1].distanceTraveled > maxNeededHistoryDistance)
            {
                positionHistory.RemoveAt(positionHistory.Count - 1);
            }
        }

        // Gap closing animation
        if (isClosingGap)
        {
            gapCloseTimer += Time.deltaTime;
            float t = gapCloseTimer / gapCloseDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            if (t >= 1f)
            {
                t = 1f;
                isClosingGap = false;
            }

            UpdateSegmentSpacing(true, t);
        }
        else
        {
            UpdateSegmentSpacing(false, 1f);
        }
    }

    private void UpdateSegmentSpacing(bool animateSmoothly, float lerpT)
    {
        // Move all segments along the history buffer.
        for (int i = 0; i < activeSegments.Count; i++)
        {
            DragonSegment segment = activeSegments[i];
            if (segment == null) continue;

            // Determine desired distance behind head
            float startDist = segmentSpacing * i;
            float endDist = segmentSpacing * i;

            if (targetSpacings.TryGetValue(segment, out float target)) endDist = target;
            if (currentSpacings.TryGetValue(segment, out float current)) startDist = current;

            float requiredDistanceBehindHead;
            if (animateSmoothly)
            {
                requiredDistanceBehindHead = Mathf.Lerp(current, endDist, lerpT);
            }
            else
            {
                requiredDistanceBehindHead = endDist;
            }

            float targetDistanceInHistory = headTotalDistance - requiredDistanceBehindHead;

            // Find the two breadcrumbs this distance falls between
            for (int j = 0; j < positionHistory.Count - 1; j++)
            {
                PositionData newer = positionHistory[j];
                PositionData older = positionHistory[j + 1];

                if (targetDistanceInHistory <= newer.distanceTraveled && targetDistanceInHistory >= older.distanceTraveled)
                {
                    float range = newer.distanceTraveled - older.distanceTraveled;
                    float t = (newer.distanceTraveled - targetDistanceInHistory) / range;
                    segment.transform.position = Vector3.Lerp(newer.position, older.position, t);
                    segment.transform.rotation = Quaternion.Slerp(newer.rotation, older.rotation, t);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Called by DragonSegment after its dissolve completes.
    /// This is the single, unit-testable hook for segment removal and spacing logic.
    /// </summary>
    public void NotifySegmentDissolved(DragonSegment dissolvedSegment)
    {
        if (dissolvedSegment == null) return;

        // 1) Capture current real distances behind head for remaining segments BEFORE removal
        Dictionary<DragonSegment, float> actualDistances = new Dictionary<DragonSegment, float>(activeSegments.Count);
        foreach (var seg in activeSegments)
        {
            if (seg == null || seg == dissolvedSegment) continue;
            actualDistances[seg] = GetCurrentDistanceBehindHead(seg);
        }

        // 2) Update boss power and remove the dissolved segment
        totalBossPower -= dissolvedSegment.powerContribution;
        bool removed = activeSegments.Remove(dissolvedSegment);
        if (!removed)
        {
            Debug.LogWarning("[SegmentedDragonManager] NotifySegmentDissolved called for a segment not in activeSegments.");
        }
        else
        {
            Debug.Log($"<color=magenta>[SegmentedDragonManager] A segment dissolved! Boss power reduced to {totalBossPower}. Closing gap!</color>");
        }

        // Notify UI / callers about remaining segments
        OnSegmentCountChanged?.Invoke(activeSegments.Count);

        // Tether handling
        if (IsTethered && currentTether != null)
        {
            Transform tail = currentTether.GetTailTransform();
            if (tail != null && (tail.IsChildOf(dissolvedSegment.transform) || tail == dissolvedSegment.transform))
            {
                ReleaseTetherFromSegment(dissolvedSegment, currentTether);
            }
        }

        if (activeSegments.Count == 0)
        {
            // The whole dragon is dead!
            return;
        }

        // 3) Compute target spacings from physical sizes for the new list (head=0)
        ComputeTargetSpacingsFromPhysicalSizes();

        // 4) Set currentSpacings to captured actual distances (fall back to target if missing)
        currentSpacings.Clear();
        foreach (var seg in activeSegments)
        {
            if (seg == null) continue;
            if (actualDistances.TryGetValue(seg, out float captured))
            {
                currentSpacings[seg] = captured;
            }
            else if (targetSpacings.TryGetValue(seg, out float t))
            {
                currentSpacings[seg] = t;
            }
            else
            {
                currentSpacings[seg] = seg.SegmentIndex * segmentSpacing;
            }
        }

        // Start the gap closing animation
        isClosingGap = true;
        gapCloseTimer = 0f;
    }

    /// <summary>
    /// Computes targetSpacings for activeSegments using each segment's physical length so gaps are exact.
    /// The head is placed at 0; subsequent segments are placed behind head cumulatively:
    /// prevHalf + gap + currHalf.
    /// </summary>
    private void ComputeTargetSpacingsFromPhysicalSizes()
    {
        targetSpacings.Clear();
        float cumulative = 0f;

        for (int i = 0; i < activeSegments.Count; i++)
        {
            var seg = activeSegments[i];
            if (seg == null) continue;

            // For head (i==0), distance is 0
            if (i == 0)
            {
                targetSpacings[seg] = 0f;
                cumulative = GetSegmentPhysicalLength(seg) * 0.5f; // head's half-length
                continue;
            }

            float prevHalf = cumulative; // this currently holds previous half-length when looped
            float currLength = GetSegmentPhysicalLength(seg);
            float currHalf = currLength * 0.5f;

            // Desired distance behind head = previous cumulative (head half + ... ) + segmentSpacing + currHalf
            float distanceBehindHead = cumulative + segmentSpacing + currHalf;

            targetSpacings[seg] = distanceBehindHead;

            // Update cumulative to be previous cumulative + segmentSpacing + currFull (so next prevHalf becomes half of next)
            cumulative = distanceBehindHead + currHalf;
        }

        // Also update logical SegmentIndex according to current ordering
        for (int i = 0; i < activeSegments.Count; i++)
        {
            if (activeSegments[i] != null) activeSegments[i].SegmentIndex = i;
        }
    }

    // Helper: estimate segment physical length along its forward axis using renderer bounds projection.
    private float GetSegmentPhysicalLength(DragonSegment seg)
    {
        if (seg == null) return segmentSpacing; // fallback
        Renderer rend = seg.GetComponentInChildren<Renderer>();
        if (rend == null)
        {
            return segmentSpacing; // fallback default
        }

        Vector3 size = rend.bounds.size; // world-space extents
        Vector3 forward = seg.transform.forward.normalized;
        // Project bounds size onto forward axis (approximation)
        float length = Mathf.Abs(Vector3.Dot(size, forward));
        if (length < 0.1f) length = segmentSpacing; // avoid degenerate cases
        return length;
    }

    // Helper: find nearest breadcrumb and compute distance behind head
    private float GetCurrentDistanceBehindHead(DragonSegment seg)
    {
        if (seg == null) return 0f;
        if (positionHistory == null || positionHistory.Count == 0)
        {
            return seg.SegmentIndex * segmentSpacing;
        }

        float bestDist = float.MaxValue;
        float bestEntryDistanceTraveled = positionHistory[0].distanceTraveled;
        Vector3 segPos = seg.transform.position;
        for (int i = 0; i < positionHistory.Count; i++)
        {
            float d = Vector3.SqrMagnitude(positionHistory[i].position - segPos);
            if (d < bestDist)
            {
                bestDist = d;
                bestEntryDistanceTraveled = positionHistory[i].distanceTraveled;
            }
        }

        float distBehindHead = headTotalDistance - bestEntryDistanceTraveled;
        if (distBehindHead < 0f) distBehindHead = 0f;
        return distBehindHead;
    }

    [Obsolete("Use NotifySegmentDissolved(DragonSegment) instead.")]
    public void OnSegmentDestroyed(DragonSegment destroyedSegment)
    {
        NotifySegmentDissolved(destroyedSegment);
    }

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

        currentTether = null;
        IsTethered = false;
        TetherAnchorTransform = null;
        TetherMaxLength = 0f;

        // Notify UI/listeners that there are zero segments left
        OnSegmentCountChanged?.Invoke(0);
    }

    /// <summary>
    /// Query how many live segments remain (convenience for UI).
    /// </summary>
    public int GetRemainingSegmentCount()
    {
        return activeSegments.Count;
    }
}
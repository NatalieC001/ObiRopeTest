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

    // Tracking list for body segments to monitor win condition
    private List<DragonSegment> destructibleSegments = new List<DragonSegment>();

    // List tracking all live segments. Head is index 0.
    private List<DragonSegment> activeSegments = new List<DragonSegment>();
    private SplineComputer bossSpline;

    // The Dreamteck follower component for the Head. The rest of the body follows this.
    private SplineFollower headFollower;
    private BossCreature bossBrain;

    // --- BREADCRUMB SLITHERING SYSTEM ---
    private struct PositionData
    {
        public Vector3 position;
        public Quaternion rotation;
        public float distanceTraveled; // Total distance head had traveled when recording this
    }
    private List<PositionData> positionHistory = new List<PositionData>();
    private float headTotalDistance = 0f;

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

        // Disable the head's follower component entirely. The body will follow the ROOT object instead!
        headFollower.enabled = false;
        headFollower.follow = false;

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

        // Clear follower components from body segments - they will be driven entirely by the History buffer!
        for (int i = 1; i < activeSegments.Count; i++)
        {
            if (activeSegments[i].Follower != null)
            {
                activeSegments[i].Follower.enabled = false;
            }
        }

        // Initialize history with pre-filled positions backward from ROOT object
        positionHistory.Clear();
        float totalLength = activeSegments.Count * segmentSpacing * 2f;
        int samples = Mathf.CeilToInt(totalLength / 0.1f) + 1;

        if (bossSpline != null)
        {
            // We use the ROOT object's percent, as that is the true brain that moves!
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

                positionHistory.Add(new PositionData {
                    position = sample.position,
                    rotation = sample.rotation,
                    distanceTraveled = -distBack
                });
            }

            UpdateSegmentSpacing(false, 1f);
        }
        else
        {
            positionHistory.Add(new PositionData {
                position = transform.position,
                rotation = transform.rotation,
                distanceTraveled = 0f
            });
        }
    }

    private void SpawnSegment(GameObject prefab, int index, SplineComputer track)
    {
        if (prefab == null) return;

        GameObject segmentObj = Instantiate(prefab, transform);
        segmentObj.name = $"DragonSegment_{index}";

        SplineFollower follower = segmentObj.GetComponent<SplineFollower>();
        if (follower == null) follower = segmentObj.AddComponent<SplineFollower>();

        DragonSegment segment = segmentObj.GetComponent<DragonSegment>();
        if (segment == null) segment = segmentObj.AddComponent<DragonSegment>();

        segment.Initialize(this, bossBrain, index);
        activeSegments.Add(segment);

        if (segment.isDestructiblePart)
        {
            destructibleSegments.Add(segment);
        }

        totalBossPower += segment.powerContribution;
    }

    /// <summary>
    /// Updates the target spline for all active segments (e.g. when the boss evades to a new track).
    /// </summary>
    public void SwitchToNewSpline(SplineComputer newTrack)
    {
        bossSpline = newTrack;
    }

    // Controls whether the Update loop forces rigid spacing. Disabled briefly when closing a gap.
    private bool isClosingGap = false;
    private float gapCloseTimer = 0f;
    private float gapCloseDuration = 1f;

    // When gap closing, segments blend from an inflated spacing value down to the normal spacing value
    private Dictionary<DragonSegment, float> currentSpacings = new Dictionary<DragonSegment, float>();

    private void LateUpdate()
    {
        if (activeSegments.Count == 0) return;

        // 1. Update the Breadcrumb History using the ROOT BOSS object, not the spawned head
        Vector3 currentHeadPos = transform.position;
        if (positionHistory.Count == 0) return;
        PositionData lastData = positionHistory[0];

        float distMovedSinceLastFrame = Vector3.Distance(currentHeadPos, lastData.position);

        if (distMovedSinceLastFrame > 0.05f)
        {
            headTotalDistance += distMovedSinceLastFrame;

            positionHistory.Insert(0, new PositionData {
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

        // 2. Handle Gap Closing Animation Timers
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
            UpdateSegmentSpacing(false, 1f); // Follow rigidly
        }
    }

    private void UpdateSegmentSpacing(bool animateSmoothly, float lerpT)
    {
        // 3. Move all segments (including the Head at index 0) along the history buffer.
        // Because they follow the root object, the head just trails at 0 distance!
        for (int i = 0; i < activeSegments.Count; i++)
        {
            DragonSegment segment = activeSegments[i];

            float requiredDistanceBehindHead = segmentSpacing * i;
            if (animateSmoothly && currentSpacings.ContainsKey(segment))
            {
                // Smoothly close the gap
                requiredDistanceBehindHead = Mathf.Lerp(currentSpacings[segment], segmentSpacing * i, lerpT);
            }

            float targetDistanceInHistory = headTotalDistance - requiredDistanceBehindHead;

            // Find the two breadcrumbs this distance falls between
            for (int j = 0; j < positionHistory.Count - 1; j++)
            {
                PositionData newer = positionHistory[j];
                PositionData older = positionHistory[j + 1];

                if (targetDistanceInHistory <= newer.distanceTraveled && targetDistanceInHistory >= older.distanceTraveled)
                {
                    // Interpolate between these two breadcrumbs
                    float range = newer.distanceTraveled - older.distanceTraveled;
                    float t = (newer.distanceTraveled - targetDistanceInHistory) / range; // 0 = at newer, 1 = at older

                    segment.transform.position = Vector3.Lerp(newer.position, older.position, t);
                    segment.transform.rotation = Quaternion.Slerp(newer.rotation, older.rotation, t);
                    break;
                }
            }
        }
    }

    public void PauseSplineFollow()
    {
        // No longer needed, root object handles its own follow state
    }

    public void ResumeSplineFollow()
    {
        // No longer needed
    }

    /// <summary>
    /// Called by a DragonSegment when it is destroyed.
    /// </summary>
    public void OnSegmentDestroyed(DragonSegment destroyedSegment)
    {
        totalBossPower -= destroyedSegment.powerContribution;

        activeSegments.Remove(destroyedSegment);
        destructibleSegments.Remove(destroyedSegment);

        if (destructibleSegments.Count == 0 && bossBrain != null)
        {
            Debug.Log("<color=green>[SegmentedDragonManager] All body segments destroyed! Triggering Boss Death.</color>");
            bossBrain.Die();
            return;
        }

        Debug.Log($"<color=magenta>[SegmentedDragonManager] A segment fell! Boss power reduced to {totalBossPower}. Closing gap!</color>");

        if (activeSegments.Count == 0)
        {
            // The whole dragon is dead!
            return;
        }

        currentSpacings.Clear();

        // When a piece in the middle dies, pieces behind it shift their indices down by 1.
        // We simply need to look at their old target distance vs their new target distance.
        // E.g. segment #4 (tail) is at index 4. It should be at distance 4*spacing.
        // When segment #3 dies, tail becomes index 3. It needs to move to 3*spacing.
        // But physically it is CURRENTLY sitting at 4*spacing.
        for (int i = 0; i < activeSegments.Count; i++)
        {
            // Their OLD index is whatever index they have right now before we update it
            int oldIndex = activeSegments[i].SegmentIndex;

            // Their NEW index is their position in the shortened list
            activeSegments[i].SegmentIndex = i;

            // Their current physical distance behind the head is just their old expected spacing
            float currentDistBehindHead = oldIndex * segmentSpacing;

            currentSpacings[activeSegments[i]] = currentDistBehindHead;
        }

        // Pause standard rigidly-spaced updates and begin the smooth gap close
        isClosingGap = true;
        gapCloseTimer = 0f;
    }

    /// <summary>
    /// Called by BossCreature when the overall health reaches 0.
    /// Commands all remaining permanent pieces (Head, Legs, Tail) to die.
    /// </summary>
    public void TriggerTotalDeath(System.Action onComplete)
    {
        Debug.Log("<color=red>[SegmentedDragonManager] The entire dragon is collapsing!</color>");

        int segmentsToDissolve = activeSegments.Count;

        if (segmentsToDissolve == 0)
        {
            onComplete?.Invoke();
            return;
        }

        System.Action onSegmentDissolved = () =>
        {
            segmentsToDissolve--;
            if (segmentsToDissolve <= 0)
            {
                activeSegments.Clear();
                onComplete?.Invoke();
            }
        };

        foreach (var segment in activeSegments)
        {
            if (segment != null)
            {
                segment.TriggerTotalDeath(onSegmentDissolved);
            }
            else
            {
                onSegmentDissolved();
            }
        }
    }

    public bool IsTethered()
    {
        return false;
    }
}

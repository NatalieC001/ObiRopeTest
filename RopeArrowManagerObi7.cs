
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

/*
 * SCENE SETUP INSTRUCTIONS:
 * 1. Attach this script to a global Manager object in the Scene Hierarchy (e.g., "RopeArrowManager").
 * 2. [solver]: Drag the single `ObiSolver` object from your Scene Hierarchy into this field.
 * 3. [ropeBlueprint]: Drag your custom ObiRopeBlueprint asset from your Project Files (Assets/.../CustomBlueprint.asset).
 * 4. [ropeMaterial]: Drag the material used for the rope from your Project Files.
 * 5. [ropeSection]: Drag an ObiRopeSection asset from your Project Files to define the mesh shape.
 */
public class RopeArrowManagerObi7 : MonoBehaviour
{
    [Header("Elemental Effects")]
    public GameObject fireEffectPrefab;
    public GameObject electricEffectPrefab;
    public GameObject stickyEffectPrefab;
    public GameObject stasisEffectPrefab;
    public GameObject iceEffectPrefab;



    public static RopeArrowManagerObi7 Instance { get; private set; }

    [Header("Obi 7 Setup (Drag from Hierarchy)")]
    [Tooltip("Drag the single ObiSolver object from the Scene Hierarchy.")]
    public ObiSolver solver;

    [Header("Assets (Drag from Project Files)")]
    [Tooltip("Drag the custom blueprint asset from Project Files.")]
    public ObiRopeBlueprint ropeBlueprint;

    [Tooltip("Drag the HitMe Prefab here. It will be mathematically pinned to the exact center of the generated rope.")]
    public GameObject hitMePrefab;

    [Tooltip("Drag a Material asset from Project Files.")]
    public Material ropeMaterial;

    [Tooltip("Drag an ObiRopeSection asset from Project Files to define the extruded shape.")]
    public ObiRopeSection ropeSection;

    [Header("Rope Settings")]
    public float ropeThickness = 0.05f;

    [Tooltip("0 = rigid steel cable (holds HitMe object firmly). 100 = flexible rubber band (sags heavily).")]
    [Range(0f, 100f)]
    public float stretchy = 0f;

    [Tooltip("Multiplies the calculated required length. <1.0 pulls the rope tighter, >1.0 adds slack.")]
    [Range(0.1f, 2.0f)]
    public float lengthMultiplier = 1.0f;

    // List of all active pairs
    public List<RopeArrowPairOB7> activePairs = new List<RopeArrowPairOB7>();

    // The current pair waiting for a second arrow
    private RopeArrowPairOB7 pendingPair;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Handled by direct Instance calls
    }

    private void OnDisable()
    {
    }

    /// <summary>
    /// Helper method for external scripts (like Magic Arrows) to find the correct pair
    /// when they collide with a specific ObiRope object.
    /// </summary>
    public RopeArrowPairOB7 GetPairForRope(ObiRope targetRope)
    {
        foreach (var pair in activePairs)
        {
            if (pair.Rope == targetRope) return pair;
        }
        return null;
    }

    /// <summary>
    /// Cleans up destroyed ropes to prevent memory leaks and updates the infusion
    /// timers and dynamic rope lengths for all active arrow pairs.
    /// </summary>
    private void Update()
    {
        // Clean up any destroyed pairs (e.g. broken tethers or destroyed arrows) to prevent memory leaks,
        // specifically ignoring pairs that are currently waiting in the Obi coroutine to be generated!
        activePairs.RemoveAll(p => p == null || (p.Rope == null && p != pendingPair && !p.IsGenerating));

        // Update lengths and infusion timers dynamically for all completed pairs
        foreach (var pair in activePairs)
        {
            if (pair.IsComplete)
            {
                pair.UpdatePair(Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// Catches the event when an arrow is destroyed (e.g. enemy died and despawned).
    /// Safely breaks any connected ropes and clears pending clipboards to prevent memory leaks.
    /// </summary>
    public void HandleArrowDestroyed(StickingArrow destroyedArrow)
    {
        if (pendingPair != null && pendingPair.Arrow1 == destroyedArrow)
        {
            Destroy(pendingPair);
            pendingPair = null;
        }

        // Find any active pair using this arrow and let it handle the detachment physics
        for (int i = activePairs.Count - 1; i >= 0; i--)
        {
            var pair = activePairs[i];
            if (pair != null)
            {
                if (pair.Arrow1 == destroyedArrow || pair.Arrow2 == destroyedArrow)
                {
                    pair.HandleArrowDestroyed(destroyedArrow);

                    // If the pair fully broke itself (both targets lost), it will destroy itself
                    if (pair == null || (!pair.IsGenerating && pair.Arrow1 == null && pair.Arrow2 == null))
                    {
                        activePairs.RemoveAt(i);
                    }
                }
            }
            else
            {
                activePairs.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Catches the event when a Rope Arrow hits a valid target.
    /// If it's the first arrow, it waits. If it's the second arrow, it triggers the rope creation.
    /// </summary>
    private void HandleArrowHit(StickingArrow arrow, GameObject hitTargetObject)
    {
        Debug.Log($"<color=magenta>[RopeManager] Received RegisterRopeArrow call from Target: {hitTargetObject.name}. Arrow: {arrow.gameObject.name}</color>");

        // If this arrow is already in a pending pair, ignore it to prevent weird states
        if (pendingPair != null && pendingPair.Arrow1 == arrow)
        {
            Debug.LogWarning("<color=orange>[RopeManager] Arrow tried to register, but it is already the FIRST arrow! Ignoring.</color>");
            return;
        }

        if (pendingPair == null)
        {
            // First arrow of a new pair
            pendingPair = ScriptableObject.CreateInstance<RopeArrowPairOB7>();
            pendingPair.Initialize(arrow);
            activePairs.Add(pendingPair);
            Debug.Log("<color=magenta>[RopeManager] Registered as FIRST arrow. Creating pending pair.</color>");
        }
        else
        {
            // Second arrow hit. Complete the pair!
            Debug.Log("<color=magenta>[RopeManager] Registered as SECOND arrow. Triggering Obi generation Coroutine...</color>");

            // Cache the reference to start the coroutine
            RopeArrowPairOB7 pairToGenerate = pendingPair;

            // IMMEDATELY NULLIFY PENDING PAIR.
            // THIS ENSURES THE VERY NEXT ARROW WILL 100% BE FORCED INTO THE (pendingPair == null) BLOCK
            pendingPair = null;

            pairToGenerate.IsGenerating = true;
            StartCoroutine(GenerateRopeForPair(pairToGenerate, arrow));
        }
    }

    /// <summary>
    /// Instantiates the custom blueprint untouched, uses the Cursor to adjust its length to span
    /// the gap, and uses the documented Snap attachment logic to rigidly bind the ends and the orb.
    /// </summary>
    /// /*The rope is created between two arrows. The HitMe object needs to appear exactly in the middle of the rope. Previously, it was spawned at the midpoint between the two arrows before the rope was fully created. This sometimes put it in the wrong place. The fix moves the HitMe spawn to after the rope is fully generated and physics are ready.It then uses the rope's own center particle position to place the HitMe, so it's always exactly in the middle of the rope.*/
    /// <summary>
    /// Instantiates the custom blueprint untouched, uses the Cursor to adjust its length to span
    /// the gap, and uses the documented Snap attachment logic to rigidly bind the ends and the orb.
    /// </summary>
    private IEnumerator GenerateRopeForPair(RopeArrowPairOB7 pair, StickingArrow secondArrow)
    {
        // One final check right at the top of the coroutine.
        if (pair == null) yield break;

        Debug.Log("<color=cyan>[RopeManager] Coroutine Start: Instantiating and Generating Blueprint.</color>");

        Transform t1 = pair.Arrow1.tailPoint != null ? pair.Arrow1.tailPoint : pair.Arrow1.transform;
        Transform t2 = secondArrow.tailPoint != null ? secondArrow.tailPoint : secondArrow.transform;

        Vector3 startLocal = solver.transform.InverseTransformPoint(t1.position);
        Vector3 endLocal = solver.transform.InverseTransformPoint(t2.position);

        Debug.Log($"<color=white>[RopeManager] DIAGNOSTIC - Arrow Transforms vs Solver Local Space:</color>\n" +
                  $"<color=white> Arrow 1 World Pos: {t1.position} | Solver Local: {startLocal}</color>\n" +
                  $"<color=white> Arrow 2 World Pos: {t2.position} | Solver Local: {endLocal}</color>");

        // 1. Create the physical Rope Actor GameObject FIRST.
        // We use TWO cursors (winches) so we can add/remove rope evenly from both halves
        GameObject ropeObject = new GameObject("ObiRope_RopeArrowPairOB7", typeof(ObiRope), typeof(ObiRopeExtrudedRenderer), typeof(ObiRopeCursor), typeof(ObiRopeCursor));

        // Parent to solver FIRST so local calculations are perfectly aligned
        ropeObject.transform.SetParent(solver.transform, false);

        // Adjust transform to be exactly halfway between targets
        ropeObject.transform.position = (t1.position + t2.position) / 2;

        if ((t2.position - t1.position).sqrMagnitude > 0.001f)
        {
            ropeObject.transform.rotation = Quaternion.FromToRotation(Vector3.right, t2.position - t1.position);
        }

        // Calculate control point positions and tangent vector relative to this rope GameObject
        Vector3 startPositionLS = ropeObject.transform.InverseTransformPoint(t1.position);
        Vector3 endPositionLS = ropeObject.transform.InverseTransformPoint(t2.position);

        // Calculate a safe tangent that doesn't bulge massively.
        // Instead of a 1-meter normalized vector, we scale it to the distance so the line is tightly strung.
        float distance = Vector3.Distance(startPositionLS, endPositionLS);
        Vector3 tangentLS = Vector3.right * (distance * 0.25f);
        if (distance > 0.001f)
        {
            tangentLS = (endPositionLS - startPositionLS).normalized * (distance * 0.25f);
        }

        // 2. Clone the user's provided blueprint to preserve thickness/resolution, but explicitly rewrite its path
        ObiRopeBlueprint instanceBlueprint = ScriptableObject.Instantiate(ropeBlueprint);

        Vector3 centerPositionLS = Vector3.Lerp(startPositionLS, endPositionLS, 0.5f);
        int filter = ObiUtils.MakeFilter(ObiUtils.CollideWithEverything, 1);

        instanceBlueprint.path.Clear();
        // Start point
        instanceBlueprint.path.AddControlPoint(startPositionLS, -tangentLS, tangentLS, Vector3.up, 0.1f, 0.1f, 1, filter, Color.white, "start");
        // Center point (For HitMe to attach to)
        instanceBlueprint.path.AddControlPoint(centerPositionLS, -tangentLS, tangentLS, Vector3.up, 0.1f, 0.1f, 1, filter, Color.white, "center");
        // End point
        instanceBlueprint.path.AddControlPoint(endPositionLS, -tangentLS, tangentLS, Vector3.up, 0.1f, 0.1f, 1, filter, Color.white, "end");

        instanceBlueprint.path.FlushEvents();

        yield return StartCoroutine(instanceBlueprint.Generate());

        // Safety check
        if (pair.Arrow1 == null || secondArrow == null)
        {
            Debug.LogError("<color=red>[RopeManager] Coroutine Abort: One of the arrows was destroyed during blueprint generation.</color>");
            Destroy(instanceBlueprint);
            pair.IsGenerating = false;
            yield break;
        }

        Debug.Log("<color=cyan>[RopeManager] Blueprint Generated! Spawning physical Rope Actor...</color>");

        int ropeLayer = LayerMask.NameToLayer("ObiRope");
        if (ropeLayer != -1) ropeObject.layer = ropeLayer;

        ObiRope rope = ropeObject.GetComponent<ObiRope>();
        ObiRopeExtrudedRenderer ropeRenderer = ropeObject.GetComponent<ObiRopeExtrudedRenderer>();
        ObiRopeCursor[] cursors = ropeObject.GetComponents<ObiRopeCursor>();

        ObiRopeCursor cursor1 = cursors[0];
        ObiRopeCursor cursor2 = cursors[1];

        rope.ropeBlueprint = instanceBlueprint;
        rope.tearingEnabled = false; // Ensure the rope cannot break/snap
        ropeRenderer.material = ropeMaterial;
        if (ropeSection != null) ropeRenderer.section = ropeSection;

        // Configure both winches to sit exactly in the middle of the rope.
        // Winch 1 will handle the left half. Winch 2 will handle the right half.
        cursor1.cursorMu = 0.5f;
        cursor1.sourceMu = 0.5f;
        cursor1.direction = false; // Points towards Arrow 1 (Left)

        cursor2.cursorMu = 0.5f;
        cursor2.sourceMu = 0.5f;
        cursor2.direction = true; // Points towards Arrow 2 (Right)

        // Lock the starting length so it doesn't spool endless slack onto the floor!
        // We give each winch exactly half of the total length so they don't fight.
        float totalTargetLength = rope.restLength * lengthMultiplier;
        float halfLength = totalTargetLength / 2f;
        cursor1.ChangeLength(halfLength);
        cursor2.ChangeLength(halfLength);

        // Wait a frame for Obi to register the particles into the solver array (WaitForFixedUpdate required for pin constraints!)
        yield return new WaitForFixedUpdate();

        // Apply stretchiness (compliance) based on user slider (0 = rigid steel, 100 = rubber band)
        var distanceConstraints = rope.GetConstraintsByType(Oni.ConstraintType.Distance) as ObiDistanceConstraintsBatch;
        if (distanceConstraints != null)
        {
            float complianceValue = stretchy / 100f;
            for (int i = 0; i < distanceConstraints.activeConstraintCount; i++)
            {
                distanceConstraints.stiffnesses[i] = new Vector2(complianceValue, 0f);
            }
            rope.SetConstraintsDirty(Oni.ConstraintType.Distance);
        }

        // 4. Fallback to Static Attachments (same as DynamicObiRope)
        ObiParticleAttachment attachment1 = ropeObject.AddComponent<ObiParticleAttachment>();
        attachment1.target = t1;
        attachment1.particleGroup = instanceBlueprint.groups[0];
        attachment1.attachmentType = ObiParticleAttachment.AttachmentType.Static;

        ObiParticleAttachment attachment2 = ropeObject.AddComponent<ObiParticleAttachment>();
        attachment2.target = t2;
        attachment2.particleGroup = instanceBlueprint.groups[2]; // Since we strictly generated 3 points
        attachment2.attachmentType = ObiParticleAttachment.AttachmentType.Static;

        GameObject spawnedHitMe = null;

        // Spawn HitMe at the midpoint between the two arrows
        if (hitMePrefab != null)
        {
            Vector3 centerPos = (t1.position + t2.position) / 2f;
            spawnedHitMe = Instantiate(hitMePrefab, centerPos, Quaternion.identity);

            Rigidbody hitMeRb = spawnedHitMe.GetComponent<Rigidbody>();
            if (hitMeRb != null)
            {
                hitMeRb.isKinematic = true;
            }

            // Add the surfer script
            ObiRopeSurfer surfer = spawnedHitMe.AddComponent<ObiRopeSurfer>();
            surfer.SetRope(rope);

            // Assign the specific index for the center particle exactly from the blueprint group
            // This guarantees we are targeting the exact center control point, avoiding timing issues
            // with activeParticleCount changing dynamically before the rope is fully loaded.
            if (instanceBlueprint.groups.Count >= 3 && instanceBlueprint.groups[1].particleIndices.Count > 0)
            {
                surfer.particleIndex = instanceBlueprint.groups[1].particleIndices[0];
            }
            else
            {
                // Fallback (this should never hit if the blueprint is standard 3-point)
                surfer.particleIndex = rope.activeParticleCount / 2;
            }
        }

        Debug.Log("<color=cyan>[RopeManager] Calling pair.CompletePair to finalize logic state.</color>");

        // Finalize pair data, handing both winches over to the script that manages the logic
        pair.CompletePair(secondArrow, rope, cursor1, cursor2, spawnedHitMe);
        pair.IsGenerating = false;

        Debug.Log("<color=lime>[RopeManager] Rope successfully created, stretched, and bound to targets securely via SnapAttachment API!</color>");
    }

    // =========================================================================
    // REQUIRED METHODS
    // =========================================================================

    public void RegisterRopeArrow(StickingArrow arrow, GameObject hitObj, Vector3 impactPoint)
    {
        HandleArrowHit(arrow, hitObj);
    }

    public void FreezeRope(ObiRope rope)
    {
        if (rope != null && rope.solver != null)
        {
            for (int i = 0; i < rope.activeParticleCount; ++i)
            {
                int solverIndex = rope.solverIndices[i];
                rope.solver.invMasses[solverIndex] = 0f;
            }
        }
    }

    public void HandleArrowTimeout(StickingArrow arrow)
    {
        if (pendingPair != null && pendingPair.Arrow1 == arrow)
        {
            Destroy(pendingPair);
            pendingPair = null;
        }
    }

    public void HandleArrowMiss(StickingArrow arrow)
    {
        HandleArrowDestroyed(arrow);
    }
}




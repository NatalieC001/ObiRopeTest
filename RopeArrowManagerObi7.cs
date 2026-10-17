using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Obi;

public interface IPausableMovement
{
    void PauseMovement();
    void ResumeMovement();
}

public class RopeArrowManagerObi7 : MonoBehaviour
{
    [Header("Elemental Effects")]
    public GameObject fireEffectPrefab;
    public GameObject electricEffectPrefab;
    public GameObject stickyEffectPrefab;
    public GameObject stasisEffectPrefab;
    public GameObject iceEffectPrefab;

    public static RopeArrowManagerObi7 Instance { get; private set; }

    public static event System.Action<RopeArrow> OnRopeBroken;

    [Header("Obi 7 Setup")]
    public ObiSolver solver;

    [Header("Assets")]
    public ObiRopeBlueprint ropeBlueprint;
    public GameObject hitMePrefab;
    public Material ropeMaterial;
    public ObiRopeSection ropeSection;

    [Header("Rope Settings")]
    public float ropeThickness = 0.05f;

    [Range(0f, 100f)]
    public float stretchy = 0f;

    [Range(0.1f, 2.0f)]
    public float lengthMultiplier = 1.0f;

    public List<RopeArrowPairOB7> activePairs = new List<RopeArrowPairOB7>();
    private RopeArrowPairOB7 pendingPair;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        activePairs.RemoveAll(p => p == null || (p.Rope == null && p != pendingPair && !p.IsGenerating));

        foreach (var pair in activePairs)
        {
            if (pair.IsComplete)
            {
                pair.UpdatePair(Time.deltaTime);
            }
        }
    }

    public void RegisterRopeArrow(StickingArrow arrow, GameObject hitObj, Vector3 impactPoint)
    {
        HandleArrowHit(arrow, hitObj);
    }

    public void RegisterRopeArrow(RopeArrow arrow, GameObject hitObj, Vector3 impactPoint)
    {
        HandleArrowHit(arrow, hitObj);
    }

    private void HandleArrowHit(StickingArrow arrow, GameObject hitTargetObject)
    {
        if (pendingPair != null && pendingPair.Arrow1 == arrow) return;

        FreezeTargetMovement(arrow);

        if (pendingPair == null)
        {
            pendingPair = ScriptableObject.CreateInstance<RopeArrowPairOB7>();
            pendingPair.Initialize(arrow);
            activePairs.Add(pendingPair);
        }
        else
        {
            RopeArrowPairOB7 pairToGenerate = pendingPair;
            pendingPair = null;

            pairToGenerate.IsGenerating = true;
            StartCoroutine(GenerateRopeForPair(pairToGenerate, arrow));
        }
    }

    private void HandleArrowHit(RopeArrow arrow, GameObject hitTargetObject)
    {
        HandleArrowHit((StickingArrow)arrow, hitTargetObject);
    }

    private IEnumerator GenerateRopeForPair(RopeArrowPairOB7 pair, StickingArrow secondArrow)
    {
        if (pair == null || pair.Arrow1 == null || secondArrow == null)
        {
            if (pair != null && pair.Arrow1 != null) UnfreezeTargetMovement(pair.Arrow1);
            if (secondArrow != null) UnfreezeTargetMovement(secondArrow);
            if (pair != null) pair.IsGenerating = false;
            yield break;
        }

        Transform t1 = pair.Arrow1.tailPoint != null ? pair.Arrow1.tailPoint : pair.Arrow1.transform;
        Transform t2 = secondArrow.tailPoint != null ? secondArrow.tailPoint : secondArrow.transform;

        Vector3 worldPos1 = t1.position;
        Vector3 worldPos2 = t2.position;

        GameObject ropeObject = new GameObject("ObiRope_RopeArrowPairOB7", typeof(ObiRope), typeof(ObiRopeExtrudedRenderer));
        ropeObject.transform.SetParent(solver.transform, false);
        ropeObject.transform.position = (worldPos1 + worldPos2) * 0.5f;
        ropeObject.transform.rotation = Quaternion.identity;

        Vector3 startRS = ropeObject.transform.InverseTransformPoint(worldPos1);
        Vector3 endRS = ropeObject.transform.InverseTransformPoint(worldPos2);
        Vector3 centerRS = Vector3.Lerp(startRS, endRS, 0.5f);

        Vector3 solverLocalStart = solver.transform.InverseTransformPoint(worldPos1);
        Vector3 solverLocalEnd = solver.transform.InverseTransformPoint(worldPos2);
        Vector3 solverLocalCenter = Vector3.Lerp(solverLocalStart, solverLocalEnd, 0.5f);

        ObiRope rope = ropeObject.GetComponent<ObiRope>();
        rope.tearingEnabled = false;
        rope.selfCollisions = false;

        ObiRopeBlueprint instanceBlueprint = ScriptableObject.Instantiate(ropeBlueprint);
        int noCollisionFilter = ObiUtils.MakeFilter(0, 0);

        instanceBlueprint.path.Clear();
        instanceBlueprint.path.AddControlPoint(startRS, Vector3.zero, Vector3.zero, Vector3.up, ropeThickness, 0.1f, 1, noCollisionFilter, Color.white, "start");
        instanceBlueprint.path.AddControlPoint(centerRS, Vector3.zero, Vector3.zero, Vector3.up, ropeThickness, 0.1f, 1, noCollisionFilter, Color.white, "center");
        instanceBlueprint.path.AddControlPoint(endRS, Vector3.zero, Vector3.zero, Vector3.up, ropeThickness, 0.1f, 1, noCollisionFilter, Color.white, "end");
        instanceBlueprint.path.FlushEvents();

        rope.ropeBlueprint = instanceBlueprint;
        yield return StartCoroutine(instanceBlueprint.Generate());

        while (!rope.isLoaded)
        {
            yield return null;
        }

        for (int i = 0; i < rope.activeParticleCount; i++)
        {
            float t = (float)i / (rope.activeParticleCount - 1);
            Vector3 targetPos = Vector3.Lerp(solverLocalStart, solverLocalEnd, t);

            rope.TeleportParticle(i, targetPos);
        }

        for (int i = 0; i < rope.activeParticleCount; i++)
        {
            int solverIndex = rope.solverIndices[i];
            rope.solver.filters[solverIndex] = noCollisionFilter;
        }

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

        int endGroupIndex = instanceBlueprint.groups.Count - 1;

        ObiParticleAttachment attachment1 = ropeObject.AddComponent<ObiParticleAttachment>();
        attachment1.target = t1;
        attachment1.particleGroup = instanceBlueprint.groups[0];
        attachment1.attachmentType = ObiParticleAttachment.AttachmentType.Static;
        attachment1.enabled = true;

        ObiParticleAttachment attachment2 = ropeObject.AddComponent<ObiParticleAttachment>();
        attachment2.target = t2;
        attachment2.particleGroup = instanceBlueprint.groups[endGroupIndex];
        attachment2.attachmentType = ObiParticleAttachment.AttachmentType.Static;
        attachment2.enabled = true;

        ObiRopeExtrudedRenderer ropeRenderer = ropeObject.GetComponent<ObiRopeExtrudedRenderer>();
        ropeRenderer.material = ropeMaterial;
        if (ropeSection != null) ropeRenderer.section = ropeSection;

        GameObject spawnedHitMe = null;
        if (hitMePrefab != null)
        {
            spawnedHitMe = Instantiate(hitMePrefab, ropeObject.transform.position, Quaternion.identity);
            Rigidbody hitMeRb = spawnedHitMe.GetComponent<Rigidbody>();
            if (hitMeRb != null) hitMeRb.isKinematic = true;

            foreach (var col in spawnedHitMe.GetComponentsInChildren<Collider>()) col.isTrigger = true;

            var obiColliderBasics = spawnedHitMe.GetComponentsInChildren<ObiColliderBase>();
            foreach(var obiCol in obiColliderBasics) Destroy(obiCol);

            ObiRopeSurfer surfer = spawnedHitMe.GetComponent<ObiRopeSurfer>();
            if (surfer == null) surfer = spawnedHitMe.AddComponent<ObiRopeSurfer>();
            surfer.SetRope(rope);
            int centerGroupIndex = Mathf.Clamp(instanceBlueprint.groups.Count / 2, 0, instanceBlueprint.groups.Count - 1);
            surfer.particleIndex = instanceBlueprint.groups[centerGroupIndex].particleIndices[0];
        }

        pair.CompletePair(secondArrow, rope, null, null, spawnedHitMe);
        pair.IsGenerating = false;

        if (pair != null && pair.Arrow1 != null) UnfreezeTargetMovement(pair.Arrow1);
        if (secondArrow != null) UnfreezeTargetMovement(secondArrow);
    }

    private void UnfreezeTargetMovement(StickingArrow arrow)
    {
        if (arrow == null || arrow.transform.parent == null) return;

        IPausableMovement[] pausableScripts = arrow.transform.parent.GetComponentsInParent<IPausableMovement>();
        foreach (var script in pausableScripts)
        {
            script.ResumeMovement();
        }

        MonoBehaviour[] allScripts = arrow.transform.parent.GetComponentsInParent<MonoBehaviour>();
        foreach (var script in allScripts)
        {
            if (script != null && script.GetType().Name.Contains("SplineFollower"))
            {
                script.enabled = true;
            }
        }
    }

    private void FreezeTargetMovement(StickingArrow arrow)
    {
        if (arrow == null || arrow.transform.parent == null) return;

        IPausableMovement[] pausableScripts = arrow.transform.parent.GetComponentsInParent<IPausableMovement>();
        foreach (var script in pausableScripts)
        {
            script.PauseMovement();
        }

        MonoBehaviour[] allScripts = arrow.transform.parent.GetComponentsInParent<MonoBehaviour>();
        foreach (var script in allScripts)
        {
            if (script != null && script.GetType().Name.Contains("SplineFollower"))
            {
                script.enabled = false;
            }
        }
    }

    public void HandleArrowDestroyed(StickingArrow destroyedArrow)
    {
        if (pendingPair != null && pendingPair.Arrow1 == destroyedArrow)
        {
            UnfreezeTargetMovement(pendingPair.Arrow1);
            Destroy(pendingPair);
            pendingPair = null;
        }

        for (int i = activePairs.Count - 1; i >= 0; i--)
        {
            var pair = activePairs[i];
            if (pair != null)
            {
                if (pair.Arrow1 == destroyedArrow || pair.Arrow2 == destroyedArrow)
                {
                    Transform deadT = destroyedArrow.tailPoint != null
                        ? destroyedArrow.tailPoint
                        : destroyedArrow.transform;

                    if (pair.Attachment1 != null && pair.Attachment1.target == deadT)
                    {
                        pair.Attachment1.enabled = false;
                        Destroy(pair.Attachment1);
                    }
                    if (pair.Attachment2 != null && pair.Attachment2.target == deadT)
                    {
                        pair.Attachment2.enabled = false;
                        Destroy(pair.Attachment2);
                    }

                    if (pair.Arrow1 != null && pair.Arrow1 != destroyedArrow) UnfreezeTargetMovement(pair.Arrow1);
                    if (pair.Arrow2 != null && pair.Arrow2 != destroyedArrow) UnfreezeTargetMovement(pair.Arrow2);

                    pair.HandleArrowDestroyed(destroyedArrow);

                    // If HandleArrowDestroyed called ForceBreakRope(), it Destroyed the ScriptableObject itself.
                    // To be safe, we check if pair is implicitly null because of UnityEngine.Object destruction.
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

    public void HandleArrowTimeout(StickingArrow arrow)
    {
        if (pendingPair != null && pendingPair.Arrow1 == arrow)
        {
            UnfreezeTargetMovement(pendingPair.Arrow1);
            Destroy(pendingPair);
            pendingPair = null;
        }
    }

    public void HandleArrowMiss(StickingArrow arrow)
    {
        HandleArrowDestroyed(arrow);
    }

    public void NotifyRopeBroken(RopeArrow ropeArrow)
    {
        OnRopeBroken?.Invoke(ropeArrow);
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

    public RopeArrowPairOB7 GetPairForRope(ObiRope targetRope)
    {
        foreach (var pair in activePairs)
        {
            if (pair.Rope == targetRope) return pair;
        }
        return null;
    }

    public void ApplyElementalEffect(RopeArrow arrow)
    {
        if (arrow == null) return;

        ElementTypeOB7 element = arrow.CurrentElement;

        ObiRope connected = arrow.GetConnectedRope();
        if (connected != null)
        {
            var pair = GetPairForRope(connected);
            if (pair != null)
            {
                bool ok = pair.TryInfuse(element);
                return;
            }
        }

        foreach (var p in activePairs)
        {
            if (p == null) continue;
            if (p.Arrow1 == arrow || p.Arrow2 == arrow)
            {
                bool ok = p.TryInfuse(element);
                return;
            }
        }

        if (pendingPair != null && pendingPair.Arrow1 == arrow)
        {
            bool ok = pendingPair.TryInfuse(element);
            return;
        }
    }
}

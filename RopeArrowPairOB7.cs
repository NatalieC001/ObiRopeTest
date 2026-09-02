using UnityEngine;
using Obi;

[CreateAssetMenu(fileName = "NewRopeArrowPair", menuName = "Rope Arrows/Rope Arrow Pair", order = 1)]
public class RopeArrowPairOB7 : ScriptableObject
{
    public StickingArrow Arrow1 { get; private set; }
    public StickingArrow Arrow2 { get; private set; }

    public ObiRope Rope { get; private set; }
    public ObiRopeCursor Cursor1 { get; private set; }
    public ObiRopeCursor Cursor2 { get; private set; }

    public ObiParticleAttachment Attachment1 { get; private set; }
    public ObiParticleAttachment Attachment2 { get; private set; }

    public GameObject HitMeObject { get; private set; }

    public enum RopeState { Pending, Tether, Bash, Tripwire, Slumped }

    private float animationTimer = 0f;
    public RopeState CurrentState { get; private set; } = RopeState.Pending;

    private float currentLength;
    private float maxLength;

    public bool IsInfusible { get; private set; } = false;
    private float infusionTimer = 0f;
    private const float MAX_INFUSION_TIME = 5.0f;

    public bool IsComplete => Arrow1 != null && Arrow2 != null;
    public bool IsGenerating { get; set; } = false;
    public bool IsLocked { get; private set; } = false;

    public void Initialize(StickingArrow firstArrow)
    {
        animationTimer = 0f;
        Arrow1 = firstArrow;
        CurrentState = RopeState.Pending;
        IsGenerating = false;
        IsLocked = false;
    }

    public void CompletePair(StickingArrow secondArrow, ObiRope generatedRope, ObiRopeCursor cursor1, ObiRopeCursor cursor2, GameObject spawnedHitMe)
    {
        if (IsLocked)
        {
            Debug.LogError("<color=red>[RopePair] CRITICAL: Attempted to modify a locked RopeArrowPair data set. Rejecting modification.</color>");
            return;
        }

        IsLocked = true; // Lock this data set permanently to prevent hijacking by other arrows.

        Arrow2 = secondArrow;
        Rope = generatedRope;
        Cursor1 = cursor1;
        Cursor2 = cursor2;
        HitMeObject = spawnedHitMe;

        Vector3 pos1 = Arrow1.tailPoint != null ? Arrow1.tailPoint.position : Arrow1.transform.position;
        Vector3 pos2 = Arrow2.tailPoint != null ? Arrow2.tailPoint.position : Arrow2.transform.position;

        float tightness = RopeArrowManagerObi7.Instance != null ? RopeArrowManagerObi7.Instance.lengthMultiplier : 1.0f;

        currentLength = Vector3.Distance(pos1, pos2) * tightness;
        maxLength = currentLength;

        Debug.Log($"<color=magenta>[RopePair] Rope physics generated. Initial Length: {currentLength:F2}</color>");
        DetermineState();
    }

    private void DetermineState()
    {
        bool t1Movable = IsMovable(Arrow1.transform.parent?.gameObject);
        bool t2Movable = IsMovable(Arrow2.transform.parent?.gameObject);

        if (t1Movable && t2Movable)
        {
            CurrentState = RopeState.Bash;
            DisableTargetLogic(Arrow1.transform.parent?.gameObject);
            DisableTargetLogic(Arrow2.transform.parent?.gameObject);
        }
        else if (t1Movable || t2Movable)
        {
            CurrentState = RopeState.Tether;
            if (t1Movable) DisableTargetLogic(Arrow1.transform.parent?.gameObject);
            if (t2Movable) DisableTargetLogic(Arrow2.transform.parent?.gameObject);
            IsInfusible = true;
            infusionTimer = MAX_INFUSION_TIME;
        }
        else
        {
            CurrentState = RopeState.Tripwire;
            IsInfusible = true;
            infusionTimer = MAX_INFUSION_TIME;
            if (Rope != null && Rope.isLoaded) FreezeRopeParticles();
        }
    }

    private void FreezeRopeParticles()
    {
        if (Rope == null || !Rope.isLoaded) return;
        for (int i = 0; i < Rope.activeParticleCount; ++i)
        {
            int solverIndex = Rope.solverIndices[i];
            Rope.solver.invMasses[solverIndex] = 0f;
        }
    }

    private void UnfreezeRopeParticles()
    {
        if (Rope == null || !Rope.isLoaded) return;
        for (int i = 0; i < Rope.activeParticleCount; ++i)
        {
            int solverIndex = Rope.solverIndices[i];
            float mass = Rope.ropeBlueprint.invMasses[i];
            Rope.solver.invMasses[solverIndex] = mass;
        }
    }

    private bool IsMovable(GameObject obj)
    {
        if (obj == null) return false;
        return obj.CompareTag("Enemy") || obj.GetComponentInParent<MovingTarget>() != null || obj.GetComponent<MovingTarget>() != null;
    }

    private void DisableTargetLogic(GameObject obj)
    {
        if (obj == null) return;
        MovingTarget mt = obj.GetComponentInParent<MovingTarget>();
        if (mt == null) mt = obj.GetComponent<MovingTarget>();
        if (mt != null) mt.DisableInternalLogic();
    }

    private void EnableTargetLogic(GameObject obj)
    {
        if (obj == null) return;
        if (!IsMovable(obj)) return;
        MovingTarget mt = obj.GetComponentInParent<MovingTarget>();
        if (mt == null) mt = obj.GetComponent<MovingTarget>();
        if (mt != null) mt.EnableInternalLogic();
    }

    public void HandleArrowDestroyed(StickingArrow destroyedArrow)
    {
        if (CurrentState == RopeState.Pending)
        {
            // If it's pending, and its arrow is destroyed, we just break it completely
            if (destroyedArrow == Arrow1)
            {
                ForceBreakRope();
            }
            return;
        }

        if (destroyedArrow == Arrow1)
        {
            if (Attachment1 != null) Destroy(Attachment1);
            Arrow1 = null;
        }
        else if (destroyedArrow == Arrow2)
        {
            if (Attachment2 != null) Destroy(Attachment2);
            Arrow2 = null;
        }

        if (Arrow1 == null && Arrow2 == null)
        {
            ForceBreakRope();
            return;
        }

        if (CurrentState != RopeState.Slumped)
        {
            CurrentState = RopeState.Slumped;
            UnfreezeRopeParticles();
            EnableTargetLogic(Arrow1 != null ? Arrow1.transform.parent?.gameObject : null);
            EnableTargetLogic(Arrow2 != null ? Arrow2.transform.parent?.gameObject : null);
        }
    }

    public void UpdatePair(float deltaTime)
    {
        if (!IsComplete || Rope == null || Cursor1 == null || Cursor2 == null) return;

        if (IsInfusible)
        {
            infusionTimer -= deltaTime;
            if (infusionTimer <= 0f)
            {
                IsInfusible = false;
                if (CurrentState == RopeState.Tether)
                {
                    EnableTargetLogic(Arrow1.transform.parent?.gameObject);
                    EnableTargetLogic(Arrow2.transform.parent?.gameObject);
                    ForceBreakRope();
                    return;
                }
            }
        }

        if (Arrow1 == null || Arrow1.transform.parent == null || Arrow2 == null || Arrow2.transform.parent == null)
        {
            // Missing a target, so we transition to slumped
            if (CurrentState != RopeState.Slumped)
            {
                CurrentState = RopeState.Slumped;
                UnfreezeRopeParticles();
                EnableTargetLogic(Arrow1 != null ? Arrow1.transform.parent?.gameObject : null);
                EnableTargetLogic(Arrow2 != null ? Arrow2.transform.parent?.gameObject : null);
            }
            return;
        }

        Vector3 pos1 = Arrow1.tailPoint != null ? Arrow1.tailPoint.position : Arrow1.transform.position;
        Vector3 pos2 = Arrow2.tailPoint != null ? Arrow2.tailPoint.position : Arrow2.transform.position;

        if (CurrentState == RopeState.Tripwire || CurrentState == RopeState.Slumped) return;

        if (CurrentState == RopeState.Bash)
        {
            animationTimer += deltaTime;

            // Anticipation (0.0s - 0.3s): Expand rope slightly to push away
            if (animationTimer <= 0.3f)
            {
                currentLength += 2f * deltaTime;
            }
            // Fast Pull (0.3s - 0.8s): Yank them together very fast
            else if (animationTimer <= 0.8f)
            {
                currentLength -= 15f * deltaTime;
            }
            // Collision Recoil (0.8s - 1.2s): Relax outwards slightly
            else if (animationTimer <= 1.2f)
            {
                currentLength += 3f * deltaTime;
            }
            // Collapse & Dissolve (> 1.2s): Shrink down to 0
            else
            {
                currentLength -= 5f * deltaTime;
            }

            currentLength = Mathf.Max(0.01f, currentLength);
            float halfLength = currentLength / 2f;
            Cursor1.ChangeLength(halfLength);
            Cursor2.ChangeLength(halfLength);
        }
        else if (CurrentState == RopeState.Tether)
        {
            float allowance = RopeArrowManagerObi7.Instance != null ? RopeArrowManagerObi7.Instance.tetherAllowance : 2.0f;
            // Actively shrink the rope to yank the lightweight object to the anchor until it hits the allowance
            float shrinkSpeed = 5f; // Fast, but smoothly clamps at allowance
            float shrinkAmount = shrinkSpeed * deltaTime;
            if (currentLength > allowance)
            {
                currentLength -= shrinkAmount;
                currentLength = Mathf.Max(allowance, currentLength);
                float halfLength = currentLength / 2f;
                Cursor1.ChangeLength(halfLength);
                Cursor2.ChangeLength(halfLength);
            }
        }
    }

    public bool TryInfuse(ElementTypeOB7 elementType)
    {
        if (!IsInfusible || CurrentState == RopeState.Slumped)
        {
            Debug.Log($"Failed to infuse rope with {elementType}. Window closed or rope is slumped.");
            return false;
        }

        Debug.Log($"Successfully infused rope with {elementType}!");
        ApplyEffectsToRope(elementType);
        IsInfusible = false;
        return true;
    }

    private void ApplyEffectsToRope(ElementTypeOB7 elementType)
    {
        GameObject effectPrefab = null;
        switch (elementType)
        {
            case ElementTypeOB7.Fire: effectPrefab = RopeArrowManagerObi7.Instance.fireEffectPrefab; break;
            case ElementTypeOB7.Electric: effectPrefab = RopeArrowManagerObi7.Instance.electricEffectPrefab; break;
            case ElementTypeOB7.Sticky: effectPrefab = RopeArrowManagerObi7.Instance.stickyEffectPrefab; break;
            case ElementTypeOB7.Stasis: effectPrefab = RopeArrowManagerObi7.Instance.stasisEffectPrefab; break;
            case ElementTypeOB7.Ice: effectPrefab = RopeArrowManagerObi7.Instance.iceEffectPrefab; break;
            default: return;
        }

        if (effectPrefab != null && Rope != null)
        {
            Vector3 centerPos = Vector3.zero;
            if (Arrow1 != null && Arrow2 != null)
            {
                centerPos = (Arrow1.transform.position + Arrow2.transform.position) / 2f;
            }
            else if (Rope != null)
            {
                centerPos = Rope.transform.position;
            }

            GameObject effect = Object.Instantiate(effectPrefab, centerPos, Quaternion.identity);
            effect.transform.SetParent(Rope.transform);
            Object.Destroy(effect, 3f);
        }

        if (CurrentState == RopeState.Tether)
        {
            Debug.Log($"Applying {elementType} damage to tethered target!");
        }
        else if (CurrentState == RopeState.Tripwire)
        {
            Debug.Log($"Tripwire infused with {elementType}.");
        }
    }

    public void ForceBreakRope()
    {
        if (HitMeObject != null) Object.Destroy(HitMeObject);
        if (Rope != null)
        {
            if (Rope.ropeBlueprint != null) Object.Destroy(Rope.ropeBlueprint);
            Object.Destroy(Rope.gameObject);
        }
        Destroy(this); // Destroy the ScriptableObject itself since it's fully broken
    }
}



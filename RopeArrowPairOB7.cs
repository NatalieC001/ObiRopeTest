
using UnityEngine;
using Obi;

[System.Serializable]
public class RopeArrowPairOB7
{
    public StickingArrow Arrow1 { get; private set; }
    public StickingArrow Arrow2 { get; private set; }

    public ObiRope Rope { get; private set; }
    public ObiRopeCursor Cursor1 { get; private set; }
    public ObiRopeCursor Cursor2 { get; private set; }

    public GameObject HitMeObject { get; private set; }

    public enum RopeState { Pending, Tether, Bash, Tripwire }
    public RopeState CurrentState { get; private set; } = RopeState.Pending;

    private float currentLength;
    private float maxLength;

    public bool IsInfusible { get; private set; } = false;
    private float infusionTimer = 0f;
    private const float MAX_INFUSION_TIME = 5.0f;

    public bool IsComplete => Arrow1 != null && Arrow2 != null;
    public bool IsGenerating { get; set; } = false;

    public RopeArrowPairOB7(StickingArrow firstArrow)
    {
        Arrow1 = firstArrow;
    }

    public void CompletePair(StickingArrow secondArrow, ObiRope generatedRope, ObiRopeCursor cursor1, ObiRopeCursor cursor2, GameObject spawnedHitMe)
    {
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

        if (Arrow1.transform.parent == null || Arrow2.transform.parent == null) return;

        Vector3 pos1 = Arrow1.tailPoint != null ? Arrow1.tailPoint.position : Arrow1.transform.position;
        Vector3 pos2 = Arrow2.tailPoint != null ? Arrow2.tailPoint.position : Arrow2.transform.position;

        if (CurrentState == RopeState.Tripwire) return;

        if (CurrentState == RopeState.Bash)
        {
            float shrinkSpeed = 10f;
            float shrinkAmount = shrinkSpeed * deltaTime;
            if (currentLength > 0.5f)
            {
                currentLength -= shrinkAmount;
                float halfLength = currentLength / 2f;
                Cursor1.ChangeLength(halfLength);
                Cursor2.ChangeLength(halfLength);
            }
        }
        else if (CurrentState == RopeState.Tether)
        {
            float tightness = RopeArrowManagerObi7.Instance != null ? RopeArrowManagerObi7.Instance.lengthMultiplier : 1.0f;
            float actualDistance = Vector3.Distance(pos1, pos2) * tightness;
            currentLength = Mathf.Min(actualDistance, maxLength);
            float halfLength = currentLength / 2f;
            Cursor1.ChangeLength(halfLength);
            Cursor2.ChangeLength(halfLength);
        }
    }

    public bool TryInfuse(ElementTypeOB7 elementType)
    {
        if (!IsInfusible)
        {
            Debug.Log($"Failed to infuse rope with {elementType}. Window closed.");
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
            Vector3 centerPos = (Arrow1.transform.position + Arrow2.transform.position) / 2f;
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
    }
}






//remove ElementalReceptorFor Hitme object so we can remove it from here too
//using UnityEngine;
//using Obi;

///* 
// * USAGE INSTRUCTIONS: 
// * This is a pure C# data class. Do NOT attach this to any GameObject in the scene. 
// * It is managed internally by the RopeArrowManagerObi7 script to track states and  
// * update rope cursor lengths dynamically. 
// */
//[System.Serializable]
//public class RopeArrowPairOB7
//{


//    public StickingArrow Arrow1 { get; private set; }
//    public StickingArrow Arrow2 { get; private set; }

//    public ObiRope Rope { get; private set; }
//    public ObiRopeCursor Cursor1 { get; private set; }
//    public ObiRopeCursor Cursor2 { get; private set; }

//    // Dynamic central target spawned by the manager 
//    private GameObject hitMePrefabInst;
//    private ElementalReceptorOB7 ropeReceptor;

//    public enum RopeState { Pending, Tether, Bash, Tripwire }
//    public RopeState CurrentState { get; private set; } = RopeState.Pending;

//    // Track the initial or target length 
//    private float currentLength;
//    private float maxLength;

//    // Infusion window tracking 
//    public bool IsInfusible { get; private set; } = false;
//    private float infusionTimer = 0f;
//    private const float MAX_INFUSION_TIME = 5.0f; // 5 second window to hit "HitMe" with an elemental arrow 

//    public bool IsComplete => Arrow1 != null && Arrow2 != null;

//    // Protects the pair from cleanup during the Obi 7 Coroutine generation delay 
//    public bool IsGenerating { get; set; } = false;

//    /// <summary> 
//    /// Creates a new, incomplete pair tracking the very first arrow that was shot. 
//    /// </summary> 
//    public RopeArrowPairOB7(StickingArrow firstArrow)
//    {
//        Arrow1 = firstArrow;
//    }

//    /// <summary> 
//    /// Finalizes the pairing process once the second arrow hits. 
//    /// Calculates the starting distance, determines what kind of rope we are making  
//    /// (Bash, Tether, or Tripwire), and wires up the elemental receptor. 
//    /// </summary> 
//    public void CompletePair(StickingArrow secondArrow, ObiRope generatedRope, ObiRopeCursor cursor1, ObiRopeCursor cursor2, GameObject spawnedHitMe)
//    {
//        Arrow2 = secondArrow;
//        Rope = generatedRope;
//        Cursor1 = cursor1;
//        Cursor2 = cursor2;
//        hitMePrefabInst = spawnedHitMe;

//        if (hitMePrefabInst != null)
//        {
//            ropeReceptor = hitMePrefabInst.GetComponent<ElementalReceptorOB7>();
//            if (ropeReceptor != null)
//            {
//                if (ropeReceptor.OnElementInfused == null)
//                {
//                    ropeReceptor.OnElementInfused = new UnityEngine.Events.UnityEvent<ElementTypeOB7>();
//                }
//                ropeReceptor.OnElementInfused.AddListener(OnElementInfused);
//            }
//        }

//        // Initialize length based on distance between targets 
//        Vector3 pos1 = Arrow1.tailPoint != null ? Arrow1.tailPoint.position : Arrow1.transform.position;
//        Vector3 pos2 = Arrow2.tailPoint != null ? Arrow2.tailPoint.position : Arrow2.transform.position;

//        // Grab the tightness multiplier from the manager so the rope doesn't suddenly slack when updated
//        float tightness = RopeArrowManagerObi7.Instance != null ? RopeArrowManagerObi7.Instance.lengthMultiplier : 1.0f;

//        currentLength = Vector3.Distance(pos1, pos2) * tightness;
//        maxLength = currentLength; // Store the initial gap distance so it never grows longer than this

//        Debug.Log($"<color=magenta>[RopePair] Rope physics generated. Calculating Target distance. Initial Length: {currentLength:F2}</color>");
//        DetermineState();
//    }

//    /// <summary> 
//    /// An internal callback that catches when the central "HitMe" prefab is struck by magic. 
//    /// Redirects the element down to the actual infusion logic. 
//    /// </summary> 
//    private void OnElementInfused(ElementTypeOB7 elementType)
//    {
//        if (Rope == null || !IsInfusible) return;
//        TryInfuse(elementType);
//    }

//    /// <summary> 
//    /// Looks at both targets the arrows are stuck to and decides the rope's role. 
//    /// If both are movable (enemies/crates), it becomes a "Bash" rope. 
//    /// If one is movable and the other is a wall, it becomes a "Tether" rope. 
//    /// If both are walls/floors, it becomes a frozen "Tripwire". 
//    /// </summary> 
//    private void DetermineState()
//    {
//        bool t1Movable = IsMovable(Arrow1.transform.parent?.gameObject);
//        bool t2Movable = IsMovable(Arrow2.transform.parent?.gameObject);

//        Debug.Log($"<color=magenta>[RopePair] Analyzing targets -> Target 1 Movable: {t1Movable} | Target 2 Movable: {t2Movable}</color>");

//        if (t1Movable && t2Movable)
//        {
//            CurrentState = RopeState.Bash;
//            Debug.Log($"<color=lime>[RopePair] State assigned: BASH. Subduing both targets.</color>");

//            // Disable internal logic/physics on both targets 
//            DisableTargetLogic(Arrow1.transform.parent?.gameObject);
//            DisableTargetLogic(Arrow2.transform.parent?.gameObject);
//        }
//        else if (t1Movable || t2Movable)
//        {
//            CurrentState = RopeState.Tether;
//            Debug.Log($"<color=lime>[RopePair] State assigned: TETHER. Subduing movable target and starting {MAX_INFUSION_TIME}s infusion timer.</color>");

//            // Subdue the movable target to create a dramatic, infinite-strength hold 
//            if (t1Movable) DisableTargetLogic(Arrow1.transform.parent?.gameObject);
//            if (t2Movable) DisableTargetLogic(Arrow2.transform.parent?.gameObject);

//            // Start infusion countdown for Tether 
//            IsInfusible = true;
//            infusionTimer = MAX_INFUSION_TIME;
//        }
//        else
//        {
//            CurrentState = RopeState.Tripwire;
//            Debug.Log($"<color=lime>[RopePair] State assigned: TRIPWIRE. Freezing rope physics and starting {MAX_INFUSION_TIME}s infusion timer.</color>");

//            // Start infusion countdown for Tripwire 
//            IsInfusible = true;
//            infusionTimer = MAX_INFUSION_TIME;

//            // For a tripwire, freeze the rope physics to save performance 
//            // while leaving particles active for collision (elemental infusion). 
//            // This is done by setting inverse mass to 0. 
//            if (Rope != null && Rope.isLoaded)
//            {
//                FreezeRopeParticles();
//            }
//            else if (Rope != null)
//            {
//                // Wait for the blueprint to load before freezing 
//                Rope.OnBlueprintLoaded += (actor, blueprint) => FreezeRopeParticles();
//            }
//        }
//    }

//    /// <summary> 
//    /// Optimizes the physics engine for Tripwires. 
//    /// Instead of updating the rope physics every frame, this stops the rope from moving 
//    /// while keeping its colliders completely active so enemies and magic arrows can still hit it. 
//    /// </summary> 
//    private void FreezeRopeParticles()
//    {
//        if (Rope == null || !Rope.isLoaded) return;

//        for (int i = 0; i < Rope.activeParticleCount; ++i)
//        {
//            int solverIndex = Rope.solverIndices[i];
//            Rope.solver.invMasses[solverIndex] = 0f;
//        }
//    }

//    /// <summary> 
//    /// A simple check to see if the object the arrow hit is an enemy or a dynamic physics crate. 
//    /// </summary> 
//    private bool IsMovable(GameObject obj)
//    {
//        if (obj == null) return false;
//        return obj.CompareTag("Enemy") || obj.GetComponentInParent<MovingTarget>() != null || obj.GetComponent<MovingTarget>() != null;
//    }

//    /// <summary> 
//    /// Subdues an enemy or crate by turning off its internal scripts (like its AI or NavMesh). 
//    /// This happens instantly when a Tether or Bash rope attaches to them. 
//    /// </summary> 
//    private void DisableTargetLogic(GameObject obj)
//    {
//        if (obj == null) return;
//        MovingTarget mt = obj.GetComponentInParent<MovingTarget>();
//        if (mt == null) mt = obj.GetComponent<MovingTarget>();

//        if (mt != null)
//        {
//            mt.DisableInternalLogic();
//        }
//    }

//    /// <summary> 
//    /// Restores the enemy's AI and movement scripts. 
//    /// This happens if a Tether breaks or the infusion timer runs out, allowing them to escape. 
//    /// </summary> 
//    private void EnableTargetLogic(GameObject obj)
//    {
//        if (obj == null) return;
//        // Only attempt to enable if it is actually a movable target 
//        if (!IsMovable(obj)) return;

//        MovingTarget mt = obj.GetComponentInParent<MovingTarget>();
//        if (mt == null) mt = obj.GetComponent<MovingTarget>();

//        if (mt != null)
//        {
//            mt.EnableInternalLogic();
//        }
//    }

//    /// <summary> 
//    /// Called every frame by the Manager. 
//    /// Handles the 5-second countdown timer for elemental infusions and physically shrinks 
//    /// or holds the rope based on whether it is a Bash or Tether. 
//    /// </summary> 
//    public void UpdatePair(float deltaTime)
//    {
//        if (!IsComplete || Rope == null || Cursor1 == null || Cursor2 == null) return;

//        // Handle infusion timer 
//        if (IsInfusible)
//        {
//            infusionTimer -= deltaTime;
//            if (infusionTimer <= 0f)
//            {
//                IsInfusible = false;
//                Debug.Log("Rope infusion window closed.");

//                // If it was a tether and the timer ran out, the enemy breaks free 
//                if (CurrentState == RopeState.Tether)
//                {
//                    Debug.Log("Tether timer ran out! The target escaped.");

//                    // Restore movement/AI to whichever target was subdued 
//                    EnableTargetLogic(Arrow1.transform.parent?.gameObject);
//                    EnableTargetLogic(Arrow2.transform.parent?.gameObject);

//                    ForceBreakRope();
//                    return; // Stop updating since rope is gone 
//                }
//            }
//        }

//        if (Arrow1.transform.parent == null || Arrow2.transform.parent == null) return;

//        Vector3 pos1 = Arrow1.tailPoint != null ? Arrow1.tailPoint.position : Arrow1.transform.position;
//        Vector3 pos2 = Arrow2.tailPoint != null ? Arrow2.tailPoint.position : Arrow2.transform.position;

//        if (CurrentState == RopeState.Tripwire)
//        {
//            // Tripwires are frozen and attached to unmovable objects, no length updates needed. 
//            return;
//        }
//        else if (CurrentState == RopeState.Bash)
//        {
//            // Violently pull them together by shrinking the length 
//            float shrinkSpeed = 10f; // units per second 
//            float shrinkAmount = shrinkSpeed * deltaTime;

//            if (currentLength > 0.5f) // Minimum length before they crash 
//            {
//                currentLength -= shrinkAmount;

//                // Provide each cursor its specific half of the total length
//                float halfLength = currentLength / 2f;
//                Cursor1.ChangeLength(halfLength);
//                Cursor2.ChangeLength(halfLength);
//            }
//        }
//        else if (CurrentState == RopeState.Tether)
//        {
//            float tightness = RopeArrowManagerObi7.Instance != null ? RopeArrowManagerObi7.Instance.lengthMultiplier : 1.0f;

//            // Dynamically evaluate the distance between the two targets
//            float actualDistance = Vector3.Distance(pos1, pos2) * tightness;

//            // We only want the rope to shrink (reel in) if the target moves closer.
//            // We never want it to grow longer than the initial maximum gap, 
//            // otherwise it would create slack and defeat the purpose of a restraint tether.
//            if (actualDistance < maxLength)
//            {
//                currentLength = actualDistance;
//            }
//            else
//            {
//                currentLength = maxLength;
//            }

//            // Provide each cursor its specific half of the total absolute required length
//            float halfLength = currentLength / 2f;
//            Cursor1.ChangeLength(halfLength);
//            Cursor2.ChangeLength(halfLength);
//        }
//    }

//    // --- Stubs for actions/effects --- 

//    /// <summary> 
//    /// A stub where future developers can put code to apply physical forces to the targets  
//    /// (e.g. yanking target A toward target B). 
//    /// </summary> 
//    public void TriggerTargetActions()
//    {
//        // Stub: e.g. If Target 1 is pulled, apply force to Target 2 
//        // Or trigger specific scripts on the HitTargets 
//    }

//    /// <summary> 
//    /// Attempts to apply an elemental magic effect to the rope. 
//    /// It will fail if the 5-second window has already closed or the enemy has escaped. 
//    /// </summary> 
//    public bool TryInfuse(ElementTypeOB7 elementType)
//    {
//        if (!IsInfusible)
//        {
//            Debug.Log($"Failed to infuse rope with {elementType}. The infusion window has closed or target escaped.");
//            return false;
//        }

//        Debug.Log($"Successfully infused rope with {elementType}!");
//        ApplyEffectsToRope(elementType);

//        // Once infused, usually we stop allowing further infusions 
//        IsInfusible = false;
//        return true;
//    }

//    /// <summary>
//    /// Fires the actual gameplay consequences of a successful infusion.
//    /// For Tethers, this delivers massive burst damage to the captured enemy.
//    /// For Tripwires, this arms the trap with the magic element so enemies walk into it.
//    /// </summary>
//    /// <summary>
//    /// Fires the actual gameplay consequences of a successful infusion.
//    /// For Tethers, this delivers massive burst damage to the captured enemy.
//    /// For Tripwires, this arms the trap with the magic element so enemies walk into it.
//    /// </summary>
//    private void ApplyEffectsToRope(ElementTypeOB7 elementType)
//    {
//        // Spawn particle effect on the rope
//        GameObject effectPrefab = null;
//        switch (elementType)
//        {
//            case ElementTypeOB7.Fire:
//                effectPrefab = RopeArrowManagerObi7.Instance.fireEffectPrefab;
//                break;
//            case ElementTypeOB7.Electric:
//                effectPrefab = RopeArrowManagerObi7.Instance.electricEffectPrefab;
//                break;
//            case ElementTypeOB7.Sticky:
//                effectPrefab = RopeArrowManagerObi7.Instance.stickyEffectPrefab;
//                break;
//            case ElementTypeOB7.Stasis:
//                effectPrefab = RopeArrowManagerObi7.Instance.stasisEffectPrefab;
//                break;
//            case ElementTypeOB7.Ice:
//                effectPrefab = RopeArrowManagerObi7.Instance.iceEffectPrefab;
//                break;
//            case ElementTypeOB7.Normal:
//                // Normal arrow doesn't spawn a particle effect
//                break;
//        }

//        if (effectPrefab != null && Rope != null)
//        {
//            Vector3 centerPos = (Arrow1.transform.position + Arrow2.transform.position) / 2f;
//            GameObject effect = Object.Instantiate(effectPrefab, centerPos, Quaternion.identity);
//            effect.transform.SetParent(Rope.transform);
//            Object.Destroy(effect, 3f);
//        }

//        // Apply state-specific effects
//        if (CurrentState == RopeState.Tether)
//        {
//            Debug.Log($"Applying massive {elementType} burst damage to the tethered target!");
//        }
//        else if (CurrentState == RopeState.Tripwire)
//        {
//            Debug.Log($"Tripwire infused with {elementType}. Waiting for an enemy to walk into it.");
//        }
//    }

//    /// <summary> 
//    /// Completely destroys the rope, the HitMe target, and the cloned blueprint. 
//    /// Cleans up all event listeners to prevent Unity memory leaks. 
//    /// </summary> 
//    public void ForceBreakRope()
//    {
//        // Unsubscribe from events to prevent memory leaks 
//        if (ropeReceptor != null && ropeReceptor.OnElementInfused != null)
//        {
//            ropeReceptor.OnElementInfused.RemoveListener(OnElementInfused);
//        }

//        // Destroy the spawned HitMe prefab 
//        if (hitMePrefabInst != null)
//        {
//            Object.Destroy(hitMePrefabInst);
//        }

//        if (Rope != null)
//        {
//            // Clean up the cloned blueprint ScriptableObject to prevent memory leaks 
//            if (Rope.ropeBlueprint != null)
//            {
//                Object.Destroy(Rope.ropeBlueprint);
//            }

//            // Destroy the generated rope object, breaking the connection 
//            Object.Destroy(Rope.gameObject);
//        }
//    }
//}



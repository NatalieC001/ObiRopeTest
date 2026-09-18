using UnityEngine;
using Dreamteck.Splines;

/// <summary>
/// Inherits from BaseBossMovement.
/// Used for purely flying bosses (like the Dragon or Butterfly).
/// Supports both Spline and Freestyle modes, allowing the boss to seamlessly blend
/// between resting on coils and engaging in serpentine flight.
/// </summary>
public class AirborneBossMovement : BaseBossMovement
{
    public enum MovementMode
    {
        Spline,
        Freestyle,
        BlendingToSpline
    }

    public enum FreestyleIntent
    {
        Pursue,
        Swoop,
        Bank,
        Stillhold,
        Withdraw
    }

    [Header("Mode State")]
    public MovementMode currentMode = MovementMode.Spline;

    [Header("Movement Speed")]
    [Tooltip("The core unified speed the boss flies at, whether on a spline or in freestyle mode.")]
    public float baseFlightSpeed = 3f;

    [Header("Freestyle Tuning")]
    public float bodyUndulationRate = 2f;
    public float coilTightness = 5f;
    public float minTurnRadius = 3f;

    [Header("Tether Struggle Setup")]
    [Tooltip("The local offset direction the dragon tries to pull when tethered (e.g. up and away).")]
    public Vector3 tetherStruggleDirection = new Vector3(0f, 5f, 5f);
    [Tooltip("How far the dragon tries to push past the anchor point to create tension.")]
    public float tetherStruggleDistance = 15f;

    private SplineFollower splineFollower;
    private CreatureStatusEffects statusEffects;
    private GameObject currentActivePath;

    // Blending state
    private float blendTimer = 0f;
    private float dynamicBlendDuration = 2f;
    private Vector3 blendStartPosition;
    private Quaternion blendStartRotation;

    // Freestyle State
    private FreestyleIntent currentIntent = FreestyleIntent.Stillhold;
    private Vector3 freestyleTargetPosition;
    private float freestyleSpeed;
    private float currentSpeedMultiplier = 1f;

    protected override void Awake()
    {
        base.Awake(); // Assuming BaseBossMovement has Awake
        splineFollower = GetComponent<SplineFollower>();
        statusEffects = GetComponent<CreatureStatusEffects>();
    }

    protected override void TickMovement()
    {
        // Apply global speed multiplier from Status Effects
        if (statusEffects != null)
        {
            currentSpeedMultiplier = statusEffects.CurrentSpeedMultiplier;
        }

        switch (currentMode)
        {
            case MovementMode.Spline:
                UpdateSplineMode();
                break;
            case MovementMode.Freestyle:
                UpdateFreestyleMode();
                break;
            case MovementMode.BlendingToSpline:
                UpdateBlendingMode();
                break;
        }

        if (isTethered)
        {
            UpdateTetherStruggle();
            ApplyTetherRubberBand();
        }
    }

    private void UpdateTetherStruggle()
    {
        // Calculate the struggle point dynamically based on the boss's mental state (Phase/Health)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null && bossBrain != null)
        {
            Vector3 playerPos = playerObj.transform.position;
            Vector3 toPlayer = (playerPos - transform.position).normalized;

            // Panicking: Low health or rapidly damaged
            if (bossBrain.currentPhase == BossCreature.BossPhase.Exhausted || bossBrain.GetCurrentHealthPct() < 0.25f)
            {
                // Thrash away and upward to snap the rope tight
                freestyleTargetPosition = transform.position - toPlayer * 6f + Vector3.up * 4f;
            }
            // Aggressive: Healthy and ready to fight
            else if (bossBrain.currentPhase == BossCreature.BossPhase.Engaged)
            {
                // Dive at the player
                freestyleTargetPosition = playerPos + toPlayer * -2f + Vector3.up * 1.5f;
            }
            // Orchestrating / Recharging
            else
            {
                // Climb and circle to build tension in the rope, keeping distance
                Vector3 side = Vector3.Cross(toPlayer, Vector3.up);
                freestyleTargetPosition = transform.position + side * 7f + Vector3.up * 6f - toPlayer * 2f;
            }
        }
        else
        {
            // Fallback: If no player is found, pull blindly against the anchor
            SegmentedDragonManager dragonManager = GetComponent<SegmentedDragonManager>();
            if (dragonManager != null && dragonManager.TetherAnchorTransform != null)
            {
                Vector3 worldStruggleDirection = transform.TransformDirection(tetherStruggleDirection.normalized);
                freestyleTargetPosition = dragonManager.TetherAnchorTransform.position + (worldStruggleDirection * tetherStruggleDistance);
            }
        }
    }

    public void RequestFreestyleIntent(FreestyleIntent intent, Vector3 targetPos)
    {
        currentMode = MovementMode.Freestyle;
        currentIntent = intent;
        freestyleTargetPosition = targetPos;

        if (splineFollower != null) splineFollower.follow = false;
    }

    public void RequestReturnToCoil(GameObject observationPath)
    {
        if (observationPath != null)
        {
            currentActivePath = observationPath;
            currentMode = MovementMode.BlendingToSpline;
            blendTimer = 0f;
            blendStartPosition = transform.position;
            blendStartRotation = transform.rotation;

            if (splineFollower != null)
            {
                SplineComputer targetSpline = observationPath.GetComponentInChildren<SplineComputer>();
                splineFollower.spline = targetSpline;
                splineFollower.follow = false; // Stay manual until blend finishes

                if (targetSpline != null)
                {
                    // Calculate distance to nearest point on target spline
                    SplineSample targetSample = new SplineSample();
                    targetSpline.Project(transform.position, ref targetSample);
                    float distanceToSpline = Vector3.Distance(transform.position, targetSample.position);

                    // Dynamic duration based on consistent flight speed
                    dynamicBlendDuration = distanceToSpline / (baseFlightSpeed > 0 ? baseFlightSpeed : 1f);
                    if (dynamicBlendDuration < 0.5f) dynamicBlendDuration = 0.5f; // ensure it doesn't snap instantly if super close
                }
            }
        }
    }

    public void RequestGlideToSpline(GameObject escapePath)
    {
        RequestReturnToCoil(escapePath); // Same blend logic applies
    }

    private void UpdateSplineMode()
    {
        if (splineFollower != null)
        {
            splineFollower.follow = true;
            // Unify the spline speed with the exposed baseFlightSpeed
            splineFollower.followSpeed = baseFlightSpeed * currentSpeedMultiplier; // Modified by stasis/ice
        }
    }

    private void UpdateFreestyleMode()
    {
        // Use unified base speed, with minor multipliers based on the intensity of the action
        switch (currentIntent)
        {
            case FreestyleIntent.Pursue: freestyleSpeed = baseFlightSpeed; break;
            case FreestyleIntent.Swoop: freestyleSpeed = baseFlightSpeed * 1.5f; break;
            case FreestyleIntent.Withdraw: freestyleSpeed = baseFlightSpeed * 0.8f; break;
            case FreestyleIntent.Stillhold: freestyleSpeed = 0f; break;
            case FreestyleIntent.Bank: freestyleSpeed = baseFlightSpeed * 0.7f; break;
        }

        // Apply element slow/freeze multiplier
        float effectiveSpeed = freestyleSpeed * currentSpeedMultiplier;

        if (currentIntent == FreestyleIntent.Stillhold)
        {
            // Slow serpentine undulation keeping head steady
            float undulationOffset = Mathf.Sin(Time.time * bodyUndulationRate) * 0.5f;
            transform.position += transform.right * (undulationOffset * Time.deltaTime * effectiveSpeed);
        }
        else
        {
            // Smoothly ease position and rotation
            Vector3 direction = (freestyleTargetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * coilTightness);
            }
            transform.position += transform.forward * (effectiveSpeed * Time.deltaTime);
        }
    }

    private void UpdateBlendingMode()
    {
        if (splineFollower == null || splineFollower.spline == null)
        {
            currentMode = MovementMode.Freestyle;
            return;
        }

        blendTimer += Time.deltaTime * currentSpeedMultiplier; // Slow down blend if frozen
        float t = Mathf.Clamp01(blendTimer / dynamicBlendDuration);

        // Use smoothstep for easing
        t = t * t * (3f - 2f * t);

        // Find nearest point on the target spline to blend to
        SplineSample targetSample = new SplineSample();
        splineFollower.spline.Project(transform.position, ref targetSample);

        transform.position = Vector3.Lerp(blendStartPosition, (Vector3)targetSample.position, t);
        transform.rotation = Quaternion.Slerp(blendStartRotation, targetSample.rotation, t);

        if (t >= 1f)
        {
            currentMode = MovementMode.Spline;
            splineFollower.SetPercent(targetSample.percent);
            splineFollower.follow = true;
        }
    }

    public override void ForceImmediateEvasion()
    {
        currentActivePath = FindNearestEscapeRoute(PathTypeTag.PathType.Airborne);
        if (currentActivePath != null)
        {
            Debug.Log($"[{gameObject.name}] Airborne movement jumping to escape route: {currentActivePath.name}");
            RequestGlideToSpline(currentActivePath);
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Tried to evade, but no Airborne escape routes were found!");

            // Withdraw gracefully using the player's position to angle away
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Vector3 awayFromPlayer = (transform.position - playerObj.transform.position).normalized;
                awayFromPlayer.y = 0; // Keep evasion horizontal
                RequestFreestyleIntent(FreestyleIntent.Withdraw, transform.position + awayFromPlayer * 30f + Vector3.up * 20f);
            }
            else
            {
                RequestFreestyleIntent(FreestyleIntent.Withdraw, transform.position + transform.forward * 30f + Vector3.up * 20f);
            }
        }
    }

    public override void HandleTetherAttached()
    {
        base.HandleTetherAttached();

        // Convert the configurable local struggle direction into world space based on the dragon's current orientation
        Vector3 worldStruggleDirection = transform.TransformDirection(tetherStruggleDirection.normalized);
        Vector3 struggleTarget = transform.position + (worldStruggleDirection * tetherStruggleDistance);

        // Force into freestyle mode so the boss actively wrestles/struggles rather than rigidly following a spline
        RequestFreestyleIntent(FreestyleIntent.Pursue, struggleTarget);
    }

    private void ApplyTetherRubberBand()
    {
        // Limit flight to tether radius.
        SegmentedDragonManager dragonManager = GetComponent<SegmentedDragonManager>();
        if (dragonManager != null && dragonManager.IsTethered && dragonManager.TetherAnchorTransform != null)
        {
            Vector3 anchorPos = dragonManager.TetherAnchorTransform.position;
            float maxRadius = dragonManager.TetherMaxLength;

            float distanceToAnchor = Vector3.Distance(transform.position, anchorPos);
            if (distanceToAnchor > maxRadius && maxRadius > 0f)
            {
                // Clamp the root object's position so it can't stretch past the chain length
                Vector3 directionFromAnchor = (transform.position - anchorPos).normalized;
                transform.position = anchorPos + directionFromAnchor * maxRadius;
            }
        }
    }
}

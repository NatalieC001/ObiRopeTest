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

    [Header("Freestyle Tuning")]
    public float bodyUndulationRate = 2f;
    public float coilTightness = 5f;
    public float glideSpeed = 10f;
    public float minTurnRadius = 3f;
    public float blendDuration = 2f;

    private SplineFollower splineFollower;
    private CreatureStatusEffects statusEffects;
    private GameObject currentActivePath;

    // Blending state
    private float blendTimer = 0f;
    private Vector3 blendStartPosition;
    private Quaternion blendStartRotation;

    // Freestyle State
    private FreestyleIntent currentIntent = FreestyleIntent.Stillhold;
    private Vector3 freestyleTargetPosition;
    private float freestyleSpeed;
    private float currentSpeedMultiplier = 1f;

    protected virtual void Awake()
    {
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
            ApplyTetherRubberBand();
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
                splineFollower.spline = observationPath.GetComponentInChildren<SplineComputer>();
                splineFollower.follow = false; // Stay manual until blend finishes
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
            splineFollower.followSpeed = splineFollower.followSpeed * currentSpeedMultiplier; // Modified by stasis/ice

            // Check if we reached the end of the current spline
            if (splineFollower.GetPercent() >= 0.99)
            {
                // Prevent spamming
                splineFollower.follow = false;
                PixelCrushers.MessageSystem.SendMessage(this, "Brain", "SplineEnd", string.Empty);
            }
        }
    }

    private void UpdateFreestyleMode()
    {
        // Calculate base freestyle speed based on intent
        switch (currentIntent)
        {
            case FreestyleIntent.Pursue: freestyleSpeed = 15f; break;
            case FreestyleIntent.Swoop: freestyleSpeed = 25f; break;
            case FreestyleIntent.Withdraw: freestyleSpeed = glideSpeed; break;
            case FreestyleIntent.Stillhold: freestyleSpeed = 0f; break;
            case FreestyleIntent.Bank: freestyleSpeed = 12f; break;
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
            // Smoothly ease position and rotation toward the target
            Vector3 direction = (freestyleTargetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * coilTightness);
            }

            // INJECT SERPENTINE UNDULATION:
            // Instead of flying perfectly straight (which causes the history buffer to be a straight line, making the dragon look stiff),
            // we wobble the flight path left and right slightly over time. The history buffer will record this S-curve,
            // and the body segments will naturally slither through it!
            float undulationOffset = Mathf.Sin(Time.time * bodyUndulationRate * 2f) * 2.0f; // Scale up speed/width for flight
            Vector3 flightPath = transform.forward * effectiveSpeed;
            Vector3 wobblePath = transform.right * undulationOffset;

            transform.position += (flightPath + wobblePath) * Time.deltaTime;
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
        float t = Mathf.Clamp01(blendTimer / blendDuration);

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
            RequestFreestyleIntent(FreestyleIntent.Withdraw, transform.position + Vector3.up * 50f);
        }
    }

    public override void HandleTetherAttached()
    {
        base.HandleTetherAttached();

        // Force into freestyle mode so the boss actively wrestles/struggles rather than rigidly following a spline
        RequestFreestyleIntent(FreestyleIntent.Pursue, transform.position + transform.forward * 5f + Vector3.up * 5f);
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

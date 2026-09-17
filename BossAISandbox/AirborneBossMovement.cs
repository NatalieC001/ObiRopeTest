using UnityEngine;

/// <summary>
/// Inherits from BaseBossMovement.
/// Used for purely flying bosses (like the Dragon or Butterfly).
/// Exclusively requests Airborne paths from the BossPathManager.
/// </summary>
public class AirborneBossMovement : BaseBossMovement
{
    private GameObject currentActivePath;

    protected override void TickMovement()
    {
        if (isTethered)
        {
            ApplyTetherRubberBand();
            return;
        }

        // Dummy logic indicating how the desire states dictate movement
        // In reality, this reads from bossBrain.CurrentDesire
        bool desiresToEscape = false;

        if (desiresToEscape)
        {
            ExecuteEscapeChoreography();
        }
        else
        {
            ExecuteObservationSpline();
        }
    }

    public override void ForceImmediateEvasion()
    {
        currentActivePath = FindNearestEscapeRoute(PathTypeTag.PathType.Airborne);
        if (currentActivePath != null)
        {
            Debug.Log($"[{gameObject.name}] Airborne movement immediately jumping to escape route: {currentActivePath.name}");
            // Insert logic here to snap or lerp to the start of the escape path
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] Tried to evade, but no Airborne escape routes were found by the BossPathManager!");
        }
    }

    private void ExecuteEscapeChoreography()
    {
        if (currentActivePath == null)
        {
            // Specifically request an Airborne escape route
            currentActivePath = FindNearestEscapeRoute(PathTypeTag.PathType.Airborne);
        }

        if (currentActivePath != null)
        {
            // Code to smoothly lerp onto the spline and follow it upwards/away
            // Debug.Log("Flying along airborne escape spline...");
        }
        else
        {
            // Freestyle fallback if no choreographed route exists
            // Debug.Log("Freestyle escaping into the sky...");
        }
    }

    private void ExecuteObservationSpline()
    {
         if (currentActivePath == null)
         {
             currentActivePath = GetObservationPath(PathTypeTag.PathType.Airborne);
         }
         // Follow observation path logic
    }

    private void ApplyTetherRubberBand()
    {
        // Limit flight to tether radius.
    }
}

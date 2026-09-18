using UnityEngine;
using System.Collections.Generic;

public enum DesireType
{
    Survival,
    Vengeance,
    Regeneration,
    Territory,
    Attrition,
    ElementalAdvantage,
    Dominance,
    Recovery,
    CrystalDefense,
    None
}

public class DesireResult
{
    public DesireType StrongestDesire;
    public DesireType SecondChoice;
    public Transform TargetTransform;
    public Vector3 TargetPosition;
    public float Urgency; // 0 to 1
}

/// <summary>
/// A completely decoupled decision maker.
/// It runs a weighted desire table and returns a decision.
/// It holds no state and subscribes to no events. It only evaluates when called.
/// </summary>
public class DesireEvaluator : MonoBehaviour
{
    // Tuning Weights
    [Header("Base Desire Weights")]
    public float survivalWeight = 1f;
    public float vengeanceWeight = 1f;
    public float territoryWeight = 1f;

    // We can expand the weights as we iterate on the AI.

    public DesireResult Evaluate(BossCreature brain, EnvironmentTagRegistry registry)
    {
        DesireResult result = new DesireResult();

        // --- 1. Evaluate Crystal Defense (Absolute Priority if active) ---
        // If the brain tells us a crystal is currently threatened (e.g. within the last 5 seconds)
        if (brain.LastThreatenedCrystal != null)
        {
            result.StrongestDesire = DesireType.CrystalDefense;
            result.SecondChoice = DesireType.Vengeance;
            result.TargetTransform = brain.LastThreatenedCrystal.transform;
            result.TargetPosition = brain.LastThreatenedCrystal.transform.position;
            result.Urgency = 1.0f; // Maximum urgency
            return result;
        }

        // --- 2. Evaluate Survival & Recovery (Health/Stamina driven) ---
        float healthPct = brain.GetCurrentHealthPct();
        if (healthPct < 0.3f)
        {
            result.StrongestDesire = DesireType.Survival;
            result.SecondChoice = DesireType.Regeneration;

            // Try to find a crystal to retreat to
            EnvironmentTag tag = registry.GetNearestTag(brain.transform.position, EnvironmentTag.TagType.ObservationLoop);
            if (tag != null)
            {
                result.TargetTransform = tag.transform;
                result.TargetPosition = tag.transform.position;
            }
            result.Urgency = 1f - healthPct;
            return result;
        }

        // --- 3. Default to Dominance/Vengeance (Combat focus) ---
        result.StrongestDesire = DesireType.Dominance;
        result.SecondChoice = DesireType.Territory;
        result.TargetPosition = brain.transform.position; // Fallback
        result.Urgency = 0.5f;

        return result;
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines how a wave progresses to the next one.
/// </summary>
public enum WaveProgressionType
{
    /// <summary>
    /// The wave ends when all targets are destroyed.
    /// </summary>
    ClearAllTargets,

    /// <summary>
    /// The wave ends after a specific amount of time has passed, regardless of how many targets are left.
    /// </summary>
    TimeBased
}

/// <summary>
/// Defines the specific MonoBehaviour movement script to inject at runtime.
/// </summary>
public enum TargetMovementType
{
    None, // Static target
    CorkscrewMovement,
    FigureEightMovement,
    FishSwimMovement,
    PingPongMovement,
    Rotator,
    SwoopAndRetreatMovement,
    SplinePathAsset, // Used for standard baked Swarm prefabs
    BossDragonAsset  // Used specifically to trigger the Boss Dragon encounter
}

/// <summary>
/// Configuration for a single target to be spawned in a wave.
/// </summary>
[Serializable]
public struct TargetSpawnConfig
{
    [Tooltip("The spawn delay for this specific target relative to the start of the wave.")]
    public float spawnDelay;

    [Tooltip("The position to spawn the target. (Could be local to the spawn point or world space).")]
    public Vector3 spawnPosition;

    [Tooltip("The required arrow element type to hit this target.")]
    public ElementTypeOB7 requiredArrowElement;

    [Tooltip("Scale modifier. Smaller targets are harder to hit (e.g. 0.5 is 50% size).")]
    public float scaleModifier;

    [Tooltip("Speed modifier for the movement script. (e.g. 1.5 is 50% faster).")]
    public float speedModifier;

    [Tooltip("The movement behavior to attach to this target.")]
    public TargetMovementType movementBehavior;
}

/// <summary>
/// Data container for a single wave of targets.
/// </summary>
[CreateAssetMenu(fileName = "NewWaveData", menuName = "Archery Range/Wave Data", order = 1)]
public class WaveDataSO : ScriptableObject
{
    [Header("Wave Progression")]
    public WaveProgressionType progressionType = WaveProgressionType.ClearAllTargets;

    [Tooltip("The duration of the wave if ProgressionType is TimeBased. Ignored if ClearAllTargets.")]
    public float waveDuration = 60f;

    [Header("Target Configurations")]
    [Tooltip("List of targets to spawn in this wave.")]
    public List<TargetSpawnConfig> targets = new List<TargetSpawnConfig>();
}

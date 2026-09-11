using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data container for a full level, consisting of multiple waves.
/// </summary>
[CreateAssetMenu(fileName = "NewLevelConfig", menuName = "Archery Range/Level Config", order = 2)]
public class LevelConfigSO : ScriptableObject
{
    [Header("Level Settings")]
    [Tooltip("The name of the level to display to the player.")]
    public string levelName = "New Archery Level";

    [Tooltip("Text displayed to the player before the level begins (e.g. teaching a mechanic).")]
    [TextArea(2, 4)]
    public string levelIntroText = "Get Ready!";

    [Tooltip("Text displayed to the player when the entire level is completed.")]
    [TextArea(2, 4)]
    public string levelOutroText = "Level Complete!";

    [Tooltip("The prefab to use for spawning targets in this level. Should have a MovingTarget component.")]
    public GameObject vanillaTargetPrefab;

    [Header("Wave Progression")]
    [Tooltip("The ordered list of waves that make up this level.")]
    public List<WaveDataSO> waves = new List<WaveDataSO>();

    [Header("Global Modifiers")]
    [Tooltip("A global scale multiplier applied to all targets in this level.")]
    public float globalScaleMultiplier = 1.0f;

    [Tooltip("A global speed multiplier applied to all targets in this level.")]
    public float globalSpeedMultiplier = 1.0f;
}

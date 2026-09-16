using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines how a wave progresses to the next one.
/// </summary>
public enum WaveProgressionType
{
    ClearAllTargets,
    TimeBased
}

/// <summary>
/// Defines the specific movement script to inject at runtime.
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
    SplinePathAsset,
    BossDragonAsset
}

[Serializable]
public abstract class CharacterConfigBase
{
    public float spawnDelay;
    public Vector3 spawnPositionOffset;
    public ElementTypeOB7 requiredArrowElement;
}

[Serializable]
public class MinionConfig : CharacterConfigBase
{
    public GameObject prefab;
    public GameObject spawnPointPrefab;
    public TargetMovementType movementType;
    public GameObject movementAssetPrefab;
}

[Serializable]
public class BossConfig : CharacterConfigBase
{
    public GameObject prefab;
    public GameObject spawnPointPrefab;
    public List<GameObject> observationPathPrefabs = new List<GameObject>();
    public List<GameObject> escapePathPrefabs = new List<GameObject>();
}

[Serializable]
public class WaveData
{
    public WaveProgressionType progressionType = WaveProgressionType.ClearAllTargets;
    public float waveDuration = 60f;

    [SerializeReference]
    public List<CharacterConfigBase> characters = new List<CharacterConfigBase>();
}

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

    [Header("Folder Paths for Picker")]
    public string minionsFolderPath = "Assets/Prefabs/Minions";
    public string bossesFolderPath = "Assets/Prefabs/Bosses";
    public string pathsFolderPath = "Assets/Prefabs/Paths";

    [Header("Waves")]
    public List<WaveData> waves = new List<WaveData>();
}

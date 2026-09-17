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

    [Tooltip("Text displayed to the player when the entire level is completed. " +
             "Segues naturally onto the next level intro — no separate outro SO needed.")]
    [TextArea(2, 4)]
    public string levelOutroText = "Level Complete!";

    [Header("Folder Paths for Picker")]
    public string minionsFolderPath = "Assets/_Project/02_Scripts/SplinePathGenerator/Entities/M";
    public string bossesFolderPath = "Assets/_Project/02_Scripts/SplinePathGenerator/Entities/B";
    public string pathsFolderPath = "Assets/SplinePathGenerator/GeneratedPaths";
    public string ObservationEscapeFolderPath = "Assets/_Project/02_Scripts/SplinePathGenerator/Entities/ObservationAndEscapePaths";

    [Header("Waves")]
    public List<WaveData> waves = new List<WaveData>();
}

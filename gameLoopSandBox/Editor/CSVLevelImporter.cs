using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CSVLevelImporter : EditorWindow
{
    private TextAsset csvFile;
    private string minionsFolderPath = "Assets/Prefabs/Minions";
    private string bossesFolderPath = "Assets/Prefabs/Bosses";
    private string pathsFolderPath = "Assets/Prefabs/Paths";

    [MenuItem("Archery Range/CSV Level Importer")]
    public static void ShowWindow()
    {
        GetWindow<CSVLevelImporter>("CSV Level Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV Level Configuration Importer", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Select your LevelDesign.csv file to generate LevelConfigSO automatically.", MessageType.Info);

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        minionsFolderPath = EditorGUILayout.TextField("Minions Folder Path", minionsFolderPath);
        bossesFolderPath = EditorGUILayout.TextField("Bosses Folder Path", bossesFolderPath);
        pathsFolderPath = EditorGUILayout.TextField("Paths Folder Path", pathsFolderPath);

        if (GUILayout.Button("Generate Levels"))
        {
            if (csvFile == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a CSV file.", "OK");
                return;
            }

            ParseAndGenerate(csvFile.text);
        }
    }

    private void ParseAndGenerate(string csvContent)
    {
        // Split by lines (handling both \n and \r\n)
        string[] lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1) return; // Need at least header and one data row

        // LevelName -> LevelBuilderData
        Dictionary<string, LevelBuilderData> levelDataMap = new Dictionary<string, LevelBuilderData>();

        // Start reading from row 1 (skipping header)
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');
            // Expected cols:
            // 0:LevelName, 1:LevelIntroText, 2:LevelOutroText, 3:WaveName, 4:ProgressionType, 5:WaveDuration, 6:SpawnDelay,
            // 7:PosX, 8:PosZ, 9:ScaleModifier, 10:SpeedModifier, 11:MovementBehavior, 12:RequiredElement

            if (cols.Length < 13) continue;

            string levelName = cols[0].Trim();
            string waveName = cols[3].Trim();

            if (!levelDataMap.ContainsKey(levelName))
            {
                levelDataMap[levelName] = new LevelBuilderData()
                {
                    introText = cols[1].Trim(),
                    outroText = cols[2].Trim(),
                    waves = new Dictionary<string, WaveData>()
                };
            }

            if (!levelDataMap[levelName].waves.ContainsKey(waveName))
            {
                WaveData newWaveData = new WaveData();

                if (Enum.TryParse(cols[4].Trim(), true, out WaveProgressionType progEnum))
                {
                    newWaveData.progressionType = progEnum;
                }

                float.TryParse(cols[5], out newWaveData.waveDuration);
                newWaveData.characters = new List<CharacterConfigBase>();

                levelDataMap[levelName].waves[waveName] = newWaveData;
            }

            float spawnDelay = 0f;
            float.TryParse(cols[6], out spawnDelay);

            float posX = 0f;
            float posZ = 0f;
            float.TryParse(cols[7], out posX);
            float.TryParse(cols[8], out posZ);
            Vector3 spawnPositionOffset = new Vector3(posX, 0f, posZ);

            TargetMovementType movementBehavior = TargetMovementType.None;
            if (Enum.TryParse(cols[11].Trim(), true, out TargetMovementType moveEnum))
            {
                movementBehavior = moveEnum;
            }

            ElementTypeOB7 requiredElement = ElementTypeOB7.Normal;
            if (Enum.TryParse(cols[12].Trim(), true, out ElementTypeOB7 elementEnum))
            {
                requiredElement = elementEnum;
            }

            CharacterConfigBase config = null;

            if (movementBehavior == TargetMovementType.BossDragonAsset)
            {
                BossConfig bossConfig = new BossConfig();
                bossConfig.spawnDelay = spawnDelay;
                bossConfig.spawnPositionOffset = spawnPositionOffset;
                bossConfig.requiredArrowElement = requiredElement;
                config = bossConfig;
            }
            else
            {
                MinionConfig minionConfig = new MinionConfig();
                minionConfig.spawnDelay = spawnDelay;
                minionConfig.spawnPositionOffset = spawnPositionOffset;
                minionConfig.requiredArrowElement = requiredElement;
                minionConfig.movementType = movementBehavior;
                config = minionConfig;
            }

            levelDataMap[levelName].waves[waveName].characters.Add(config);
        }

        GenerateAssets(levelDataMap);
    }

    private void GenerateAssets(Dictionary<string, LevelBuilderData> levelDataMap)
    {
        string rootPath = "Assets/Data";
        if (!AssetDatabase.IsValidFolder(rootPath)) AssetDatabase.CreateFolder("Assets", "Data");

        string levelsPath = rootPath + "/Levels";
        if (!AssetDatabase.IsValidFolder(levelsPath)) AssetDatabase.CreateFolder(rootPath, "Levels");

        foreach (var levelKvp in levelDataMap)
        {
            string levelName = levelKvp.Key;
            LevelBuilderData levelDataInfo = levelKvp.Value;

            // Create Level Config
            string levelAssetPath = $"{levelsPath}/{levelName}.asset";
            LevelConfigSO levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(levelAssetPath);
            if (levelConfig == null)
            {
                levelConfig = ScriptableObject.CreateInstance<LevelConfigSO>();
                AssetDatabase.CreateAsset(levelConfig, levelAssetPath);
            }

            levelConfig.levelName = levelName;
            levelConfig.levelIntroText = levelDataInfo.introText;
            levelConfig.levelOutroText = levelDataInfo.outroText;
            levelConfig.minionsFolderPath = minionsFolderPath;
            levelConfig.bossesFolderPath = bossesFolderPath;
            levelConfig.pathsFolderPath = pathsFolderPath;

            levelConfig.waves.Clear();

            foreach (var waveKvp in levelDataInfo.waves)
            {
                levelConfig.waves.Add(waveKvp.Value);
            }

            EditorUtility.SetDirty(levelConfig);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "Levels generated successfully from CSV!", "OK");
    }

    private class LevelBuilderData
    {
        public string introText;
        public string outroText;
        public Dictionary<string, WaveData> waves;
    }
}

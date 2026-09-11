using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class CSVLevelImporter : EditorWindow
{
    private TextAsset csvFile;
    private GameObject vanillaTargetPrefab;

    [MenuItem("Archery Range/CSV Level Importer")]
    public static void ShowWindow()
    {
        GetWindow<CSVLevelImporter>("CSV Level Importer");
    }

    private void OnGUI()
    {
        GUILayout.Label("CSV Level Configuration Importer", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox("Select your LevelDesign.csv file and the default Target Prefab to generate ScriptableObjects automatically.", MessageType.Info);

        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV File", csvFile, typeof(TextAsset), false);
        vanillaTargetPrefab = (GameObject)EditorGUILayout.ObjectField("Vanilla Target Prefab", vanillaTargetPrefab, typeof(GameObject), false);

        if (GUILayout.Button("Generate Levels & Waves"))
        {
            if (csvFile == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a CSV file.", "OK");
                return;
            }
            if (vanillaTargetPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign the Vanilla Target Prefab.", "OK");
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
                    waves = new Dictionary<string, WaveDataSO>()
                };
            }

            if (!levelDataMap[levelName].waves.ContainsKey(waveName))
            {
                WaveDataSO newWaveData = ScriptableObject.CreateInstance<WaveDataSO>();

                if (Enum.TryParse(cols[4].Trim(), true, out WaveProgressionType progEnum))
                {
                    newWaveData.progressionType = progEnum;
                }

                float.TryParse(cols[5], out newWaveData.waveDuration);
                newWaveData.targets = new List<TargetSpawnConfig>();

                levelDataMap[levelName].waves[waveName] = newWaveData;
            }

            TargetSpawnConfig config = new TargetSpawnConfig();

            float.TryParse(cols[6], out config.spawnDelay);

            float posX, posZ;
            float.TryParse(cols[7], out posX);
            float.TryParse(cols[8], out posZ);
            config.spawnPosition = new Vector3(posX, 0, posZ);

            if (!float.TryParse(cols[9], out config.scaleModifier)) config.scaleModifier = 1f;
            if (!float.TryParse(cols[10], out config.speedModifier)) config.speedModifier = 1f;

            if (Enum.TryParse(cols[11].Trim(), true, out TargetMovementType moveEnum))
            {
                config.movementBehavior = moveEnum;
            }
            else
            {
                config.movementBehavior = TargetMovementType.None;
            }

            if (Enum.TryParse(cols[12].Trim(), true, out ElementTypeOB7 elementEnum))
            {
                config.requiredArrowElement = elementEnum;
            }
            else
            {
                config.requiredArrowElement = ElementTypeOB7.Normal;
            }

            levelDataMap[levelName].waves[waveName].targets.Add(config);
        }

        GenerateAssets(levelDataMap);
    }

    private void GenerateAssets(Dictionary<string, LevelBuilderData> levelDataMap)
    {
        string rootPath = "Assets/Data";
        if (!AssetDatabase.IsValidFolder(rootPath)) AssetDatabase.CreateFolder("Assets", "Data");

        string wavesPath = rootPath + "/Waves";
        if (!AssetDatabase.IsValidFolder(wavesPath)) AssetDatabase.CreateFolder(rootPath, "Waves");

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
            levelConfig.vanillaTargetPrefab = vanillaTargetPrefab;
            levelConfig.waves.Clear();

            foreach (var waveKvp in levelDataInfo.waves)
            {
                string waveName = waveKvp.Key;
                WaveDataSO parsedWaveData = waveKvp.Value;

                // Create Wave Config
                string waveAssetPath = $"{wavesPath}/{levelName}_{waveName}.asset";
                WaveDataSO waveData = AssetDatabase.LoadAssetAtPath<WaveDataSO>(waveAssetPath);
                if (waveData == null)
                {
                    waveData = ScriptableObject.CreateInstance<WaveDataSO>();
                    AssetDatabase.CreateAsset(waveData, waveAssetPath);
                }

                waveData.progressionType = parsedWaveData.progressionType;
                waveData.waveDuration = parsedWaveData.waveDuration;
                waveData.targets = parsedWaveData.targets;

                EditorUtility.SetDirty(waveData);
                levelConfig.waves.Add(waveData);
            }

            EditorUtility.SetDirty(levelConfig);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success", "Levels and Waves generated successfully from CSV!", "OK");
    }

    private class LevelBuilderData
    {
        public string introText;
        public string outroText;
        public Dictionary<string, WaveDataSO> waves;
    }
}

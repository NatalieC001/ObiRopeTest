using UnityEditor;
using UnityEngine;
using System.IO;

public class LevelDiagnosticTool : EditorWindow
{
    [MenuItem("Archery Range/Run Level Diagnostics")]
    public static void ShowWindow()
    {
        RunDiagnostics();
    }

    private static void RunDiagnostics()
    {
        string reportPath = "Assets/LevelDiagnosticReport.txt";
        using (StreamWriter writer = new StreamWriter(reportPath))
        {
            writer.WriteLine("======================================");
            writer.WriteLine("🎯 ARCHERY RANGE LEVEL DIAGNOSTICS");
            writer.WriteLine("======================================");
            writer.WriteLine("This report shows exactly what is configured to spawn in each level.");
            writer.WriteLine();

            string[] configGuids = AssetDatabase.FindAssets("t:LevelConfigSO");
            if (configGuids.Length == 0)
            {
                writer.WriteLine("NO LEVEL CONFIGS FOUND. Have you run the CSV Importer yet?");
                return;
            }

            foreach (string guid in configGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                LevelConfigSO config = AssetDatabase.LoadAssetAtPath<LevelConfigSO>(path);

                writer.WriteLine($"\n--- Level: {config.levelName} ---");

                // Verify Prefabs
                writer.WriteLine("PREFAB ASSIGNMENTS:");
                writer.WriteLine($"  - Vanilla Target: {(config.vanillaTargetPrefab != null ? config.vanillaTargetPrefab.name : "MISSING!")}");
                writer.WriteLine($"  - Spline Swarm:   {(config.splinePathAssetPrefab != null ? config.splinePathAssetPrefab.name : "MISSING!")}");
                writer.WriteLine($"  - Boss Dragon:    {(config.bossDragonPrefab != null ? config.bossDragonPrefab.name : "MISSING!")}");
                writer.WriteLine();

                if (config.bossDragonPrefab == null || config.splinePathAssetPrefab == null)
                {
                     writer.WriteLine("  WARNING: You have MISSING prefabs in this level config!");
                     writer.WriteLine("  If you ask a wave to spawn a Boss or a Swarm, it might fail or spawn the wrong thing.");
                     writer.WriteLine("  FIX: Open the CSV Importer and assign all prefabs before generating, or assign them manually in the inspector.");
                }

                // Verify Waves
                for (int i = 0; i < config.waves.Count; i++)
                {
                    WaveDataSO wave = config.waves[i];
                    writer.WriteLine($"  Wave {i + 1}: {wave.progressionType}");

                    int vanillaCount = 0;
                    int splineCount = 0;
                    int bossCount = 0;

                    foreach (var target in wave.targets)
                    {
                        if (target.movementBehavior == TargetMovementType.SplinePathAsset) splineCount++;
                        else if (target.movementBehavior == TargetMovementType.BossDragonAsset) bossCount++;
                        else vanillaCount++;
                    }

                    writer.WriteLine($"    -> Spawning: {vanillaCount} Normal Targets, {splineCount} Swarms, {bossCount} Bosses.");

                    if (bossCount > 1)
                    {
                        writer.WriteLine($"    -> ⚠️ CRITICAL WARNING: This wave is spawning {bossCount} Bosses at the same time!");
                        writer.WriteLine($"       This is usually a mistake. Check your CSV and ensure only ONE row uses 'BossDragonAsset'.");
                    }
                }
            }
        }

        AssetDatabase.ImportAsset(reportPath);
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(reportPath);
        Selection.activeObject = textAsset;
        EditorGUIUtility.PingObject(textAsset);

        Debug.Log($"<color=cyan>[LevelDiagnosticTool] Diagnostics complete! Report generated at {reportPath}</color>");
    }
}

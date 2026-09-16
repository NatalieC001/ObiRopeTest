using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LevelConfigSO))]
public class LevelConfigSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        LevelConfigSO config = (LevelConfigSO)target;

        EditorGUILayout.LabelField("Level Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("levelName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("levelIntroText"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("levelOutroText"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Folder Paths for Picker", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("minionsFolderPath"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bossesFolderPath"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);

        SerializedProperty wavesProp = serializedObject.FindProperty("waves");
        for (int i = 0; i < wavesProp.arraySize; i++)
        {
            SerializedProperty waveProp = wavesProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Wave {i + 1}", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("progressionType"), new GUIContent("Wave Type"));
            if ((WaveProgressionType)waveProp.FindPropertyRelative("progressionType").enumValueIndex == WaveProgressionType.TimeBased)
            {
                EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("waveDuration"), new GUIContent("Duration"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Characters", EditorStyles.boldLabel);

            SerializedProperty charactersProp = waveProp.FindPropertyRelative("characters");

            for (int j = 0; j < charactersProp.arraySize; j++)
            {
                SerializedProperty charProp = charactersProp.GetArrayElementAtIndex(j);
                DrawCharacterConfig(charProp, config, j);
            }

            // Buttons to add manually (even though CSV does it mostly)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Minion"))
            {
                config.waves[i].characters.Add(new MinionConfig());
                EditorUtility.SetDirty(config);
            }
            if (GUILayout.Button("Add Boss"))
            {
                config.waves[i].characters.Add(new BossConfig());
                EditorUtility.SetDirty(config);
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button($"Remove Wave {i + 1}", GUILayout.Width(120)))
            {
                wavesProp.DeleteArrayElementAtIndex(i);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        if (GUILayout.Button("Add New Wave"))
        {
            wavesProp.arraySize++;
            serializedObject.ApplyModifiedProperties();
            config.waves[config.waves.Count - 1] = new WaveData();
            EditorUtility.SetDirty(config);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCharacterConfig(SerializedProperty charProp, LevelConfigSO config, int index)
    {
        EditorGUILayout.BeginVertical("helpbox");

        // The managed reference value gives us the actual type (MinionConfig or BossConfig)
        string typeName = charProp.managedReferenceFullTypename;

        if (typeName.Contains("MinionConfig"))
        {
            EditorGUILayout.LabelField($"Character {index + 1}: Minion", EditorStyles.boldLabel);
            DrawPrefabSelectionRow("Prefab", charProp.FindPropertyRelative("prefab"), config.minionsFolderPath);

            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnPointPrefab"), new GUIContent("Spawn Point (Prefab)"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnDelay"), new GUIContent("Spawn Delay"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("requiredArrowElement"), new GUIContent("Element"));

            SerializedProperty movementTypeProp = charProp.FindPropertyRelative("movementType");
            EditorGUILayout.PropertyField(movementTypeProp, new GUIContent("Movement Type"));

            TargetMovementType currentMoveType = (TargetMovementType)movementTypeProp.enumValueIndex;
            if (currentMoveType == TargetMovementType.SplinePathAsset)
            {
                EditorGUILayout.PropertyField(charProp.FindPropertyRelative("movementAssetPrefab"), new GUIContent("Movement Asset (Prefab)"));
            }
        }
        else if (typeName.Contains("BossConfig"))
        {
            EditorGUILayout.LabelField($"Character {index + 1}: Big Boss", EditorStyles.boldLabel);
            DrawPrefabSelectionRow("Prefab", charProp.FindPropertyRelative("prefab"), config.bossesFolderPath);

            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnPointPrefab"), new GUIContent("Spawn Point (Prefab)"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnDelay"), new GUIContent("Spawn Delay"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("requiredArrowElement"), new GUIContent("Element"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("observationPathPrefab"), new GUIContent("Observation Path (Prefab)"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("escapePathPrefab"), new GUIContent("Escape Path (Prefab)"));
        }
        else
        {
            EditorGUILayout.LabelField($"Character {index + 1}: Unknown", EditorStyles.boldLabel);
        }

        // Add a remove button for the character
        if (GUILayout.Button("Remove Character", GUILayout.Width(150)))
        {
            // Note: In a real custom editor deleting from a SerializeReference array can sometimes be finicky,
            // but standard DeleteArrayElementAtIndex works in modern Unity versions.
            charProp.serializedObject.Update();
            charProp.DeleteCommand();
            charProp.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPrefabSelectionRow(string label, SerializedProperty prefabProp, string searchFolderPath)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prefabProp, new GUIContent(label));

        if (GUILayout.Button("Select", GUILayout.Width(70)))
        {
            int controlID = EditorGUIUtility.GetControlID(FocusType.Passive);
            // By showing the object picker we can set it to search for GameObjects
            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "", controlID);

            // Using ProjectBrowser is difficult through scripts, but we can set the search string to point inside the folder
            // An advanced version would open a custom window. For simplicity, we use Unity's Object Picker but you'd
            // typically have to manually narrow it to the folder. Since Unity's default picker doesn't natively support
            // folder-restricting via API easily, a common trick is to prefix search with the folder name or use Search filter.
            // A more robust approach for exact folder picking requires a custom EditorWindow.

            // Since the user asked specifically to "open Minion folder" and "Select", another way is to focus the project window
            // on that folder, but assigning still requires drag and drop.
            // Let's use a custom Picker Window that we define next.
            PrefabPickerWindow.ShowPicker(searchFolderPath, (selectedPrefab) =>
            {
                prefabProp.objectReferenceValue = selectedPrefab;
                prefabProp.serializedObject.ApplyModifiedProperties();
            });
        }
        EditorGUILayout.EndHorizontal();
    }
}

public class PrefabPickerWindow : EditorWindow
{
    private string folderPath;
    private Action<GameObject> onSelect;
    private List<GameObject> prefabsInFolder = new List<GameObject>();
    private Vector2 scrollPos;

    public static void ShowPicker(string folderPath, Action<GameObject> onSelectCallback)
    {
        PrefabPickerWindow window = GetWindow<PrefabPickerWindow>("Select Prefab");
        window.folderPath = folderPath;
        window.onSelect = onSelectCallback;
        window.LoadPrefabs();
        window.Show();
    }

    private void LoadPrefabs()
    {
        prefabsInFolder.Clear();
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning($"[PrefabPicker] Folder path '{folderPath}' is invalid.");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:GameObject", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (obj != null)
            {
                prefabsInFolder.Add(obj);
            }
        }
    }

    private void OnGUI()
    {
        GUILayout.Label($"Select Prefab from: {folderPath}", EditorStyles.boldLabel);

        if (prefabsInFolder.Count == 0)
        {
            GUILayout.Label("No prefabs found in this folder.", EditorStyles.helpBox);
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (GameObject prefab in prefabsInFolder)
        {
            if (GUILayout.Button(prefab.name, GUILayout.Height(30)))
            {
                onSelect?.Invoke(prefab);
                Close();
            }
        }

        EditorGUILayout.EndScrollView();
    }
}

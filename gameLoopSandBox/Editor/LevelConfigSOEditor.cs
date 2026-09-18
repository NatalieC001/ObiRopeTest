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
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pathsFolderPath"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ObservationEscapeFolderPath"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Waves", EditorStyles.boldLabel);

        SerializedProperty wavesProp = serializedObject.FindProperty("waves");
        for (int i = 0; i < wavesProp.arraySize; i++)
        {
            SerializedProperty waveProp = wavesProp.GetArrayElementAtIndex(i);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Wave {i + 1}", EditorStyles.boldLabel);

            // --- Announcement / Narrative Fields ---
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField("Announcement (shown to player during pre-wave countdown)", EditorStyles.miniLabel);

            SerializedProperty waveNameProp = waveProp.FindPropertyRelative("waveName");
            EditorGUILayout.PropertyField(waveNameProp, new GUIContent("Wave Name",
                "Short title shown on the big announcer. e.g. 'The Siege Begins'. Leave blank for plain 'Wave 2/3'."));

            SerializedProperty announcementProp = waveProp.FindPropertyRelative("waveAnnouncementText");
            EditorGUILayout.LabelField(new GUIContent("Announcement Text",
                "Optional extra line shown under the wave name. e.g. 'Enemies incoming from all sides!'"), EditorStyles.miniLabel);
            announcementProp.stringValue = EditorGUILayout.TextArea(
                announcementProp.stringValue,
                GUILayout.MinHeight(40), GUILayout.MaxHeight(80));

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // --- Wave Rules ---
            EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("progressionType"), new GUIContent("Wave Type"));
            if ((WaveProgressionType)waveProp.FindPropertyRelative("progressionType").enumValueIndex == WaveProgressionType.TimeBased)
            {
                EditorGUILayout.PropertyField(waveProp.FindPropertyRelative("waveDuration"), new GUIContent("Duration (seconds)"));
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
                EditorGUILayout.EndVertical();
                break;
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
            DrawPrefabSelectionRow("Prefab", charProp.FindPropertyRelative("prefab"), config.minionsFolderPath, config);

            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnPointPrefab"), new GUIContent("Spawn Point (Prefab)"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnPositionOffset"), new GUIContent("Position Offset"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnDelay"), new GUIContent("Spawn Delay"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("requiredArrowElement"), new GUIContent("Element"));

            SerializedProperty movementTypeProp = charProp.FindPropertyRelative("movementType");
            EditorGUILayout.PropertyField(movementTypeProp, new GUIContent("Movement Type"));

            TargetMovementType currentMoveType = (TargetMovementType)movementTypeProp.enumValueIndex;
            if (currentMoveType == TargetMovementType.SplinePathAsset)
            {
                DrawPrefabSelectionRow("Movement Asset (Prefab)", charProp.FindPropertyRelative("movementAssetPrefab"), config.pathsFolderPath, config);
            }
        }
        else if (typeName.Contains("BossConfig"))
        {
            EditorGUILayout.LabelField($"Character {index + 1}: Big Boss", EditorStyles.boldLabel);
            DrawPrefabSelectionRow("Prefab", charProp.FindPropertyRelative("prefab"), config.bossesFolderPath, config);

            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnPointPrefab"), new GUIContent("Spawn Point (Prefab)"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnPositionOffset"), new GUIContent("Position Offset"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("spawnDelay"), new GUIContent("Spawn Delay"));
            EditorGUILayout.PropertyField(charProp.FindPropertyRelative("requiredArrowElement"), new GUIContent("Element"));

            DrawPrefabList("Observation Paths", charProp.FindPropertyRelative("observationPathPrefabs"), config.ObservationEscapeFolderPath, config);
            DrawPrefabList("Escape Paths", charProp.FindPropertyRelative("escapePathPrefabs"), config.ObservationEscapeFolderPath, config);
        }
        else
        {
            EditorGUILayout.LabelField($"Character {index + 1}: Unknown", EditorStyles.boldLabel);
        }

        // Add a remove button for the character
        if (GUILayout.Button("Remove Character", GUILayout.Width(150)))
        {
            // Find the character's parent wave to remove correctly
            string path = charProp.propertyPath;
            string parentArrayPath = path.Substring(0, path.LastIndexOf(".Array.data["));
            SerializedProperty charactersProp = charProp.serializedObject.FindProperty(parentArrayPath);

            charactersProp.serializedObject.Update();
            charactersProp.DeleteArrayElementAtIndex(index);
            charactersProp.serializedObject.ApplyModifiedProperties();
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPrefabList(string listLabel, SerializedProperty listProp, string searchFolderPath, LevelConfigSO config)
    {
        EditorGUILayout.Space();

        // Check if list is completely empty
        bool isEmpty = listProp.arraySize == 0;
        if (isEmpty)
        {
            GUI.backgroundColor = Color.red;
        }

        EditorGUILayout.LabelField(listLabel, EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.white; // Reset immediately for contents

        // Standard Unity list size control
        int newSize = EditorGUILayout.IntField("Size", listProp.arraySize);
        if (newSize < 0) newSize = 0;
        if (newSize != listProp.arraySize)
        {
            listProp.arraySize = newSize;
        }

        for (int i = 0; i < listProp.arraySize; i++)
        {
            SerializedProperty elementProp = listProp.GetArrayElementAtIndex(i);

            // Check if this specific element is null/missing
            bool isMissing = elementProp.objectReferenceValue == null;
            if (isMissing)
            {
                GUI.backgroundColor = Color.red;
            }

            EditorGUILayout.BeginHorizontal("box"); // Add box to apply background color
            GUI.backgroundColor = Color.white; // Reset immediately so only background is red
            EditorGUILayout.PropertyField(elementProp, new GUIContent($"Element {i}"));

            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                string propPath = elementProp.propertyPath;
                PrefabPickerWindow.ShowPicker(searchFolderPath, (selectedPrefab) =>
                {
                    SerializedObject serializedObj = new SerializedObject(config);
                    serializedObj.Update();
                    SerializedProperty propToUpdate = serializedObj.FindProperty(propPath);
                    if (propToUpdate != null)
                    {
                        propToUpdate.objectReferenceValue = selectedPrefab;
                        serializedObj.ApplyModifiedProperties();
                    }
                });
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawPrefabSelectionRow(string label, SerializedProperty prefabProp, string searchFolderPath, LevelConfigSO config)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(prefabProp, new GUIContent(label));

        if (GUILayout.Button("Select", GUILayout.Width(70)))
        {
            // Store the property path to lookup later in the callback, avoiding SerializedProperty expiration
            string propPath = prefabProp.propertyPath;

            PrefabPickerWindow.ShowPicker(searchFolderPath, (selectedPrefab) =>
            {
                SerializedObject serializedObj = new SerializedObject(config);
                serializedObj.Update();
                SerializedProperty propToUpdate = serializedObj.FindProperty(propPath);
                if (propToUpdate != null)
                {
                    propToUpdate.objectReferenceValue = selectedPrefab;
                    serializedObj.ApplyModifiedProperties();
                }
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

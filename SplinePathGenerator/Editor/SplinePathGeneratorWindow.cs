using UnityEngine;
using UnityEditor;
using Dreamteck.Splines;
using System.Collections.Generic;

/// <summary>
/// A custom editor window for generating mathematical spline shapes for Dreamteck Splines.
/// It creates a temporary preview object in the scene and can save it as a prefab.
/// </summary>
public class SplinePathGeneratorWindow : EditorWindow
{
    public enum ShapeType
    {
        Star,
        RegularPolygon,
        SimpleCelticKnot,
        Lissajous,
        Spiral,
        Spirograph
    }

    // --- Shape Settings ---
    private ShapeType currentShape = ShapeType.Star;
    private float outerRadius = 5f;
    private float innerRadius = 2.5f; // Used for stars
    private int points = 5; // Used for stars, polygons
    private int resolution = 20; // Used for more complex curves

    // --- Metadata Settings (PathSpawnInfo) ---
    private GameObject enemyPrefab;
    private int spawnCount = 1;
    private float movementSpeed = 5f;

    // --- Preview Object ---
    private GameObject previewRootObject;
    private GameObject previewSplineObject;
    private SplineComputer previewSpline;
    private PathSpawnInfo previewInfo;

    private const string PREFAB_SAVE_PATH = "Assets/SplinePathGenerator/GeneratedPaths/";

    [MenuItem("Tools/Spline Path Generator")]
    public static void ShowWindow()
    {
        GetWindow<SplinePathGeneratorWindow>("Path Generator");
    }

    private void OnEnable()
    {
        // Subscribe to SceneView to draw gizmos
        SceneView.duringSceneGui += OnSceneGUI;
        CreateOrUpdatePreview();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        if (previewRootObject != null)
        {
            DestroyImmediate(previewRootObject);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Shape Generation Settings", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        currentShape = (ShapeType)EditorGUILayout.EnumPopup("Shape Type", currentShape);

        switch (currentShape)
        {
            case ShapeType.Star:
                points = EditorGUILayout.IntSlider("Number of Points", points, 3, 20);
                outerRadius = EditorGUILayout.FloatField("Outer Radius", outerRadius);
                innerRadius = EditorGUILayout.FloatField("Inner Radius", innerRadius);
                break;

            case ShapeType.RegularPolygon:
                points = EditorGUILayout.IntSlider("Number of Sides", points, 3, 20);
                outerRadius = EditorGUILayout.FloatField("Radius", outerRadius);
                break;

            case ShapeType.SimpleCelticKnot:
                resolution = EditorGUILayout.IntSlider("Resolution", resolution, 20, 100);
                outerRadius = EditorGUILayout.FloatField("Size", outerRadius);
                break;

            case ShapeType.Lissajous:
                points = EditorGUILayout.IntSlider("A (Freq X)", points, 1, 10);
                resolution = EditorGUILayout.IntSlider("B (Freq Y)", resolution, 1, 10);
                outerRadius = EditorGUILayout.FloatField("Scale", outerRadius);
                break;

            case ShapeType.Spiral:
                resolution = EditorGUILayout.IntSlider("Turns", resolution, 1, 10);
                outerRadius = EditorGUILayout.FloatField("Max Radius", outerRadius);
                points = EditorGUILayout.IntSlider("Points per Turn", points, 10, 50);
                break;

            case ShapeType.Spirograph:
                outerRadius = EditorGUILayout.FloatField("Outer Circle Radius (R)", outerRadius);
                innerRadius = EditorGUILayout.FloatField("Inner Circle Radius (r)", innerRadius);
                points = EditorGUILayout.IntSlider("Pen Offset (d)", points, 1, 20);
                resolution = EditorGUILayout.IntSlider("Resolution", resolution, 50, 300);
                break;
        }

        GUILayout.Space(15);
        GUILayout.Label("Path Metadata Settings", EditorStyles.boldLabel);

        enemyPrefab = (GameObject)EditorGUILayout.ObjectField("Enemy Prefab", enemyPrefab, typeof(GameObject), false);
        spawnCount = EditorGUILayout.IntField("Spawn Count", spawnCount);
        if (spawnCount < 1) spawnCount = 1;
        movementSpeed = EditorGUILayout.FloatField("Movement Speed", movementSpeed);

        if (EditorGUI.EndChangeCheck())
        {
            CreateOrUpdatePreview();
        }

        GUILayout.Space(20);

        if (GUILayout.Button("Generate and Save Prefab", GUILayout.Height(40)))
        {
            SavePrefab();
        }
    }

    private void CreateOrUpdatePreview()
    {
        if (previewRootObject == null)
        {
            // The Root Object (Holds Metadata, does NOT hold SplineComputer)
            previewRootObject = new GameObject("Spline_Preview");
            previewRootObject.hideFlags = HideFlags.DontSave;
            previewInfo = previewRootObject.AddComponent<PathSpawnInfo>();

            // The Child Object (Holds SplineComputer)
            previewSplineObject = new GameObject("Spline_Curve");
            previewSplineObject.transform.SetParent(previewRootObject.transform);
            previewSpline = previewSplineObject.AddComponent<SplineComputer>();
        }

        // Update Metadata
        previewInfo.enemyPrefab = enemyPrefab;
        previewInfo.spawnCount = spawnCount;
        previewInfo.movementSpeed = movementSpeed;

        // Define if it is closed based on shape
        bool isClosed = (currentShape != ShapeType.Spiral);
        previewInfo.isClosed = isClosed;

        // Auto-attach Swarm Manager for standard baked paths
        if (previewRootObject.GetComponent<SplineSwarmManager>() == null)
        {
            previewRootObject.AddComponent<SplineSwarmManager>();
        }

        // Generate points
        SplinePoint[] splinePoints = GeneratePoints();

        // Apply to spline
        previewSpline.type = Spline.Type.BSpline;
        previewSpline.SetPoints(splinePoints);

        if (isClosed)
        {
            previewSpline.Close();
        }
        else
        {
            previewSpline.Break();
        }

        // Rebuild spline so we can evaluate it immediately
        previewSpline.RebuildImmediate();

        // Update Baked Enemies
        UpdateBakedEnemies();

        // Force scene redraw
        SceneView.RepaintAll();
    }

    private void UpdateBakedEnemies()
    {
        if (previewRootObject == null || previewSpline == null) return;

        // 1. Clean up existing baked enemies
        for (int i = previewRootObject.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = previewRootObject.transform.GetChild(i);
            if (child.gameObject != previewSplineObject)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // 2. Spawn new enemies if a prefab is set
        if (enemyPrefab != null && spawnCount > 0)
        {
            double percentStep = 1.0 / spawnCount;
            for (int i = 0; i < spawnCount; i++)
            {
                double percent = percentStep * i;

                // Instantiate the enemy safely in the editor
                GameObject enemyObj = null;
                if (PrefabUtility.IsPartOfPrefabAsset(enemyPrefab))
                {
                    enemyObj = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
                }
                else
                {
                    enemyObj = Instantiate(enemyPrefab);
                }

                if (enemyObj == null) continue;

                enemyObj.transform.SetParent(previewRootObject.transform);
                enemyObj.name = $"{enemyPrefab.name}_{i}";

                // Evaluate position and rotation on the spline for immediate visual feedback
                SplineSample sample = previewSpline.Evaluate(percent);
                enemyObj.transform.position = sample.position;
                enemyObj.transform.rotation = sample.rotation;

                // Add standard Dreamteck follower component to bake the movement
                SplineFollower follower = enemyObj.GetComponent<SplineFollower>();
                if (follower == null)
                {
                    follower = enemyObj.AddComponent<SplineFollower>();
                }

                follower.spline = previewSpline;
                follower.followSpeed = movementSpeed;
                follower.wrapMode = previewInfo.isClosed ? SplineFollower.Wrap.Loop : SplineFollower.Wrap.Default;
                follower.SetPercent(percent);
            }
        }
    }

    private SplinePoint[] GeneratePoints()
    {
        List<SplinePoint> pointsList = new List<SplinePoint>();

        // Note: We generate points on the XY plane (vertical) rather than XZ plane (flat).
        // This ensures the shapes stand perfectly upright, facing the player when spawned.
        switch (currentShape)
        {
            case ShapeType.Star:
                for (int i = 0; i < points * 2; i++)
                {
                    float radius = (i % 2 == 0) ? outerRadius : innerRadius;
                    float angle = i * Mathf.PI / points;
                    Vector3 pos = new Vector3(Mathf.Sin(angle) * radius, Mathf.Cos(angle) * radius, 0);
                    pointsList.Add(new SplinePoint(pos));
                }
                break;

            case ShapeType.RegularPolygon:
                for (int i = 0; i < points; i++)
                {
                    float angle = i * 2 * Mathf.PI / points;
                    Vector3 pos = new Vector3(Mathf.Sin(angle) * outerRadius, Mathf.Cos(angle) * outerRadius, 0);
                    pointsList.Add(new SplinePoint(pos));
                }
                break;

            case ShapeType.SimpleCelticKnot:
                // Using a 3-lobed knot (trefoil knot variant projected to 2D)
                for (int i = 0; i < resolution; i++)
                {
                    float t = i * 2 * Mathf.PI / resolution;
                    float x = Mathf.Sin(t) + 2 * Mathf.Sin(2 * t);
                    float y = Mathf.Cos(t) - 2 * Mathf.Cos(2 * t);
                    Vector3 pos = new Vector3(x, y, 0) * (outerRadius / 3f);
                    pointsList.Add(new SplinePoint(pos));
                }
                break;

            case ShapeType.Lissajous:
                // points = A, resolution = B
                int lissajousSteps = 50;
                for (int i = 0; i < lissajousSteps; i++)
                {
                    float t = i * 2 * Mathf.PI / lissajousSteps;
                    float x = Mathf.Sin(points * t);
                    float y = Mathf.Sin(resolution * t);
                    Vector3 pos = new Vector3(x, y, 0) * outerRadius;
                    pointsList.Add(new SplinePoint(pos));
                }
                break;

            case ShapeType.Spiral:
                // resolution = turns, points = points per turn
                int totalPoints = resolution * points;
                for (int i = 0; i < totalPoints; i++)
                {
                    float t = i / (float)(points); // Turns
                    float currentRadius = (t / resolution) * outerRadius;
                    float angle = t * 2 * Mathf.PI;
                    Vector3 pos = new Vector3(Mathf.Sin(angle) * currentRadius, Mathf.Cos(angle) * currentRadius, 0);
                    pointsList.Add(new SplinePoint(pos));
                }
                break;

            case ShapeType.Spirograph:
                // R = outerRadius, r = innerRadius, d = points
                for (int i = 0; i < resolution; i++)
                {
                    float t = i * 10 * Mathf.PI / resolution; // Multiplied by 10 to get multiple loops
                    float R = outerRadius;
                    float r = innerRadius;
                    float d = points;

                    float x = (R - r) * Mathf.Cos(t) + d * Mathf.Cos((R - r) / r * t);
                    float y = (R - r) * Mathf.Sin(t) - d * Mathf.Sin((R - r) / r * t);

                    Vector3 pos = new Vector3(x, y, 0);
                    pointsList.Add(new SplinePoint(pos));
                }
                break;
        }

        return pointsList.ToArray();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (previewSpline == null || previewInfo == null) return;

        // If the path is open, clearly label START and END
        if (!previewInfo.isClosed && previewSpline.pointCount > 0)
        {
            SplinePoint[] pts = previewSpline.GetPoints();

            // Draw START
            Vector3 startPos = pts[0].position;
            Handles.color = Color.green;
            Handles.Label(startPos + Vector3.up * 0.5f, "START", EditorStyles.boldLabel);
            Handles.DrawSolidDisc(startPos, Vector3.up, 0.2f);

            // Draw END
            Vector3 endPos = pts[pts.Length - 1].position;
            Handles.color = Color.red;
            Handles.Label(endPos + Vector3.up * 0.5f, "END", EditorStyles.boldLabel);
            Handles.DrawSolidDisc(endPos, Vector3.up, 0.2f);
        }
    }

    private void SavePrefab()
    {
        if (previewRootObject == null) return;

        if (!System.IO.Directory.Exists(PREFAB_SAVE_PATH))
        {
            System.IO.Directory.CreateDirectory(PREFAB_SAVE_PATH);
            AssetDatabase.Refresh();
        }

        string fileName = $"{currentShape}_Path_{System.DateTime.Now:yyyyMMdd_HHmmss}.prefab";
        string fullPath = PREFAB_SAVE_PATH + fileName;

        // Remove DontSave flags before saving
        previewRootObject.hideFlags = HideFlags.None;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(previewRootObject, fullPath);

        if (prefab != null)
        {
            Debug.Log($"[Spline Path Generator] Saved new path prefab at {fullPath}");
        }
        else
        {
            Debug.LogError("[Spline Path Generator] Failed to save prefab.");
        }

        // Restore preview flags
        previewRootObject.hideFlags = HideFlags.DontSave;
    }
}

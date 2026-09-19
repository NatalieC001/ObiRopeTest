using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DragonBrainMonitorWindow : EditorWindow
{
    private BossCreature targetBoss;
    private DesireEvaluator evaluator;
    private EnvironmentTagRegistry registry;
    private MinionRequestBroker spawner;

    private DesireResult latestResult;

    [MenuItem("Archery Range/Dragon Brain Monitor")]
    public static void ShowWindow()
    {
        DragonBrainMonitorWindow window = GetWindow<DragonBrainMonitorWindow>("Dragon Brain Monitor");
        window.minSize = new Vector2(600, 400);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += Repaint;
    }

    private void OnDisable()
    {
        EditorApplication.update -= Repaint;
    }

    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to monitor the Dragon Brain.", MessageType.Info);
            return;
        }

        if (targetBoss == null) targetBoss = FindFirstObjectByType<BossCreature>();
        if (evaluator == null) evaluator = FindFirstObjectByType<DesireEvaluator>();
        if (registry == null) registry = FindFirstObjectByType<EnvironmentTagRegistry>();
        if (spawner == null) spawner = FindFirstObjectByType<MinionRequestBroker>();

        if (targetBoss == null || evaluator == null)
        {
            EditorGUILayout.HelpBox("Waiting for BossCreature and DesireEvaluator to spawn...", MessageType.Warning);
            return;
        }

        // Perform a silent evaluation to get the raw math for the bars
        if (registry != null)
        {
            latestResult = evaluator.Evaluate(targetBoss, registry);
        }

        EditorGUILayout.BeginHorizontal();

        // ------------------ LEFT PANEL: STATE ------------------
        EditorGUILayout.BeginVertical("box", GUILayout.Width(200));
        GUILayout.Label("PHYSICAL STATE", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        GUI.color = Color.cyan;
        GUILayout.Label($"PHASE: {targetBoss.currentPhase}", new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 });
        GUI.color = Color.white;

        EditorGUILayout.Space();

        // Use a visual slider for health/stamina to look more like bars
        EditorGUILayout.LabelField("Health");
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), targetBoss.GetCurrentHealthPct(), "");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stamina");
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), targetBoss.maxStamina > 0 ? targetBoss.currentStamina / targetBoss.maxStamina : 0, "");

        EditorGUILayout.Space();
        if (spawner != null)
        {
            GUILayout.Label($"Minion Spawner Linked", EditorStyles.boldLabel);
        }

        if (targetBoss.LastThreatenedCrystal != null)
        {
            GUI.color = Color.red;
            GUILayout.Label($"ALERT: Crystal Threatened!", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        EditorGUILayout.EndVertical();

        // ------------------ MIDDLE PANEL: DESIRES ------------------
        EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
        GUILayout.Label("DESIRE INFLUENCE (MATH)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (latestResult != null)
        {
            // Calculate a color mapping from Red -> Orange -> Yellow -> Green
            Color urgencyColor = EvaluateColor(latestResult.Urgency);
            GUI.color = urgencyColor;

            GUILayout.Label(latestResult.StrongestDesire.ToString().ToUpper(), new GUIStyle(EditorStyles.boldLabel) { fontSize = 24 });
            GUI.color = Color.white;

            EditorGUILayout.Space();


            if (latestResult.TargetTransform != null)
            {
                GUILayout.Label($"Target Focus: {latestResult.TargetTransform.name}");
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    /// <summary>
    /// Returns Red for low (0.0), Orange (0.33), Yellow (0.66), Green for high (1.0)
    /// </summary>
    private Color EvaluateColor(float value)
    {
        value = Mathf.Clamp01(value);
        if (value <= 0.33f) return Color.Lerp(Color.red, new Color(1f, 0.5f, 0f), value / 0.33f); // Red to Orange
        if (value <= 0.66f) return Color.Lerp(new Color(1f, 0.5f, 0f), Color.yellow, (value - 0.33f) / 0.33f); // Orange to Yellow
        return Color.Lerp(Color.yellow, Color.green, (value - 0.66f) / 0.34f); // Yellow to Green
    }
}

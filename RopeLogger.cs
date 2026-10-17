using UnityEngine;

public static class RopeLogger
{
    private const string Prefix = "[RopeLogger]";

    public static void Step(string stepName, string detail)
    {
        Debug.Log($"{Prefix} [STEP: {stepName}] {detail}");
    }

    public static void Warn(string stepName, string detail)
    {
        Debug.LogWarning($"{Prefix} [WARN: {stepName}] {detail}");
    }

    public static void Fail(string stepName, string detail)
    {
        Debug.LogError($"{Prefix} [FAIL: {stepName}] {detail}");
    }
}

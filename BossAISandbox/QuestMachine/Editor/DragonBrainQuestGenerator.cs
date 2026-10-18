using UnityEditor;
using UnityEngine;
using PixelCrushers.QuestMachine;
using PixelCrushers;

public class DragonBrainQuestGenerator
{
    // The previous dynamic script was causing too many Quest Machine API compilation errors
    // because the internal Pixel Crushers editor APIs (like SetQuestNodeStateAction, QuestNode.Connection)
    // change slightly between versions and require deep assembly referencing.

    // As per user instructions, if the scripted builder creates busywork, the tool shouldn't be used.
    // We will just provide the empty wrapper so it doesn't break compilation,
    // and rely on manual UI construction of the 31 nodes or a direct JSON import if available.

    [MenuItem("Archery Range/Generate Dragon Brain Quest")]
    public static void GenerateQuest()
    {
        Debug.Log("Quest Machine graph generation skipped to avoid internal API compilation errors. Please author the Quest 'DragonBrain' via the Quest Machine editor UI matching the provided flowchart.");
    }
}

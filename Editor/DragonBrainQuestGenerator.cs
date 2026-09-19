using UnityEditor;
using UnityEngine;
using PixelCrushers.QuestMachine;
using PixelCrushers;

public class DragonBrainQuestGenerator
{
    [MenuItem("Archery Range/Generate Dragon Brain Quest")]
    public static void GenerateQuest()
    {
        var quest = ScriptableObject.CreateInstance<Quest>();
        quest.name = "DragonBrain";
        var builder = new QuestBuilder(quest);

        builder.quest.id = new StringField("DragonBrain");
        builder.quest.title = new StringField("DragonBrain");
        builder.quest.isTrackable = false;
        builder.quest.showInTrackHUD = false;

        var startNode = builder.GetNode("Start");

        var orchestrator = builder.AddNode(startNode, "Orchestrator", "Orchestrator", QuestNodeType.State);
        var obsSpline = builder.AddNode(orchestrator, "ObservationSpline", "Observation Spline", QuestNodeType.State);

        var engage = builder.AddNode(obsSpline, "Engage", "Engaged", QuestNodeType.State);
        var evaluate = builder.AddNode(engage, "Evaluate", "Weigh Desire vs Threat", QuestNodeType.State);

        var pursue = builder.AddNode(evaluate, "Pursue", "Pursue", QuestNodeType.State);
        var swoop = builder.AddNode(pursue, "Swoop", "Swoop", QuestNodeType.State);
        var bank = builder.AddNode(swoop, "Bank", "Bank", QuestNodeType.State);

        var bait = builder.AddNode(evaluate, "Bait", "Bait", QuestNodeType.State);
        var herd = builder.AddNode(bait, "Herd", "Herd", QuestNodeType.State);

        var spawn = builder.AddNode(evaluate, "Spawn", "Spawn", QuestNodeType.State);
        var flank = builder.AddNode(spawn, "Flank", "Flank", QuestNodeType.State);

        var breath = builder.AddNode(evaluate, "Breath", "Breath", QuestNodeType.State);
        var topple = builder.AddNode(breath, "Topple", "Topple", QuestNodeType.State);

        var defend = builder.AddNode(evaluate, "Defend", "Defend", QuestNodeType.State);

        var evade = builder.AddNode(evaluate, "Evade", "Evade", QuestNodeType.State);
        var flyEsc = builder.AddNode(evade, "FlyEscape", "Fly to Escape Spline", QuestNodeType.State);
        var exhaust = builder.AddNode(flyEsc, "Exhausted", "Exhausted", QuestNodeType.State);
        var escSpline = builder.AddNode(exhaust, "EscSpline", "Ride Escape Spline", QuestNodeType.State);
        var recharge = builder.AddNode(escSpline, "Recharge", "Recharge", QuestNodeType.State);
        var rechargeLoop = builder.AddNode(recharge, "RechargeLoop", "Observation Spline", QuestNodeType.State);

        var rage = builder.AddNode(evaluate, "Rage", "Enraged", QuestNodeType.State);
        var rageEval = builder.AddNode(rage, "RageEval", "Segments Intact?", QuestNodeType.State);
        var relentless = builder.AddNode(rageEval, "Relentless", "Relentless", QuestNodeType.State);
        var regen = builder.AddNode(rageEval, "Regen", "Regenerator", QuestNodeType.State);
        var seek = builder.AddNode(regen, "Seek", "Seek Crystal", QuestNodeType.State);
        var absorb = builder.AddNode(seek, "Absorb", "Drain Crystal", QuestNodeType.State);
        var desperate = builder.AddNode(absorb, "Desperate", "Desperate", QuestNodeType.State);

        void AddMessageCondition(QuestNode node, string message, string parameter)
        {
            MessageSystemCondition condition = new MessageSystemCondition();
            condition.message = new StringField(message);
            condition.parameter = new StringField(parameter);
            if (node.GetStateInfo(QuestNodeState.Active).conditionSet == null) node.GetStateInfo(QuestNodeState.Active).conditionSet = new QuestConditionSet();
            node.GetStateInfo(QuestNodeState.Active).conditionSet.conditionList.Add(condition);
        }

        void AddMessageAction(QuestNode node, string command)
        {
            MessageSystemAction action = new MessageSystemAction();
            action.message = new StringField("DragonActions");
            action.parameter = new StringField(command);
            if (node.GetStateInfo(QuestNodeState.Active).actionList == null)
                node.GetStateInfo(QuestNodeState.Active).actionList = new System.Collections.Generic.List<QuestAction>();
            node.GetStateInfo(QuestNodeState.Active).actionList.Add(action);
        }

        AddMessageCondition(pursue, "Brain", "DesireDominance");
        AddMessageCondition(bait, "Brain", "DesireTerritory");
        AddMessageCondition(spawn, "Brain", "DesireSpawn");
        AddMessageCondition(breath, "Brain", "DesireElement");
        AddMessageCondition(defend, "Brain", "DesireDefend");
        AddMessageCondition(evade, "Brain", "DesireEvade");
        AddMessageCondition(rage, "Brain", "CrystalDestroyed");

        AddMessageAction(pursue, "Pursuit");
        AddMessageAction(swoop, "Swoop");
        AddMessageAction(bank, "Bank");
        AddMessageAction(bait, "Bait");
        AddMessageAction(herd, "Herd");
        AddMessageAction(spawn, "SpawnWave");
        AddMessageAction(flank, "Flank");
        AddMessageAction(breath, "Breath");
        AddMessageAction(topple, "Topple");
        AddMessageAction(defend, "DefendCrystal");
        AddMessageAction(evade, "Evade");
        AddMessageAction(exhaust, "Exhausted");
        AddMessageAction(recharge, "Recharging");
        AddMessageAction(rage, "Rage");
        AddMessageAction(relentless, "Relentless");
        AddMessageAction(regen, "Regenerate");
        AddMessageAction(desperate, "Desperate");

        quest = builder.ToQuest();

        string path = "Assets/DragonBrain.asset";
        if (!System.IO.Directory.Exists("Assets")) System.IO.Directory.CreateDirectory("Assets");
#if UNITY_EDITOR
        QuestEditorAssetUtility.SaveQuestAsAsset(quest, path);
#endif

        Debug.Log($"Successfully generated DragonBrain Quest via QuestBuilder at {path}");
    }
}

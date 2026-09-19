using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using PixelCrushers;
using PixelCrushers.QuestMachine;

public class DragonBrainQuestGenerator : EditorWindow
{
    [MenuItem("Archery Range/Generate Dragon Brain Quest")]
    public static void GenerateQuest()
    {
        Quest quest = ScriptableObject.CreateInstance<Quest>();
        quest.Initialize();
        quest.id = new StringField("DragonBrain");
        quest.title = new StringField("Dragon Boss AI");

        if (quest.nodeList == null) quest.nodeList = new List<QuestNode>();
        if (quest.stateInfoList == null) quest.stateInfoList = new List<QuestStateInfo>();

        string path = "Assets/DragonBrain_Automated.asset";
        AssetDatabase.CreateAsset(quest, path);

        QuestNode CreateNode(string id, QuestNodeType type, Vector2 position)
        {
            QuestNode node = new QuestNode();
            node.id = new StringField(id);
            node.internalName = new StringField(id);
            node.canvasRect = new Rect(position.x, position.y, 150, 50);
            node.nodeType = type;

            node.stateInfoList = new List<QuestStateInfo>();
            for (int i = 0; i < 3; i++) node.stateInfoList.Add(new QuestStateInfo());

            quest.nodeList.Add(node);
            return node;
        }

        QuestNode CreateStateNode(string id, string actionTarget,
                                  string actionMessage, Vector2 position)
        {
            QuestNode node = CreateNode(id, QuestNodeType.Passthrough, position);

            if (!string.IsNullOrEmpty(actionTarget) && !string.IsNullOrEmpty(actionMessage))
            {
                var stateInfo = node.GetStateInfo(QuestNodeState.Active);
                if (stateInfo.actionList == null)
                    stateInfo.actionList = new List<QuestAction>();

                MessageQuestAction msgAction =
                    ScriptableObject.CreateInstance<MessageQuestAction>();
                msgAction.senderID = StringField.empty;
                msgAction.targetID = new StringField(actionTarget);
                msgAction.message = new StringField(actionMessage);
                msgAction.parameter = new StringField("");

                stateInfo.actionList.Add(msgAction);
                AssetDatabase.AddObjectToAsset(msgAction, quest);
            }
            return node;
        }

        void ConnectWithCondition(QuestNode source, QuestNode dest,
                                  string msgTarget, string msgString)
        {
            int childIndex = quest.nodeList.IndexOf(dest);
            if (childIndex >= 0)
            {
                if (source.childIndexList == null)
                    source.childIndexList = new List<int>();
                source.childIndexList.Add(childIndex);
            }

            if (!string.IsNullOrEmpty(msgTarget) && !string.IsNullOrEmpty(msgString))
            {
                if (dest.conditionSet == null)
                    dest.conditionSet = new QuestConditionSet();
                if (dest.conditionSet.conditionList == null)
                    dest.conditionSet.conditionList = new List<QuestCondition>();

                if (dest.conditionSet.conditionList.Count > 0)
                    dest.conditionSet.conditionCountMode = ConditionCountMode.Any;

                MessageQuestCondition msgCondition =
                    ScriptableObject.CreateInstance<MessageQuestCondition>();
                msgCondition.senderID = StringField.empty;
                msgCondition.targetID = new StringField(msgTarget);
                msgCondition.message = new StringField(msgString);
                msgCondition.parameter = new StringField("");

                dest.conditionSet.conditionList.Add(msgCondition);
                AssetDatabase.AddObjectToAsset(msgCondition, quest);
            }
        }

        QuestNode startNode = CreateNode("Start", QuestNodeType.Start, new Vector2(800, 50));
        QuestNode orchNode = CreateStateNode("Orchestrator", "", "", new Vector2(800, 150));

        QuestNode engageHub = CreateStateNode("Engaged", "DragonActions", "Weigh Desire vs Threat", new Vector2(800, 300));
        QuestNode exhaustHub = CreateStateNode("Exhausted", "DragonActions", "Ride Escape Spline", new Vector2(800, 600));
        QuestNode rageHub = CreateStateNode("Enraged", "DragonActions", "Rage", new Vector2(1400, 450));
        QuestNode rechargeHub = CreateStateNode("Recharging", "DragonActions", "Recharging", new Vector2(800, 750));

        QuestNode obsNode = CreateStateNode("Observation Spline", "", "", new Vector2(1100, 150));
        QuestNode pursueNode = CreateStateNode("Pursue", "DragonActions", "Pursuit", new Vector2(50, 450));
        QuestNode swoopNode = CreateStateNode("Swoop", "DragonActions", "Swoop", new Vector2(50, 600));
        QuestNode bankNode = CreateStateNode("Bank", "DragonActions", "Bank", new Vector2(50, 750));
        QuestNode baitNode = CreateStateNode("Bait", "DragonActions", "Bait", new Vector2(250, 450));
        QuestNode herdNode = CreateStateNode("Herd", "DragonActions", "Herd", new Vector2(250, 600));
        QuestNode spawnNode = CreateStateNode("Spawn", "DragonActions", "SpawnWave", new Vector2(450, 450));
        QuestNode flankNode = CreateStateNode("Flank", "DragonActions", "Flank", new Vector2(450, 600));
        QuestNode breathNode = CreateStateNode("Breath", "DragonActions", "Breath", new Vector2(650, 450));
        QuestNode toppleNode = CreateStateNode("Topple", "DragonActions", "Topple", new Vector2(650, 600));
        QuestNode defendNode = CreateStateNode("Defender", "DragonActions", "DefendCrystal", new Vector2(1000, 450));
        QuestNode evadeNode = CreateStateNode("Evade", "DragonActions", "Evade", new Vector2(1200, 450));
        QuestNode flyEscNode = CreateStateNode("Glide to Escape", "DragonActions", "Evade", new Vector2(1200, 600));
        QuestNode escSplineNode = CreateStateNode("EscSpline", "DragonActions", "Ride Escape Spline", new Vector2(800, 900));
        QuestNode rechargeLoopNode = CreateStateNode("RechargeLoop", "DragonActions", "Observation Spline - Regen Stamina", new Vector2(800, 1050));
        QuestNode relentlessNode = CreateStateNode("Relentless", "DragonActions", "Relentless", new Vector2(1400, 600));
        QuestNode regenNode = CreateStateNode("Regenerator", "DragonActions", "Regenerate", new Vector2(1600, 600));
        QuestNode seekNode = CreateStateNode("Seek", "DragonActions", "Fly to Nearest Crystal", new Vector2(1600, 750));
        QuestNode absorbNode = CreateStateNode("Absorb", "DragonActions", "Drain Crystal", new Vector2(1600, 900));
        QuestNode desperateNode = CreateStateNode("Desperate", "DragonActions", "Desperate", new Vector2(1800, 900));

        ConnectWithCondition(startNode, orchNode, "", "");
        ConnectWithCondition(orchNode, obsNode, "Brain", "Circle + Spawn Minions");
        ConnectWithCondition(obsNode, engageHub, "Brain", "PlayerShootsBoss");
        ConnectWithCondition(obsNode, engageHub, "Brain", "Minions beaten / Time");
        ConnectWithCondition(obsNode, rageHub, "Brain", "Crystal destroyed");
        ConnectWithCondition(engageHub, pursueNode, "Brain", "DesireDominance");
        ConnectWithCondition(engageHub, baitNode, "Brain", "DesireTerritory");
        ConnectWithCondition(engageHub, spawnNode, "Brain", "DesireSpawn");
        ConnectWithCondition(engageHub, breathNode, "Brain", "DesireElement");
        ConnectWithCondition(engageHub, defendNode, "Brain", "DesireDefend");
        ConnectWithCondition(engageHub, evadeNode, "Brain", "BurstDamageTaken");
        ConnectWithCondition(engageHub, evadeNode, "Brain", "StaminaEmpty");
        ConnectWithCondition(pursueNode, swoopNode, "", "");
        ConnectWithCondition(swoopNode, bankNode, "Brain", "SwoopOvershoot");
        ConnectWithCondition(bankNode, pursueNode, "", "");
        ConnectWithCondition(baitNode, herdNode, "", "");
        ConnectWithCondition(herdNode, swoopNode, "", "");
        ConnectWithCondition(spawnNode, herdNode, "Brain", "TagChokepoint");
        ConnectWithCondition(spawnNode, flankNode, "Brain", "TagCover");
        ConnectWithCondition(breathNode, toppleNode, "", "");
        ConnectWithCondition(toppleNode, engageHub, "Brain", "ActionComplete");
        ConnectWithCondition(defendNode, engageHub, "Brain", "CrystalSafe");
        ConnectWithCondition(defendNode, rageHub, "Brain", "CrystalDestroyed");
        ConnectWithCondition(evadeNode, flyEscNode, "", "");
        ConnectWithCondition(flyEscNode, exhaustHub, "Brain", "ReachedSafeAltitude");
        ConnectWithCondition(bankNode, exhaustHub, "Brain", "StaminaDepleted");
        ConnectWithCondition(herdNode, exhaustHub, "Brain", "StaminaDepleted");
        ConnectWithCondition(flankNode, exhaustHub, "Brain", "StaminaDepleted");
        ConnectWithCondition(exhaustHub, rechargeHub, "Brain", "SafeZoneReached");
        ConnectWithCondition(rechargeHub, escSplineNode, "", "");
        ConnectWithCondition(escSplineNode, rechargeLoopNode, "Brain", "SplineEnd");
        ConnectWithCondition(rechargeLoopNode, engageHub, "Brain", "StaminaFullyCharged");
        ConnectWithCondition(rechargeLoopNode, rageHub, "Brain", "CrystalsLost");
        ConnectWithCondition(rageHub, relentlessNode, "Brain", "EnrageStart");
        ConnectWithCondition(rageHub, regenNode, "Brain", "SegmentsBreached");
        ConnectWithCondition(relentlessNode, regenNode, "Brain", "AngerMaxed");
        ConnectWithCondition(regenNode, seekNode, "", "");
        ConnectWithCondition(seekNode, absorbNode, "Brain", "AtCrystal");
        ConnectWithCondition(absorbNode, desperateNode, "Brain", "CrystalInterrupted");
        ConnectWithCondition(absorbNode, engageHub, "Brain", "RegenComplete");
        ConnectWithCondition(desperateNode, engageHub, "Brain", "DesperateTimerEnd");

        EditorUtility.SetDirty(quest);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Dragon Brain Quest built → {path}");
    }
}

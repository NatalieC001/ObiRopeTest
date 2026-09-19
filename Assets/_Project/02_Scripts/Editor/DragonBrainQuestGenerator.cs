using UnityEngine;
using UnityEditor;
using PixelCrushers;
using PixelCrushers.QuestMachine;

public class DragonBrainQuestGenerator : EditorWindow
{
    [MenuItem("Archery Range/Generate Dragon Brain Quest")]
    public static void GenerateQuest()
    {
        // 1. Create the Main Quest Asset
        Quest quest = ScriptableObject.CreateInstance<Quest>();
        quest.id = new StringField("DragonBrain");
        quest.title = new StringField("Dragon Boss AI");

        string path = "Assets/DragonBrain_Automated.asset";
        AssetDatabase.CreateAsset(quest, path);

        // --- HELPER METHODS ---

        // Helper to create and properly assign a QuestNode to the quest
        QuestNode CreateNode(string id, QuestNodeType type, Vector2 position)
        {
            QuestNode node = ScriptableObject.CreateInstance<QuestNode>();
            node.SetRuntimeReferences(quest);
            node.id = new StringField(id);
            node.internalName = new StringField(id);
            node.nodeType = type;
            node.canvasRect = new Rect(position.x, position.y, 150, 50);

            quest.nodeList.Add(node);
            AssetDatabase.AddObjectToAsset(node, quest);
            return node;
        }

        // Creates a Condition Node and adds an Action to its Active state
        QuestNode CreateStateNode(string id, string actionTarget, string actionMessage, Vector2 position)
        {
            QuestNode node = CreateNode(id, QuestNodeType.Condition, position);

            // Add the action to the Active state
            if (!string.IsNullOrEmpty(actionTarget) && !string.IsNullOrEmpty(actionMessage))
            {
                var actionList = node.GetStateInfo(QuestNodeState.Active).actionList;

                MessageQuestAction msgAction = ScriptableObject.CreateInstance<MessageQuestAction>();
                msgAction.senderID = new StringField("");
                msgAction.targetID = new StringField(actionTarget);
                msgAction.message = new StringField(actionMessage);
                msgAction.parameter = new StringField("");

                actionList.Add(msgAction);
                AssetDatabase.AddObjectToAsset(msgAction, quest);
            }
            return node;
        }

        // Connects Source to Dest using Quest Machine's childIndexList, and adds a Condition to the DESTINATION node
        void ConnectWithCondition(QuestNode source, QuestNode dest, string msgTarget, string msgString)
        {
            int childIndex = quest.nodeList.IndexOf(dest);
            if (childIndex >= 0)
            {
                source.childIndexList.Add(childIndex);
            }

            if (!string.IsNullOrEmpty(msgTarget) && !string.IsNullOrEmpty(msgString))
            {
                MessageQuestCondition msgCondition = ScriptableObject.CreateInstance<MessageQuestCondition>();
                msgCondition.senderID = new StringField("");
                msgCondition.targetID = new StringField(msgTarget);
                msgCondition.message = new StringField(msgString);
                msgCondition.parameter = new StringField("");

                dest.conditionSet.conditionList.Add(msgCondition);
                AssetDatabase.AddObjectToAsset(msgCondition, quest);
            }
        }

        // --- CREATE ALL NODES FROM GRAPH ---

        // 2. Create Start Node and assign it properly
        QuestNode startNode = CreateNode("Start", QuestNodeType.Start, new Vector2(800, 50));
        quest.startNode = startNode;

        // Hubs
        QuestNode orchNode = CreateStateNode("Orchestrator", "", "", new Vector2(800, 150));
        QuestNode engageHub = CreateStateNode("Engaged", "DragonActions", "Weigh Desire vs Threat", new Vector2(800, 300));
        QuestNode exhaustHub = CreateStateNode("Exhausted", "DragonActions", "Ride Escape Spline", new Vector2(800, 600));
        QuestNode rageHub = CreateStateNode("Enraged", "DragonActions", "Rage", new Vector2(1400, 450));
        QuestNode rechargeHub = CreateStateNode("Recharging", "DragonActions", "Recharging", new Vector2(800, 750));

        // Observation phase
        QuestNode obsNode = CreateStateNode("Observation Spline", "", "", new Vector2(1100, 150));

        // Engaged Branches
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

        // Recharging Phase
        QuestNode escSplineNode = CreateStateNode("EscSpline", "DragonActions", "Ride Escape Spline", new Vector2(800, 900));
        QuestNode rechargeLoopNode = CreateStateNode("RechargeLoop", "DragonActions", "Observation Spline - Regen Stamina", new Vector2(800, 1050));

        // Rage Phase
        QuestNode relentlessNode = CreateStateNode("Relentless", "DragonActions", "Relentless", new Vector2(1400, 600));
        QuestNode regenNode = CreateStateNode("Regenerator", "DragonActions", "Regenerate", new Vector2(1600, 600));
        QuestNode seekNode = CreateStateNode("Seek", "DragonActions", "Fly to Nearest Crystal", new Vector2(1600, 750));
        QuestNode absorbNode = CreateStateNode("Absorb", "DragonActions", "Drain Crystal", new Vector2(1600, 900));
        QuestNode desperateNode = CreateStateNode("Desperate", "DragonActions", "Desperate", new Vector2(1800, 900));


        // --- WIRE CONNECTIONS & CONDITIONS ---

        // Start -> Orchestrator
        ConnectWithCondition(startNode, orchNode, "", "");

        // Orchestrator -> Obs
        ConnectWithCondition(orchNode, obsNode, "Brain", "Circle + Spawn Minions");

        // Obs -> Engage / Rage
        ConnectWithCondition(obsNode, engageHub, "Brain", "PlayerShootsBoss");
        ConnectWithCondition(obsNode, engageHub, "Brain", "Minions beaten / Time");
        ConnectWithCondition(obsNode, rageHub, "Brain", "Crystal destroyed");

        // Engage -> Branches
        ConnectWithCondition(engageHub, pursueNode, "Brain", "DesireDominance");
        ConnectWithCondition(engageHub, baitNode, "Brain", "DesireTerritory");
        ConnectWithCondition(engageHub, spawnNode, "Brain", "DesireSpawn");
        ConnectWithCondition(engageHub, breathNode, "Brain", "DesireElement");
        ConnectWithCondition(engageHub, defendNode, "Brain", "DesireDefend");
        ConnectWithCondition(engageHub, evadeNode, "Brain", "BurstDamageTaken");
        ConnectWithCondition(engageHub, evadeNode, "Brain", "StaminaEmpty");

        // Pursue Branch
        ConnectWithCondition(pursueNode, swoopNode, "", "");
        ConnectWithCondition(swoopNode, bankNode, "Brain", "SwoopOvershoot");
        ConnectWithCondition(bankNode, pursueNode, "", "");

        // Bait Branch
        ConnectWithCondition(baitNode, herdNode, "", "");
        ConnectWithCondition(herdNode, swoopNode, "", "");

        // Spawn Branch
        ConnectWithCondition(spawnNode, herdNode, "Brain", "TagChokepoint");
        ConnectWithCondition(spawnNode, flankNode, "Brain", "TagCover");

        // Breath Branch
        ConnectWithCondition(breathNode, toppleNode, "Brain", "TagToppleObject");

        // Defender Branch
        ConnectWithCondition(defendNode, engageHub, "Brain", "Crystal safe");
        ConnectWithCondition(defendNode, rageHub, "Brain", "Crystal destroyed");

        // Evade Branch
        ConnectWithCondition(evadeNode, flyEscNode, "", "");
        ConnectWithCondition(flyEscNode, exhaustHub, "", "");

        // Global Exhaustion funnels
        ConnectWithCondition(swoopNode, exhaustHub, "Brain", "StaminaEmpty");
        ConnectWithCondition(herdNode, exhaustHub, "Brain", "StaminaEmpty");
        ConnectWithCondition(flankNode, exhaustHub, "Brain", "StaminaEmpty");
        ConnectWithCondition(toppleNode, exhaustHub, "Brain", "StaminaEmpty");

        // Exhausted Flow
        ConnectWithCondition(exhaustHub, escSplineNode, "", "");
        ConnectWithCondition(escSplineNode, rechargeHub, "", "");
        ConnectWithCondition(rechargeHub, rechargeLoopNode, "", "");

        // Recharge Loop
        ConnectWithCondition(rechargeLoopNode, engageHub, "Brain", "RechargeFull");
        ConnectWithCondition(rechargeLoopNode, rageHub, "Brain", "Crystals lost");

        // Rage Flow
        ConnectWithCondition(rageHub, relentlessNode, "Brain", "SegmentsIntact");
        ConnectWithCondition(rageHub, regenNode, "Brain", "SegmentsMissing");

        ConnectWithCondition(relentlessNode, exhaustHub, "Brain", "StaminaEmpty");

        // Regen Flow
        ConnectWithCondition(regenNode, seekNode, "", "");
        ConnectWithCondition(seekNode, absorbNode, "", "");
        ConnectWithCondition(absorbNode, engageHub, "Brain", "Segments restored");
        ConnectWithCondition(absorbNode, desperateNode, "Brain", "NoCrystalsLeft");
        ConnectWithCondition(desperateNode, relentlessNode, "", "");

        // 4. Save and Refresh
        EditorUtility.SetDirty(quest);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[DragonBrainQuestGenerator] Successfully generated automated Quest Machine brain at: {path}");
        Selection.activeObject = quest;
    }
}
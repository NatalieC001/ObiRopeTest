using UnityEditor;
using UnityEngine;
using PixelCrushers.QuestMachine;
using PixelCrushers;

public class DragonBrainQuestGenerator
{
    [MenuItem("Archery Range/Generate Dragon Brain Quest")]
    public static void GenerateQuest()
    {
        Quest quest = ScriptableObject.CreateInstance<Quest>();
        quest.name = "DragonBrain";
        quest.id = new StringField("DragonBrain");
        quest.title = new StringField("DragonBrain");
        quest.isTrackable = false;
        quest.showInTrackHUD = false;

        if (quest.nodeList == null) quest.nodeList = new System.Collections.Generic.List<QuestNode>();

        QuestNode CreateNode(string id, string internalName, QuestNode.State initialState = QuestNode.State.Inactive)
        {
            QuestNode node = new QuestNode();
            node.id = new StringField(id);
            node.internalName = new StringField(internalName);
            node.SetState(initialState);
            quest.nodeList.Add(node);
            return node;
        }

        QuestNode startNode = CreateNode("Start", "Battle Starts", QuestNode.State.Active);

        QuestNode orchestrator = CreateNode("Orchestrator", "Orchestrator");
        QuestNode obsSpline = CreateNode("ObservationSpline", "Observation Spline");

        QuestNode engage = CreateNode("Engage", "Engaged");
        QuestNode evaluate = CreateNode("Evaluate", "Weigh Desire vs Threat");

        QuestNode pursue = CreateNode("Pursue", "Graceful Pursuit");
        QuestNode swoop = CreateNode("Swoop", "Committed Swoop");
        QuestNode bank = CreateNode("Bank", "Wide Bank");

        QuestNode bait = CreateNode("Bait", "Drop Spirit Zones");
        QuestNode herd = CreateNode("Herd", "Herd Player");

        QuestNode spawn = CreateNode("Spawn", "Spawn Wave");
        QuestNode flank = CreateNode("Flank", "Flank via Cover");

        QuestNode breath = CreateNode("Breath", "Breath Attack");
        QuestNode topple = CreateNode("Topple", "Topple Object");

        QuestNode defend = CreateNode("Defend", "Defender");

        QuestNode evade = CreateNode("Evade", "Tactical Withdrawal");
        QuestNode flyEsc = CreateNode("FlyEscape", "Glide to Escape Spline");
        QuestNode exhaust = CreateNode("Exhausted", "Exhausted");
        QuestNode escSpline = CreateNode("EscSpline", "Ride Escape Spline");
        QuestNode recharge = CreateNode("Recharge", "Recharging");
        QuestNode rechargeLoop = CreateNode("RechargeLoop", "Observation Spline Regen");

        QuestNode rage = CreateNode("Rage", "Enraged");
        QuestNode rageEval = CreateNode("RageEval", "Segments Intact?");
        QuestNode relentless = CreateNode("Relentless", "All-in Aggression");
        QuestNode regen = CreateNode("Regen", "Regenerator");
        QuestNode seek = CreateNode("Seek", "Seek Nearest Crystal");
        QuestNode absorb = CreateNode("Absorb", "Drain Crystal");
        QuestNode desperate = CreateNode("Desperate", "Desperate");

        QuestNode die = CreateNode("Die", "Die");

        void ConnectWithCondition(QuestNode from, QuestNode to, string message, string parameter)
        {
            QuestNode.Connection connection = new QuestNode.Connection();
            connection.childNodeID = new StringField(to.id.value);
            from.childList.Add(connection);

            MessageCondition condition = new MessageCondition();
            condition.message = new StringField(message);
            condition.parameter = new StringField(parameter);

            if (to.conditionSet == null) to.conditionSet = new QuestConditionSet();
            to.conditionSet.conditionList.Add(condition);
        }

        void ConnectAuto(QuestNode from, QuestNode to)
        {
            QuestNode.Connection connection = new QuestNode.Connection();
            connection.childNodeID = new StringField(to.id.value);
            from.childList.Add(connection);

            // Auto Proceed Message Condition Strategy
            MessageCondition condition = new MessageCondition();
            condition.message = new StringField("AutoProceed");
            condition.parameter = new StringField(to.id.value);
            if (to.conditionSet == null) to.conditionSet = new QuestConditionSet();
            to.conditionSet.conditionList.Add(condition);

            MessageAction action = new MessageAction();
            action.message = new StringField("AutoProceed");
            action.parameter = new StringField(to.id.value);
            if (from.stateInfoList[(int)QuestNode.State.Active].actionList == null)
                from.stateInfoList[(int)QuestNode.State.Active].actionList = new System.Collections.Generic.List<QuestAction>();
            from.stateInfoList[(int)QuestNode.State.Active].actionList.Add(action);
        }

        void AddLoopResetAction(QuestNode fromNode, string targetNodeID)
        {
            SetQuestNodeStateAction resetAction = new SetQuestNodeStateAction();
            resetAction.questID = new StringField("DragonBrain");
            resetAction.nodeID = new StringField(targetNodeID);
            resetAction.state = QuestNode.State.Active;

            if (fromNode.stateInfoList[(int)QuestNode.State.Active].actionList == null)
                fromNode.stateInfoList[(int)QuestNode.State.Active].actionList = new System.Collections.Generic.List<QuestAction>();

            fromNode.stateInfoList[(int)QuestNode.State.Active].actionList.Add(resetAction);
        }

        // Add FireMessageAction to notify DragonActionListeners to physically act
        void AddBehaviorMessage(QuestNode node, string command)
        {
            MessageAction action = new MessageAction();
            action.message = new StringField("DragonActions");
            action.parameter = new StringField(command);
            if (node.stateInfoList[(int)QuestNode.State.Active].actionList == null)
                node.stateInfoList[(int)QuestNode.State.Active].actionList = new System.Collections.Generic.List<QuestAction>();
            node.stateInfoList[(int)QuestNode.State.Active].actionList.Add(action);
        }

        // ======================= WIRING THE GRAPH =======================
        // Start -> Orchestrator
        ConnectAuto(startNode, orchestrator);
        ConnectAuto(orchestrator, obsSpline);

        // Obs -.-> Engage / Rage
        ConnectWithCondition(obsSpline, engage, "Brain", "PlayerShootsBoss");
        ConnectWithCondition(obsSpline, engage, "Brain", "MinionsBeaten");
        ConnectWithCondition(obsSpline, rage, "Brain", "CrystalDestroyed");

        // Engage -> Eval
        ConnectAuto(engage, evaluate);

        // Eval -> Branches (Driven by 10Hz DecisionTick messages)
        ConnectWithCondition(evaluate, pursue, "Brain", "DesireDominance");
        ConnectWithCondition(evaluate, bait, "Brain", "DesireTerritory");
        ConnectWithCondition(evaluate, spawn, "Brain", "DesireSpawn");
        ConnectWithCondition(evaluate, breath, "Brain", "DesireElement");
        ConnectWithCondition(evaluate, defend, "Brain", "DesireDefend");

        // Immediate overrides
        ConnectWithCondition(evaluate, evade, "Brain", "BurstDamageTaken");
        ConnectWithCondition(evaluate, evade, "Brain", "StaminaEmpty");

        ConnectWithCondition(evaluate, rage, "Brain", "CrystalDestroyed"); // Fast track to rage

        // Dominance: Pursue Loop
        AddBehaviorMessage(pursue, "Pursuit");
        ConnectAuto(pursue, swoop);

        AddBehaviorMessage(swoop, "Swoop");
        ConnectWithCondition(swoop, bank, "Brain", "SwoopOvershoot");

        AddBehaviorMessage(bank, "Bank");
        AddLoopResetAction(bank, "Pursue");

        // Territory: Bait Loop
        AddBehaviorMessage(bait, "Bait");
        ConnectAuto(bait, herd);

        AddBehaviorMessage(herd, "Herd");
        ConnectAuto(herd, swoop); // Drops back into pursue loop

        // Reserve: Spawn Branches
        AddBehaviorMessage(spawn, "SpawnWave");
        ConnectWithCondition(spawn, herd, "Brain", "TagChokepoint");
        ConnectWithCondition(spawn, flank, "Brain", "TagCover");

        AddBehaviorMessage(flank, "Flank");
        AddLoopResetAction(flank, "Evaluate");

        // Elements: Breath Branch
        AddBehaviorMessage(breath, "Breath");
        ConnectWithCondition(breath, topple, "Brain", "TagTopple");

        AddBehaviorMessage(topple, "Topple");
        AddLoopResetAction(topple, "Evaluate");

        // Defender: Defend Branch
        AddBehaviorMessage(defend, "DefendCrystal");
        ConnectWithCondition(defend, engage, "Brain", "CrystalSafe");
        ConnectWithCondition(defend, rage, "Brain", "CrystalDestroyed");

        // Evade & Recharge Branch
        AddBehaviorMessage(evade, "Evade");
        ConnectAuto(evade, flyEsc);
        ConnectAuto(flyEsc, exhaust);

        AddBehaviorMessage(exhaust, "Exhausted");
        ConnectAuto(exhaust, escSpline);
        ConnectAuto(escSpline, recharge);
        ConnectAuto(recharge, rechargeLoop);

        ConnectWithCondition(rechargeLoop, engage, "Brain", "RechargeFull");
        ConnectWithCondition(rechargeLoop, rage, "Brain", "RechargeFullLostCrystal");

        // Rage & Regeneration Branch
        AddBehaviorMessage(rage, "Rage");
        ConnectAuto(rage, rageEval);
        ConnectWithCondition(rageEval, relentless, "Brain", "SegmentsIntact");
        ConnectWithCondition(rageEval, regen, "Brain", "SegmentsMissing");

        AddBehaviorMessage(relentless, "Relentless");
        ConnectWithCondition(relentless, exhaust, "Brain", "StaminaEmpty");

        // Regen Loop
        AddBehaviorMessage(regen, "Regenerate");
        ConnectAuto(regen, seek);
        ConnectAuto(seek, absorb);
        ConnectWithCondition(absorb, engage, "Brain", "SegmentsRestored");
        ConnectWithCondition(absorb, desperate, "Brain", "NoCrystalsLeft");

        AddBehaviorMessage(desperate, "Desperate");
        ConnectAuto(desperate, relentless);

        // Death logic - ANY state can trigger this
        ConnectWithCondition(startNode, die, "Brain", "BossDied");

        SetQuestStateAction endQuest = new SetQuestStateAction();
        endQuest.questID = new StringField("DragonBrain");
        endQuest.state = QuestState.Successful;
        if (die.stateInfoList[(int)QuestNode.State.Active].actionList == null)
            die.stateInfoList[(int)QuestNode.State.Active].actionList = new System.Collections.Generic.List<QuestAction>();
        die.stateInfoList[(int)QuestNode.State.Active].actionList.Add(endQuest);

        // ================================================================

        string path = "Assets/DragonBrain.asset";
        if (!System.IO.Directory.Exists("Assets")) System.IO.Directory.CreateDirectory("Assets");
        AssetDatabase.CreateAsset(quest, path);
        AssetDatabase.SaveAssets();

        Debug.Log($"Successfully generated DragonBrain Quest at {path}");
    }
}

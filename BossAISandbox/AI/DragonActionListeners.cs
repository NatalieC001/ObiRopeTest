using UnityEngine;
using PixelCrushers;

/// <summary>
/// Passive listeners that wait for Quest Machine to dictate state,
/// then drive the actual animation, movement, and spawning.
/// NO UPDATE LOOPS HERE!
/// </summary>
public class DragonActionListeners : MonoBehaviour, IMessageHandler
{
    private AirborneBossMovement movement;
    private ElementalBreathController breath;
    private MinionRequestBroker spawner;
    private BossCreature brain;
    private SegmentedDragonManager anatomy;

    private void Awake()
    {
        movement = GetComponent<AirborneBossMovement>();
        brain = GetComponent<BossCreature>();
        anatomy = GetComponent<SegmentedDragonManager>();
        breath = GetComponent<ElementalBreathController>();
        spawner = FindFirstObjectByType<MinionRequestBroker>();
    }

    private void OnEnable()
    {
        MessageSystem.AddListener(this, "DragonActions", string.Empty);
        MessageSystem.AddListener(this, "Brain", string.Empty);
    }

    private void OnDisable()
    {
        MessageSystem.RemoveListener(this, "DragonActions", string.Empty);
        MessageSystem.RemoveListener(this, "Brain", string.Empty);
    }

    public void OnMessage(MessageArgs messageArgs)
    {
        // PixelCrushers MessageSystem passes the actual command in the parameter
        string action = messageArgs.parameter;
        Vector3 playerPos = GameObject.FindGameObjectWithTag("Player")?.transform.position ?? Vector3.zero;

        switch (action)
        {
            // ---- DragonActions: visual/behavioral commands ----
            case "Pursuit":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerPos);
                break;

            case "Swoop":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerPos);
                if (breath != null) breath.FireBreath(BreathType.Fire, playerPos);
                Invoke(nameof(SimulateSwoopOvershoot), 2.5f);
                break;

            case "Bank":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Withdraw, transform.position + Vector3.up * 20f);
                break;

            case "Bait":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerPos);
                break;

            case "Herd":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerPos);
                break;

            case "SpawnWave":
                if (spawner != null) spawner.RequestMinions(SpawnIntent.AllIn, playerPos);
                break;

            case "Flank":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerPos);
                break;

            case "Breath":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (breath != null) breath.FireBreath(BreathType.Fire, playerPos);
                break;

            case "Topple":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerPos);
                break;

            case "DefendCrystal":
                if (movement != null) movement.RequestReturnToCoil(gameObject);
                break;

            case "Evade":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Exhausted;
                if (movement != null) movement.ForceImmediateEvasion();
                break;

            case "Exhausted":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Exhausted;
                break;

            case "Recharging":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Recharging;
                break;

            case "Rage":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                break;

            case "Relentless":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerPos);
                break;

            case "Regenerate":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Recharging;
                RegeneratorController regen = GetComponent<RegeneratorController>();
                if (regen != null)
                {
                    HealthCrystal[] crystals = FindObjectsByType<HealthCrystal>(FindObjectsSortMode.None);
                    HealthCrystal crystalToHeal = System.Array.Find(crystals, c => c.gameObject.activeInHierarchy);
                    if (crystalToHeal != null)
                    {
                        regen.BeginRegeneration(crystalToHeal);
                        if (spawner != null) spawner.RefillReserve();
                    }
                }
                break;

            case "Desperate":
                if (brain != null) brain.currentPhase = BossCreature.BossPhase.Engaged;
                if (movement != null) movement.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Pursue, playerPos);
                break;

            // ---- Brain: signals that satisfy Quest Machine conditions ----
            case "Weigh Desire vs Threat":
                break;

            case "SwoopOvershoot":
                MessageSystem.SendMessage(this, "SwoopOvershoot", "");
                break;

            case "PlayerShootsBoss":
                MessageSystem.SendMessage(this, "PlayerShootsBoss", "");
                break;

            case "Minions beaten / Time":
                MessageSystem.SendMessage(this, "Minions beaten / Time", "");
                break;

            case "Crystal destroyed":
                MessageSystem.SendMessage(this, "Crystal destroyed", "");
                break;

            case "DesireDominance":
                MessageSystem.SendMessage(this, "DesireDominance", "");
                break;

            case "DesireTerritory":
                MessageSystem.SendMessage(this, "DesireTerritory", "");
                break;

            case "DesireSpawn":
                MessageSystem.SendMessage(this, "DesireSpawn", "");
                break;

            case "DesireElement":
                MessageSystem.SendMessage(this, "DesireElement", "");
                break;

            case "DesireDefend":
                MessageSystem.SendMessage(this, "DesireDefend", "");
                break;

            case "BurstDamageTaken":
                MessageSystem.SendMessage(this, "BurstDamageTaken", "");
                break;

            case "StaminaEmpty":
                MessageSystem.SendMessage(this, "StaminaEmpty", "");
                break;

            case "TagChokepoint":
                MessageSystem.SendMessage(this, "TagChokepoint", "");
                break;

            case "TagCover":
                MessageSystem.SendMessage(this, "TagCover", "");
                break;

            case "ActionComplete":
                MessageSystem.SendMessage(this, "ActionComplete", "");
                break;

            case "CrystalSafe":
                MessageSystem.SendMessage(this, "CrystalSafe", "");
                break;

            case "CrystalDestroyed":
                MessageSystem.SendMessage(this, "CrystalDestroyed", "");
                break;

            case "ReachedSafeAltitude":
                MessageSystem.SendMessage(this, "ReachedSafeAltitude", "");
                break;

            case "StaminaDepleted":
                MessageSystem.SendMessage(this, "StaminaDepleted", "");
                break;

            case "SafeZoneReached":
                MessageSystem.SendMessage(this, "SafeZoneReached", "");
                break;

            case "SplineEnd":
                MessageSystem.SendMessage(this, "SplineEnd", "");
                break;

            case "StaminaFullyCharged":
                MessageSystem.SendMessage(this, "StaminaFullyCharged", "");
                break;

            case "CrystalsLost":
                MessageSystem.SendMessage(this, "CrystalsLost", "");
                break;

            case "EnrageStart":
                MessageSystem.SendMessage(this, "EnrageStart", "");
                break;

            case "SegmentsBreached":
                MessageSystem.SendMessage(this, "SegmentsBreached", "");
                break;

            case "AngerMaxed":
                MessageSystem.SendMessage(this, "AngerMaxed", "");
                break;

            case "AtCrystal":
                MessageSystem.SendMessage(this, "AtCrystal", "");
                break;

            case "CrystalInterrupted":
                MessageSystem.SendMessage(this, "CrystalInterrupted", "");
                break;

            case "RegenComplete":
                MessageSystem.SendMessage(this, "RegenComplete", "");
                break;

            case "DesperateTimerEnd":
                MessageSystem.SendMessage(this, "DesperateTimerEnd", "");
                break;

            case "Circle + Spawn Minions":
                if (spawner != null) spawner.RequestMinions(SpawnIntent.Circle, playerPos);
                MessageSystem.SendMessage(this, "Circle + Spawn Minions", "");
                break;
        }
    }

    private void SimulateSwoopOvershoot()
    {
        MessageSystem.SendMessage(this, "SwoopOvershoot", "");
    }
}

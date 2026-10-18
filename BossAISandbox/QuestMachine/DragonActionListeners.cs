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
        // Listen to all messages directed at "DragonActions"
        MessageSystem.AddListener(this, "DragonActions", string.Empty);
    }

    private void OnDisable()
    {
        MessageSystem.RemoveListener(this);
    }

    public void OnMessage(MessageArgs messageArgs)
    {
        string action = messageArgs.parameter;
        Vector3 playerPos = GameObject.FindGameObjectWithTag("Player")?.transform.position ?? Vector3.zero;
        switch (action)
        {

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
                if (movement != null)
                {
                    movement.RequestReturnToCoil(gameObject);
                }
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
        }
    }

    private void SimulateSwoopOvershoot()
    {
        MessageSystem.SendMessage(this, "Brain", "SwoopOvershoot", string.Empty);
    }
}

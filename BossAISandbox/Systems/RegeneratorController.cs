using UnityEngine;

public class RegeneratorController : MonoBehaviour
{
    [Header("Regeneration Limits")]
    public float maxRegenTime = 15f; // Hard cap
    public float healthPerSecond = 5f;

    private float currentRegenTimer = 0f;
    private bool isRegenerating = false;

    private AirborneBossMovement movementManager;
    private MinionRequestBroker requestBroker;
    private SegmentedDragonManager segmentManager;

    private void Awake()
    {
        movementManager = GetComponent<AirborneBossMovement>();
        segmentManager = GetComponent<SegmentedDragonManager>();
        requestBroker = FindFirstObjectByType<MinionRequestBroker>();
    }

    public void BeginRegeneration(HealthCrystal targetCrystal)
    {
        if (targetCrystal == null || targetCrystal.IsDestroyed) return;

        isRegenerating = true;
        currentRegenTimer = 0f;

        // 1. Move to the crystal
        // BossCreature already blends to it via Desire Evaluator, but we ensure we are in a slower coil state

        // 2. Refill minion reserve immediately
        if (requestBroker != null)
        {
            requestBroker.RefillReserve();
        }

        if (segmentManager != null)
        {
            segmentManager.StartRegeneration(targetCrystal);
        }

        Debug.Log("[RegeneratorController] Dragon has begun regeneration loop.");
    }

    private void Update()
    {
        if (!isRegenerating) return;

        currentRegenTimer += Time.deltaTime;

        if (currentRegenTimer >= maxRegenTime)
        {
            FinishRegeneration();
        }
    }

    private void FinishRegeneration()
    {
        isRegenerating = false;
        currentRegenTimer = 0f;
        Debug.Log("[RegeneratorController] 15-second cap reached. Forcing Dragon off crystal.");

        if (segmentManager != null)
        {
            segmentManager.StopRegeneration();
        }

        if (movementManager != null)
        {
            movementManager.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Withdraw, transform.position + Vector3.up * 20f);
        }

        PixelCrushers.MessageSystem.SendMessage(this, "Brain", "RechargeFull", string.Empty);
    }
}

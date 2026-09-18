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
    private BossCreature brain;
    private SegmentedDragonManager segmentManager;


    private void Awake()
    {
        movementManager = GetComponent<AirborneBossMovement>();
        brain = GetComponent<BossCreature>();
        requestBroker = FindFirstObjectByType<MinionRequestBroker>();
        segmentManager = GetComponent<SegmentedDragonManager>();
    }

    public void BeginRegeneration(HealthCrystal targetCrystal)
    {
        if (targetCrystal == null || targetCrystal.isDestroyed) return;

        isRegenerating = true;
        currentRegenTimer = 0f;

        // 1. Move to the crystal
        // BossCreature already blends to it via Desire Evaluator, but we ensure we are in a slower coil state

        // 2. Refill minion reserve immediately
        if (requestBroker != null)
        {
            requestBroker.RefillReserve();
        }

        Debug.Log("[RegeneratorController] Dragon has begun regeneration loop.");
    }

    private void Update()
    {
        if (!isRegenerating) return;

        currentRegenTimer += Time.deltaTime;

        // Heal Boss
        if (brain != null)
        {
            // Note: Since Health is abstracted, this would need an AddHealth method,
            // but for now we apply negative damage.
            brain.TakeDamage(-healthPerSecond * Time.deltaTime, transform.position, ElementTypeOB7.Normal);
        }

        if (segmentManager != null)
        {
            // Segment rebuild hook would go here, e.g. segmentManager.RebuildNextSegment()
            // (Assuming SegmentedDragonManager exposes such a method, which we would add if needed)
        }

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

        if (movementManager != null)
        {
            movementManager.RequestFreestyleIntent(AirborneBossMovement.FreestyleIntent.Withdraw, transform.position + Vector3.up * 20f);
        }

        // We could manually trigger a Desire evaluation here to re-prioritize
        if (brain != null)
        {
            brain.EvaluateDesires();
        }
    }
}

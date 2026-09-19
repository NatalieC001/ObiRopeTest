using UnityEngine;
using Dreamteck.Splines;
using System.Collections.Generic;

/// <summary>
/// This script lives permanently in the Boss Level Scene.
/// It holds references to environmental splines (which prefabs cannot do) 
/// and orchestrates the battle phases between the Boss and its minions.
// We can consider this a placeholder script because I think that we will make the Dragons. Have their own AI, and brain. And they're going to manage their own splines. That they can find within the scene. This is already partially created through the level. Scriptable objects. Where we can see the observation paths and also the escape paths arrays. Already providing. Paths to the scene when it's instantiated. If there's multiple busses. I think it's quite OK that they share the paths. So we can consider the script as mostly. Unneeded unless we were testing a prototype in the scene without the wish to. Create the CVS for the level loading.
/// </summary>
public class BossArenaManager : MonoBehaviour
{
    [Header("Arena Environment")]
    [Tooltip("The spline where the boss watches safely during Phase 1.")]
    public SplineComputer observationSpline;

    [Tooltip("The splines the boss uses to evade attacks during Phase 2.")]
    public SplineComputer[] tacticalEscapeRoutes;

    [Header("Battle Rules")]
    [Tooltip("The percentage of minions remaining that triggers the boss to panic and attack (e.g., 0.3 = 30%).")]
    [Range(0f, 1f)]
    public float panicEngagementThreshold = 0.3f;

    [Header("Battle State")]
    public BossCreature activeBoss;
    public List<StandardCreature> activeMinions = new List<StandardCreature>();

    private int startingMinionCount;
    private bool bossEngaged = false;

    private void Update()
    {
        // If the battle hasn't progressed to Phase 2 yet...
        if (activeBoss != null && !bossEngaged)
        {
            // Clean up the minion list (remove dead ones)
            activeMinions.RemoveAll(m => m == null);

            // Calculate remaining percentage
            float currentPercentage = startingMinionCount > 0 ? (float)activeMinions.Count / startingMinionCount : 0f;

            // If minions drop below the threshold, the boss panics and enters the fight!
            if (currentPercentage <= panicEngagementThreshold)
            {
                TriggerBossEngagement();
            }
        }
    }

    /// <summary>
    /// Called by your wave spawner when the boss wave begins.
    /// </summary>
    public void RegisterBattleParticipants(BossCreature boss, List<StandardCreature> minions)
    {
        activeBoss = boss;
        activeMinions = minions;
        startingMinionCount = activeMinions.Count;
        bossEngaged = false;

        // Give the boss the environmental splines from the scene
        // activeBoss.InitializeArena(this); // Handled natively by BossCreature
    }

    /// <summary>
    /// Exclusively used when a developer drags the Boss prefab into the scene manually for testing.
    /// It bypasses the minion count logic and instantly starts the fight.
    /// </summary>
    public void RegisterStrayBoss(BossCreature boss)
    {
        activeBoss = boss;
        activeMinions = new List<StandardCreature>();
        startingMinionCount = 0;

        // Give the boss the environmental splines from the scene
        // activeBoss.InitializeArena(this); // Handled natively by BossCreature

        // Because there are no minions to protect it, force it to engage immediately
        TriggerBossEngagement();
    }

    private void TriggerBossEngagement()
    {
        bossEngaged = true;
        Debug.Log("<color=magenta>[BossArenaManager] All minions defeated! The Boss is engaging the player!</color>");
        // activeBoss.EngagePlayer(); // Handled natively by QuestMachine
    }
}

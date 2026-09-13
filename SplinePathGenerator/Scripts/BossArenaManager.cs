using UnityEngine;
using Dreamteck.Splines;
using System.Collections.Generic;

/// <summary>
/// This script lives permanently in the Boss Level Scene.
/// It holds references to environmental splines (which prefabs cannot do)
/// and orchestrates the battle phases between the Boss and its minions.
/// </summary>
public class BossArenaManager : MonoBehaviour
{
    [Header("Arena Environment")]
    [Tooltip("The spline where the boss watches safely during Phase 1.")]
    public SplineComputer observationSpline;

    [Tooltip("The splines the boss uses to evade attacks during Phase 2.")]
    public SplineComputer[] tacticalEscapeRoutes;

    [Header("Battle State")]
    public BossCreature activeBoss;
    public List<StandardCreature> activeMinions = new List<StandardCreature>();

    private bool bossEngaged = false;

    private void Update()
    {
        // If the battle hasn't progressed to Phase 2 yet...
        if (activeBoss != null && !bossEngaged)
        {
            // Clean up the minion list (remove dead ones)
            activeMinions.RemoveAll(m => m == null);

            // If all minions are defeated, the boss enters the fight!
            if (activeMinions.Count == 0)
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
        bossEngaged = false;

        // Give the boss the environmental splines from the scene
        activeBoss.InitializeArena(this);
    }

    private void TriggerBossEngagement()
    {
        bossEngaged = true;
        Debug.Log("<color=magenta>[BossArenaManager] All minions defeated! The Boss is engaging the player!</color>");
        activeBoss.EngagePlayer();
    }
}

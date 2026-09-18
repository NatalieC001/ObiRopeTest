using System;
using UnityEngine;

/// <summary>
/// A non-static, scene-level manager that routes major events to the Boss Brain and other listeners.
/// Provides decoupled immediate interrupts.
/// </summary>
public class BossEventBus : MonoBehaviour
{
    // C# Actions
    public event Action<BossCreature, ElementTypeOB7> OnBossDamaged;
    public event Action<HealthCrystal> OnCrystalDamaged;
    public event Action<HealthCrystal> OnCrystalDestroyed;
    public event Action<GameObject> OnBossMinionDied; // Using GameObject to represent MinionRef for now
    public event Action OnBossStatusEnded; // Could pass StatusFlags in the future

    public void TriggerBossDamaged(BossCreature boss, ElementTypeOB7 element)
    {
        OnBossDamaged?.Invoke(boss, element);
    }

    public void TriggerCrystalDamaged(HealthCrystal crystal)
    {
        OnCrystalDamaged?.Invoke(crystal);
    }

    public void TriggerCrystalDestroyed(HealthCrystal crystal)
    {
        OnCrystalDestroyed?.Invoke(crystal);
    }

    public void TriggerBossMinionDied(GameObject minion)
    {
        OnBossMinionDied?.Invoke(minion);
    }

    public void TriggerBossStatusEnded()
    {
        OnBossStatusEnded?.Invoke();
    }
}

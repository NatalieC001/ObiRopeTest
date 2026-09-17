# Level Progression Core Loop

> [!info] Architecture Overview
> This diagram illustrates the **Event-Driven Progression Loop** from the player's perspective, mapping how hitting the Gong triggers the state machine, which sequentially fires instructions to the UI and Spawner without relying on Coroutines.

```mermaid
sequenceDiagram
    autonumber

    actor Player
    participant Gong as LevelAdvanceGong
    participant LPM as LevelProgressionManager
    participant UI as LevelUIManager
    participant Wave as WaveSpawner
    participant Boss as BossArenaManager

    note over Player,Boss: STATE: Waiting For Gong

    Player->>Gong: Shoots Arrow
    Gong-->>LPM: ReceiveGongHit() (Event)

    note over LPM: STATE: Countdown
    LPM-->>UI: OnCountdownUpdated(time)

    loop Every Frame
        UI->>Player: Displays "3... 2... 1..."
    end

    note over LPM: STATE: Wave Active
    LPM-->>Wave: OnWaveStartRequested(LevelConfig)

    loop Target Spawning
        Wave->>Wave: Checks WaveData.characters
        Wave->>Wave: Instantiates Minion/Boss Prefabs
    end

    alt If Standard Minion Wave
        loop Combat
            Player->>Wave: Destroys targets
            Wave-->>UI: OnWaveProgressUpdated("Targets Left: X")
        end
        Wave-->>LPM: ReceiveWaveCompleted()
        LPM->>LPM: Increments Wave Index. Loops back to Wave Active.

    else If Boss Wave
        Wave-->>LPM: NotifyBossWaveStarted()
        note over LPM: STATE: Boss Active
        loop Boss Combat
            Player->>Boss: Defeats Dragon
        end
        Boss-->>LPM: ReceiveBossDefeated()
    end

    note over LPM: STATE: Level Victory
    LPM-->>UI: OnLevelOutroReady()
    UI->>Player: Displays "Level Complete!"

    note over LPM: Wait 4 seconds (Timer)

    LPM->>LPM: PrepareLevel(nextIndex)
    note over LPM: STATE: Waiting For Gong (Loop repeats)
```
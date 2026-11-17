using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Bliss.Physics;
using ET;

namespace Bliss;

/// <summary>
/// Stops server-side LevelScene behavior when enabling the scene tree fails.
/// </summary>
[Event(ELogicSceneType.Level)]
public class EventHandler_StopEnableLevelScene_Server : AEvent<LogicScene, Event_StopEnableLevelScene>
{
    protected override async ETTask Run(LogicScene levelScene, Event_StopEnableLevelScene eventInfo)
    {
        List<Exception> cleanupExceptions = new List<Exception>();
        try
        {
            levelScene.GetComponent<PhysicalScene>()?.StopUpdates();
        }
        catch (Exception exception)
        {
            cleanupExceptions.Add(exception);
        }

        try
        {
            LevelSceneServerEnableStateComponent enableState =
                levelScene.GetComponent<LevelSceneServerEnableStateComponent>();
            GameplayWorld gameplayWorld =
                levelScene.GetComponent<LevelGameWorldComponent>()?.GameplayWorld;
            if (enableState?.CreatedDemoGameMode != null &&
                gameplayWorld != null &&
                ReferenceEquals(gameplayWorld.GetComponent<DemoGameMode>(), enableState.CreatedDemoGameMode))
            {
                gameplayWorld.RemoveComponent<DemoGameMode>();
            }
        }
        catch (Exception exception)
        {
            cleanupExceptions.Add(exception);
        }

        ThrowCleanupExceptions(cleanupExceptions);

        await ETTask.CompletedTask;
    }

    private static void ThrowCleanupExceptions(List<Exception> cleanupExceptions)
    {
        if (cleanupExceptions.Count == 1)
        {
            ExceptionDispatchInfo.Capture(cleanupExceptions[0]).Throw();
        }
        else if (cleanupExceptions.Count > 1)
        {
            throw new AggregateException("Server LevelScene cleanup failed.", cleanupExceptions);
        }
    }
}

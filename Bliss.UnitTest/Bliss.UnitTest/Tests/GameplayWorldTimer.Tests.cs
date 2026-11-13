using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Bliss.Core;
using ET;

namespace Bliss.Tests;

[NonParallelizable]
public class GameplayWorldTimerTests
{
    private const int ROOT_ID = 101;
    private const int ROOT_ZONE = 1;
    private const long GAMEPLAY_WORLD_ID = 202;
    private const long GAMEPLAY_WORLD_INSTANCE_ID = 10001;
    private const int LEVEL_SCENE_ID = 303;
    private const long LEVEL_SCENE_INSTANCE_ID = 10002;
    private const long IMMEDIATE_DELAY_MILLISECONDS = 0;
    private const long INVALID_DELAY_MILLISECONDS = -1;
    private const long INVALID_TIMER_ID = 0;
    private const int EXPECTED_CALLBACK_COUNT = 1;
    private const int CANCELLED_CALLBACK_INCREMENT = 100;
    private const int OWNER_INVALID_WARNING_COUNT = 3;
    private const long FIRST_TEST_TIME = 1000;
    private const long EARLIER_DUE_TIME = 1010;
    private const long SAME_DUE_TIME = 1020;
    private const long REPEATED_INTERVAL_MILLISECONDS = 10;
    private const long DELAYED_SERVER_TIME = 1050;
    private const long NEXT_REPEATED_DUE_TIME = 1060;
    private const long WAIT_DEADLINE = 1010;
    private const long WAIT_DURATION_MILLISECONDS = 10;
    private const long OWNER_TIMER_FUTURE_DEADLINE = 2000;
    private const int FIRST_CALLBACK_INCREMENT = 1;
    private const int DEFERRED_CALLBACK_INCREMENT = 10;
    private const int SECOND_INVOCATION_COUNT = 2;

    private TypeManager _typeManager;
    private EntitySystemManager _entitySystemManager;
    private Root _root;
    private GameplayWorld _gameplayWorld;
    private ILogger _previousLogger;
    private RecordingLogger _logger;

    [SetUp]
    public void SetUp()
    {
        TimeInfo.Instance.ServerMinusClientTime = 0;
        _previousLogger = Log.Logger;
        _logger = new RecordingLogger();
        Log.Logger = _logger;

        _typeManager = new TypeManager();
        _typeManager.Init(new List<Assembly>
        {
            typeof(Entity).Assembly,
            typeof(GameplayWorldTimerTests).Assembly,
        });
        _typeManager.Register();

        _entitySystemManager = new EntitySystemManager();
        _entitySystemManager.Init();
        _entitySystemManager.Register();
        _root = new Root(ROOT_ID, ROOT_ZONE, ELogicSceneType.Main, nameof(GameplayWorldTimerTests));

        _gameplayWorld = GameplayWorldFactory.Create(
            _root.Scene,
            GAMEPLAY_WORLD_ID,
            GAMEPLAY_WORLD_INSTANCE_ID,
            nameof(GameplayWorld));
        var levelScene = LogicSceneFactoryCore.CreateLogicScene(
            _root.Scene,
            LEVEL_SCENE_ID,
            LEVEL_SCENE_INSTANCE_ID,
            ELogicSceneType.Level,
            nameof(LEVEL_SCENE_ID));
        levelScene.AddComponent<LevelGameWorldComponent>().GameplayWorld = _gameplayWorld;
    }

    [TearDown]
    public void TearDown()
    {
        _gameplayWorld?.Dispose();
        _root?.Dispose();
        _entitySystemManager.Dispose();
        _typeManager.Dispose();
        TimeInfo.Instance.ServerMinusClientTime = 0;
        Log.Logger = _previousLogger;
    }

    [Test]
    public void DueTimerRunsWhenTheGameplayWorldIsUpdated()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => callbackCount++);

        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));
    }

    [Test]
    public void DueTimersRunByTimestampThenRegistrationOrder()
    {
        var executionOrder = new List<string>();
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleOnceAt(
            SAME_DUE_TIME,
            () => executionOrder.Add("first"));
        _gameplayWorld.Timer.ScheduleOnceAt(
            EARLIER_DUE_TIME,
            () => executionOrder.Add("earlier"));
        _gameplayWorld.Timer.ScheduleOnceAt(
            SAME_DUE_TIME,
            () => executionOrder.Add("second"));

        setServerTime(SAME_DUE_TIME);
        _root.Update();

        Assert.That(executionOrder, Is.EqualTo(new[] { "earlier", "first", "second" }));
    }

    [Test]
    public void TimersCanBeCancelledAndInvalidIntervalsAreRejected()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);
        long timerId = _gameplayWorld.Timer.ScheduleOnceAt(
            OWNER_TIMER_FUTURE_DEADLINE,
            () => callbackCount++);

        Assert.That(_gameplayWorld.Timer.Cancel(timerId), Is.True);
        Assert.That(_gameplayWorld.Timer.Cancel(timerId), Is.False);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _gameplayWorld.Timer.ScheduleOnce(INVALID_DELAY_MILLISECONDS, () => { }));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _gameplayWorld.Timer.ScheduleRepeated(IMMEDIATE_DELAY_MILLISECONDS, () => { }));

        setServerTime(OWNER_TIMER_FUTURE_DEADLINE);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(0));
    }

    [Test]
    public void TimersAddedDuringCallbacksWaitUntilNextUpdateAndCancelledCallbacksAreSkipped()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        long cancelledTimerId = INVALID_TIMER_ID;
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () =>
        {
            callbackCount += FIRST_CALLBACK_INCREMENT;
            _gameplayWorld.Timer.ScheduleOnce(
                IMMEDIATE_DELAY_MILLISECONDS,
                () => callbackCount += DEFERRED_CALLBACK_INCREMENT);
            _gameplayWorld.Timer.Cancel(cancelledTimerId);
        });
        cancelledTimerId = _gameplayWorld.Timer.ScheduleOnce(
            IMMEDIATE_DELAY_MILLISECONDS,
            () => callbackCount += CANCELLED_CALLBACK_INCREMENT);

        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(FIRST_CALLBACK_INCREMENT));

        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(FIRST_CALLBACK_INCREMENT + DEFERRED_CALLBACK_INCREMENT));
    }

    [Test]
    public void DelayedRepeatedTimerFiresOnceAndReschedulesFromCurrentTime()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleRepeated(
            REPEATED_INTERVAL_MILLISECONDS,
            () => callbackCount++);

        setServerTime(DELAYED_SERVER_TIME);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));

        _root.Update();
        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));

        setServerTime(NEXT_REPEATED_DUE_TIME);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT + EXPECTED_CALLBACK_COUNT));
    }

    [Test]
    public void RepeatedTimerKeepsOriginalRegistrationOrderWhenRescheduledIntoExistingBucket()
    {
        var executionOrder = new List<string>();
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleRepeated(
            REPEATED_INTERVAL_MILLISECONDS,
            () => executionOrder.Add("repeated"));
        _gameplayWorld.Timer.ScheduleOnceAt(SAME_DUE_TIME, () => executionOrder.Add("once"));

        setServerTime(EARLIER_DUE_TIME);
        _root.Update();
        setServerTime(SAME_DUE_TIME);
        _root.Update();

        Assert.That(executionOrder, Is.EqualTo(new[] { "repeated", "repeated", "once" }));
    }

    [Test]
    public void CallbackFailureLeavesOtherDueTimersAvailableForNextUpdate()
    {
        var executionOrder = new List<string>();
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () =>
        {
            executionOrder.Add("failed");
            throw new InvalidOperationException("expected test failure");
        });
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => executionOrder.Add("remaining"));

        Assert.Throws<Exception>(() => _root.Update());
        Assert.That(executionOrder, Is.EqualTo(new[] { "failed" }));

        _root.Update();
        Assert.That(executionOrder, Is.EqualTo(new[] { "failed", "remaining" }));
    }

    [Test]
    public void DisposingWorldDuringCallbackCancelsRemainingTimersInSnapshot()
    {
        var executionOrder = new List<string>();
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () =>
        {
            executionOrder.Add("dispose");
            _gameplayWorld.Dispose();
        });
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => executionOrder.Add("remaining"));

        _root.Update();

        Assert.That(executionOrder, Is.EqualTo(new[] { "dispose" }));
    }

    [Test]
    public async Task WaitsCompleteWhenTheirDueTimeOrNextFrameIsProcessed()
    {
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        ETTask<bool> durationWait = _gameplayWorld.Timer.WaitDurationAsync(WAIT_DURATION_MILLISECONDS);
        ETTask<bool> timestampWait = _gameplayWorld.Timer.WaitUntilAsync(WAIT_DEADLINE);
        ETTask<bool> nextFrameWait = _gameplayWorld.Timer.WaitNextFrameAsync();

        setServerTime(WAIT_DEADLINE);
        _root.Update();

        Assert.That(await durationWait, Is.True);
        Assert.That(await timestampWait, Is.True);
        Assert.That(await nextFrameWait, Is.True);
    }

    [Test]
    public async Task CancellationAndWorldDestructionCompleteWaitsWithFalse()
    {
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);
        var cancellationToken = new ETCancellationToken();
        int callbackCount = 0;

        ETTask<bool> cancelledWait = _gameplayWorld.Timer.WaitDurationAsync(
            WAIT_DURATION_MILLISECONDS,
            cancellationToken);
        cancellationToken.Cancel();

        Assert.That(await cancelledWait, Is.False);
        Assert.That(
            await _gameplayWorld.Timer.WaitDurationAsync(IMMEDIATE_DELAY_MILLISECONDS, cancellationToken),
            Is.False);

        ETTask<bool> destroyedWorldWait = _gameplayWorld.Timer.WaitNextFrameAsync();
        _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => callbackCount++);
        _gameplayWorld.Dispose();

        Assert.That(await destroyedWorldWait, Is.False);

        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(0));
    }

    [Test]
    public async Task OwnerMustBelongToWorldAndInvalidOwnersAreSkipped()
    {
        var foreignOwner = _root.Scene.AddChild<TimerOwnerEntity>();
        Assert.Throws<InvalidOperationException>(
            () => _gameplayWorld.Timer.ScheduleOnce(IMMEDIATE_DELAY_MILLISECONDS, () => { }, foreignOwner));

        var owner = _gameplayWorld.AddChild<TimerOwnerEntity>();
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);
        long timerId = _gameplayWorld.Timer.ScheduleOnceAt(
            OWNER_TIMER_FUTURE_DEADLINE,
            () => callbackCount++,
            owner);
        ETTask<bool> ownerWait = _gameplayWorld.Timer.WaitNextFrameAsync(owner: owner);
        long repeatedTimerId = _gameplayWorld.Timer.ScheduleRepeated(
            REPEATED_INTERVAL_MILLISECONDS,
            () => callbackCount++,
            owner);

        owner.Dispose();
        setServerTime(WAIT_DEADLINE);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(0));
        Assert.That(_gameplayWorld.Timer.Cancel(timerId), Is.False);
        Assert.That(_gameplayWorld.Timer.Cancel(repeatedTimerId), Is.False);
        Assert.That(await ownerWait, Is.False);
        Assert.That(_logger.Warnings, Has.Count.EqualTo(OWNER_INVALID_WARNING_COUNT));
    }

    [Test]
    public void CallbackFailuresPropagateAndRepeatedTimersRemainScheduled()
    {
        int callbackCount = 0;
        _root.EarlyUpdate();
        setServerTime(FIRST_TEST_TIME);

        _gameplayWorld.Timer.ScheduleRepeated(REPEATED_INTERVAL_MILLISECONDS, () =>
        {
            callbackCount++;
            if (callbackCount == EXPECTED_CALLBACK_COUNT)
            {
                throw new InvalidOperationException("expected test failure");
            }
        });

        setServerTime(FIRST_TEST_TIME + REPEATED_INTERVAL_MILLISECONDS);
        Exception exception = Assert.Throws<Exception>(() => _root.Update());

        Assert.That(exception.InnerException, Is.TypeOf<GameplayWorldTimerException>());
        Assert.That(exception.InnerException.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(callbackCount, Is.EqualTo(EXPECTED_CALLBACK_COUNT));

        setServerTime(NEXT_REPEATED_DUE_TIME);
        _root.Update();

        Assert.That(callbackCount, Is.EqualTo(SECOND_INVOCATION_COUNT));
    }

    private static void setServerTime(long serverTime)
    {
        TimeInfo.Instance.ServerMinusClientTime = serverTime - TimeInfo.Instance.FrameTime;
    }

    public class TimerOwnerEntity : Entity
    {
    }

    private sealed class RecordingLogger : ILogger
    {
        internal List<string> Warnings { get; } = new List<string>();

        public void Trace(string message)
        {
        }

        public void Warning(string message)
        {
            this.Warnings.Add(message);
        }

        public void Info(string message)
        {
        }

        public void Debug(string message)
        {
        }

        public void Error(string message)
        {
        }

        public void Error(Exception exception)
        {
        }

        public void Trace(ref DefaultInterpolatedStringHandler message)
        {
        }

        public void Warning(ref DefaultInterpolatedStringHandler message)
        {
        }

        public void Info(ref DefaultInterpolatedStringHandler message)
        {
        }

        public void Debug(ref DefaultInterpolatedStringHandler message)
        {
        }

        public void Error(ref DefaultInterpolatedStringHandler message)
        {
        }
    }
}
